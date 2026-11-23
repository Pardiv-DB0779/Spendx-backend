using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GPC.Boost.SpendX.Data;
using GPC.Boost.SpendX.Domain.Models;
using GPC.Boost.SpendX.Dto.RequestDto;
using GPC.Boost.SpendX.Dto.ResponseDto;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace GPC.Boost.SpendX.Repository
{
    public interface ISpendXRepository
    {
        Task<List<TProduct>> GetProductsByBrandsAsync(List<string> brands, string country, List<string>? itemClass4 = null);
        Task<(bool Success, int OfferId)> SaveOrUpdateSpendXOfferAsync(TEventOffer offer, List<TEventOfferDetail> details, bool isUpdate, bool isReserved = false);
        Task<TEventOffer?> GetOfferByIdAsync(int offerId, int offerNo = 1);
        Task<TEventOffer?> GetOfferByPositionAsync(int eventId, int pageId, int positionId);
        Task<List<TEventOfferDetail>> GetOfferDetailsAsync(int offerId, int offerNo = 1);
        Task<bool> UpdateOfferHeaderOverviewAsync(TEventOffer offer);
        Task<bool> AddEventOfferSearchHistoryAsync(TEventOfferSearchHistory searchHistory);
        Task<TEventOfferSearchHistory?> GetEventOfferSearchHistoryAsync(int eventId, int pageId, int positionId, int offerId, int offerNumber);
        Task<bool> UpdateEventOfferSearchHistoryAsync(TEventOfferSearchHistory searchHistory);
        Task<(List<OfferSkuOverviewDto> Items, int TotalCount)> GetDataViewAsync(DataViewRequestDto request);
        Task<bool> UpdateOfferDetailsRangeAsync(List<TEventOfferDetail> details);
        Task<bool> SaveDataViewDetailsAndRecalculateHeaderAsync(List<TEventOfferDetail> details, int offerId, int offerNo, int offerTypeId);
        Task<bool> CalculateDataViewAsync(DataViewRequestDto request);
        Task<bool> CalculateEventOfferHeaderAsync(int offerId, int offerNo, int offerTypeId);
    }

    public class SpendXRepository : ISpendXRepository
    {
        private readonly SpendXDbContext _context;
        private readonly ILogger<SpendXRepository> _logger;

        public SpendXRepository(SpendXDbContext context, ILogger<SpendXRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<TProduct>> GetProductsByBrandsAsync(List<string> brands, string country, List<string>? itemClass4 = null)
        {
            var upperBrands = brands.Where(b => !string.IsNullOrWhiteSpace(b)).Select(b => b.ToUpper()).ToList();
            _logger.LogInformation("Querying tproducts for Brands={Brands}, Country={Country}", string.Join(", ", upperBrands), country);

            var query = _context.TProducts
                .Where(p => p.Country.ToUpper() == country.ToUpper()
                         && (p.IsActive == true || p.IsActive == null)
                         && p.Brand != null && upperBrands.Contains(p.Brand.ToUpper()));

            if (itemClass4 != null && itemClass4.Any())
            {
                var upperIc4 = itemClass4.Where(ic => !string.IsNullOrWhiteSpace(ic)).Select(ic => ic.ToUpper()).ToList();
                query = query.Where(p => p.ItemClass4 != null && upperIc4.Contains(p.ItemClass4.ToUpper()));
            }

            return await query.ToListAsync();
        }

        public async Task<(bool Success, int OfferId)> SaveOrUpdateSpendXOfferAsync(TEventOffer offer, List<TEventOfferDetail> details, bool isUpdate, bool isReserved = false)
        {
            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                if (isUpdate)
                {
                    _context.TEventOffers.Update(offer);

                    // Execute direct delete to avoid EF tracking collisions
                    await _context.TEventOfferDetails
                        .Where(d => d.OfferId == offer.OfferId && d.OfferNo == offer.OfferNumber)
                        .ExecuteDeleteAsync();
                }
                else
                {
                    await _context.TEventOffers.AddAsync(offer);
                    await _context.SaveChangesAsync();
                }

                foreach (var d in details)
                {
                    d.OfferId = offer.OfferId;
                    d.OfferNo = offer.OfferNumber;
                    d.Page = offer.Page;
                    d.PagePosition = offer.PagePosition;
                }

                await _context.TEventOfferDetails.AddRangeAsync(details);
                await _context.SaveChangesAsync();

                // Synchronize tile in tmudmap (5th table)
                var mudMap = await _context.TMudMaps
                    .FirstOrDefaultAsync(m => m.EventId == offer.EventId && m.PageId == offer.Page && m.PositionId == offer.PagePosition);

                if (mudMap == null)
                {
                    mudMap = new TMudMap
                    {
                        PageId = offer.Page,
                        PositionId = offer.PagePosition,
                        EventId = offer.EventId,
                        Country = offer.Country,
                        IsReserved = isReserved
                    };
                    await _context.TMudMaps.AddAsync(mudMap);
                }
                else if (isReserved)
                {
                    mudMap.IsReserved = true;
                }

                SyncMudMapFromOffer(mudMap, offer);

                await _context.SaveChangesAsync();

                // Recalculate header rollup via stored procedure inside transaction to ensure consistency
                if (details != null && details.Any())
                {
                    await CalculateEventOfferHeaderAsync(offer.OfferId, offer.OfferNumber, offer.OfferTypeId);
                }

                await tx.CommitAsync();
                return (true, offer.OfferId);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Failed to save or update SpendX offer: {Message}", ex.Message);
                return (false, 0);
            }
        }

        public async Task<TEventOffer?> GetOfferByIdAsync(int offerId, int offerNo = 1)
        {
            return await _context.TEventOffers
                .FirstOrDefaultAsync(o => o.OfferId == offerId && o.OfferNumber == offerNo);
        }

        public async Task<TEventOffer?> GetOfferByPositionAsync(int eventId, int pageId, int positionId)
        {
            return await _context.TEventOffers
                .OrderByDescending(o => o.OfferId)
                .FirstOrDefaultAsync(o => o.EventId == eventId && o.Page == pageId && o.PagePosition == positionId);
        }

        public async Task<List<TEventOfferDetail>> GetOfferDetailsAsync(int offerId, int offerNo = 1)
        {
            return await _context.TEventOfferDetails
                .Where(d => d.OfferId == offerId && d.OfferNo == offerNo && (d.IsSkuActive == true || d.IsSkuActive == null))
                .ToListAsync();
        }

        public async Task<bool> UpdateOfferHeaderOverviewAsync(TEventOffer offer)
        {
            try
            {
                _context.TEventOffers.Update(offer);

                var mudMap = await _context.TMudMaps
                    .FirstOrDefaultAsync(m => m.EventId == offer.EventId && m.PageId == offer.Page && m.PositionId == offer.PagePosition);

                if (mudMap != null)
                {
                    SyncMudMapFromOffer(mudMap, offer);
                }

                await _context.SaveChangesAsync();

                // Recalculate header financials via stored procedure (BRD Items 2 & 7)
                await CalculateEventOfferHeaderAsync(offer.OfferId, offer.OfferNumber, offer.OfferTypeId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update offer overview: {Message}", ex.Message);
                return false;
            }
        }

        private static void SyncMudMapFromOffer(TMudMap mudMap, TEventOffer offer)
        {
            mudMap.OfferId = offer.OfferId;
            mudMap.OfferNumber = offer.OfferNumber;
            mudMap.OfferName = offer.OfferName;
            mudMap.OfferType = offer.OfferType;
            mudMap.Brand = offer.Brand;
            mudMap.Loyalty = offer.IsRewards ?? false;
            mudMap.SavePercent = offer.SavePct1;
            mudMap.Clearance = offer.IsClearance ?? false;
            mudMap.New = offer.IsNew ?? false;
            mudMap.EverydayPrice = offer.EverydayPrice;
            mudMap.AdvertisedPrice = offer.AdvertisedPrice;
            mudMap.SaveValue = offer.SaveValue;
            mudMap.UpdatedAt = DateTime.UtcNow;
        }

        public async Task<bool> AddEventOfferSearchHistoryAsync(TEventOfferSearchHistory searchHistory)
        {
            await _context.TEventOfferSearchHistories.AddAsync(searchHistory);
            var result = await _context.SaveChangesAsync();
            return result > 0;
        }

        public async Task<TEventOfferSearchHistory?> GetEventOfferSearchHistoryAsync(int eventId, int pageId, int positionId, int offerId, int offerNumber)
        {
            return await _context.TEventOfferSearchHistories
                .Where(e => e.EventId == eventId && e.PageId == pageId && e.PositionId == positionId && e.EventOfferId == offerId && e.OfferNumber == offerNumber)
                .OrderByDescending(e => e.SearchedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> UpdateEventOfferSearchHistoryAsync(TEventOfferSearchHistory searchHistory)
        {
            _context.TEventOfferSearchHistories.Update(searchHistory);
            var result = await _context.SaveChangesAsync();
            return result > 0;
        }

        public async Task<(List<OfferSkuOverviewDto> Items, int TotalCount)> GetDataViewAsync(DataViewRequestDto request)
        {
            var query = _context.TEventOfferDetails
                .Where(d => d.OfferId == request.EventOfferId && d.OfferNo == request.OfferNo && (d.IsSkuActive == true || d.IsSkuActive == null));

            if (request.SKUs != null && request.SKUs.Any())
            {
                var upperSkus = request.SKUs.Select(s => s.Trim().ToUpper()).ToList();
                query = query.Where(d => upperSkus.Contains(d.Sku.ToUpper()));
            }

            if (request.Brands != null && request.Brands.Any())
            {
                var upperBrands = request.Brands.Select(b => b.Trim().ToUpper()).ToList();
                query = query.Where(d => d.Brand != null && upperBrands.Contains(d.Brand.ToUpper()));
            }

            if (request.PartNumbers != null && request.PartNumbers.Any())
            {
                var upperParts = request.PartNumbers.Select(p => p.Trim().ToUpper()).ToList();
                query = query.Where(d => d.PartNo != null && upperParts.Contains(d.PartNo.ToUpper()));
            }

            if (request.Descriptions != null && request.Descriptions.Any())
            {
                query = query.Where(d => d.Description != null && request.Descriptions.Any(desc => d.Description.Contains(desc)));
            }

            if (request.SortOnEdPriceDesc.HasValue)
            {
                query = request.SortOnEdPriceDesc.Value
                    ? query.OrderByDescending(d => d.EverydayPrice).ThenBy(d => d.Sku)
                    : query.OrderBy(d => d.EverydayPrice).ThenBy(d => d.Sku);
            }
            else if (request.SortOnCatFcstDesc.HasValue)
            {
                query = request.SortOnCatFcstDesc.Value
                    ? query.OrderByDescending(d => d.Categoryforecast).ThenBy(d => d.Sku)
                    : query.OrderBy(d => d.Categoryforecast).ThenBy(d => d.Sku);
            }
            else
            {
                query = query.OrderBy(d => d.Sku);
            }

            int totalCount = await query.CountAsync();
            int pageSize = request.PageSize > 0 ? request.PageSize : 100;
            int pageNumber = request.PageNumber > 0 ? request.PageNumber : 1;

            var details = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = details.Select(d => new OfferSkuOverviewDto
            {
                Sku = d.Sku,
                PartNumber = d.PartNo ?? d.Sku,
                Brand = d.Brand ?? string.Empty,
                Description = d.Description ?? string.Empty,
                EdPrice = d.EverydayPrice ?? 0m,
                EdPriceGst = d.EverydayPriceGst ?? 0m,
                LeCost = d.LeCost ?? 0m,
                NatAvgCost = d.NationalAverageCost ?? 0m,
                CatFcst = d.Categoryforecast ?? 0,
                IncrFcst = d.IncrementalForecast ?? 0,
                EdUnits = d.EverydayUnits ?? 0m,
                FcstCost = d.ForecastCost ?? 0m,
                FcstSales = d.ForecastSales ?? 0m,
                FcstTmValue = d.ForecastTradeMarginDollar ?? 0m,
                FcstTmPercent = d.ForecastTradeMarginPercent ?? 0m,
                SohStore = (int)(d.StockOnHandStore ?? 0m),
                SohDc = (int)(d.StockOnHandDc ?? 0m),
                TotalTieUp = d.TotalTieUp ?? 0,
                TieUpCost = d.TieUpCost ?? 0m,
                ScanSupportValue = d.ScanSupportValue ?? 0m,
                ScanSupportPercent = d.ScanSupportPercent ?? 0m,
                DisplayInd = d.DisplayIndicator ?? true,
                Clearance = d.ClearanceIndicator,
                Showroom = d.ShowroomIndicator,
                isCatFcstLocked = d.IsCategoryForecastLocked,
                isSkuEdited = d.IsSkuEdited,
                isSkuActive = d.IsSkuActive
            }).ToList();

            return (items, totalCount);
        }

        public async Task<bool> UpdateOfferDetailsRangeAsync(List<TEventOfferDetail> details)
        {
            if (details == null || !details.Any()) return true;
            _context.TEventOfferDetails.UpdateRange(details);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> SaveDataViewDetailsAndRecalculateHeaderAsync(List<TEventOfferDetail> details, int offerId, int offerNo, int offerTypeId)
        {
            if (details == null || !details.Any()) return true;

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.TEventOfferDetails.UpdateRange(details);
                await _context.SaveChangesAsync();

                // Recalculate header rollups via SP inside the active transaction
                await CalculateEventOfferHeaderAsync(offerId, offerNo, offerTypeId);

                await tx.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Transaction failed while updating details and recalculating header for OfferId={OfferId}: {Message}", offerId, ex.Message);
                throw;
            }
        }

        public async Task<bool> CalculateDataViewAsync(DataViewRequestDto request)
        {
            var conn = _context.Database.GetDbConnection();
            bool wasClosed = conn.State != System.Data.ConnectionState.Open;
            if (wasClosed)
            {
                await conn.OpenAsync();
            }
            try
            {
                await using var cmd = conn.CreateCommand();
                if (_context.Database.CurrentTransaction != null)
                {
                    cmd.Transaction = _context.Database.CurrentTransaction.GetDbTransaction();
                }
                cmd.CommandText = "CALL public.sp_update_event_offer_detail_header(@p_offer_id, @p_offer_no, @p_offer_type_id, @p_save_percent, @p_incremental_percent, @p_space_purchase, @p_advertised_price_gst, @p_total_multibuy_price, @p_required_qty, @p_spend1, @p_save_pct1, @p_spend2, @p_save_pct2, @p_spend3, @p_save_pct3)";
                cmd.CommandTimeout = 600;

                void AddParam(string name, object? value)
                {
                    var param = cmd.CreateParameter();
                    param.ParameterName = name.StartsWith("@") ? name : "@" + name;
                    param.Value = value ?? DBNull.Value;
                    cmd.Parameters.Add(param);
                }

                AddParam("p_offer_id", request.EventOfferId);
                AddParam("p_offer_no", request.OfferNo);
                AddParam("p_offer_type_id", request.OfferTypeId);
                AddParam("p_save_percent", request.SavePercent);
                AddParam("p_incremental_percent", request.IncrementalPercent);
                AddParam("p_space_purchase", request.SpacePurchase);
                AddParam("p_advertised_price_gst", DBNull.Value);
                AddParam("p_total_multibuy_price", DBNull.Value);
                AddParam("p_required_qty", DBNull.Value);
                AddParam("p_spend1", request.Spend1);
                AddParam("p_save_pct1", request.SavePct1);
                AddParam("p_spend2", request.Spend2);
                AddParam("p_save_pct2", request.SavePct2);
                AddParam("p_spend3", request.Spend3);
                AddParam("p_save_pct3", request.SavePct3);

                await cmd.ExecuteNonQueryAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error while calculating SpendX data view: {Message}", ex.Message);
                return false;
            }
            finally
            {
                if (wasClosed)
                {
                    await conn.CloseAsync();
                }
            }
        }

        public async Task<bool> CalculateEventOfferHeaderAsync(int offerId, int offerNo, int offerTypeId)
        {
            var conn = _context.Database.GetDbConnection();
            bool wasClosed = conn.State != System.Data.ConnectionState.Open;
            if (wasClosed)
            {
                await conn.OpenAsync();
            }
            try
            {
                await using var cmd = conn.CreateCommand();
                if (_context.Database.CurrentTransaction != null)
                {
                    cmd.Transaction = _context.Database.CurrentTransaction.GetDbTransaction();
                }
                cmd.CommandText = "CALL public.sp_update_event_offer_header(@p_offer_id, @p_offer_no, @p_offer_type_id)";
                cmd.CommandTimeout = 600;

                void AddParam(string name, object? value)
                {
                    var param = cmd.CreateParameter();
                    param.ParameterName = name.StartsWith("@") ? name : "@" + name;
                    param.Value = value ?? DBNull.Value;
                    cmd.Parameters.Add(param);
                }

                AddParam("p_offer_id", offerId);
                AddParam("p_offer_no", offerNo);
                AddParam("p_offer_type_id", offerTypeId);

                await cmd.ExecuteNonQueryAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error while calculating SpendX event offer header: {Message}", ex.Message);
                return false;
            }
            finally
            {
                if (wasClosed)
                {
                    await conn.CloseAsync();
                }
            }
        }
    }
}
