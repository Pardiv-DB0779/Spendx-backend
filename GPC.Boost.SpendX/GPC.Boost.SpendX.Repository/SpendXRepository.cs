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
        Task<(string? PartNumber, decimal StoreSOH, decimal DcSOH)> GetOfferDetailSummaryAsync(int offerId, int offerNo = 1);
        Task<bool> UpdateOfferHeaderOverviewAsync(TEventOffer offer);
        Task<bool> AddEventOfferSearchHistoryAsync(TEventOfferSearchHistory searchHistory);
        Task<TEventOfferSearchHistory?> GetEventOfferSearchHistoryAsync(int eventId, int pageId, int positionId, int offerId, int offerNumber);
        Task<bool> UpdateEventOfferSearchHistoryAsync(TEventOfferSearchHistory searchHistory);
        Task<(List<OfferSkuOverviewDto> Items, int TotalCount)> GetDataViewAsync(DataViewRequestDto request);
        Task<bool> UpdateOfferDetailsRangeAsync(List<TEventOfferDetail> details);
        Task<bool> SaveDataViewDetailsAndRecalculateHeaderAsync(List<TEventOfferDetail> details, int offerId, int offerNo, int offerTypeId);
        Task<bool> CalculateDataViewAsync(DataViewRequestDto request);
        Task<bool> CalculateEventOfferHeaderAsync(int offerId, int offerNo, int offerTypeId);
        Task<bool> CalculateOfferDetailAndHeaderAsync(TEventOffer offer);
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

                // Recalculate detail lines and header rollups via stored procedure inside transaction
                if (details != null && details.Any())
                {
                    await CalculateOfferDetailAndHeaderAsync(offer);
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

        public async Task<(string? PartNumber, decimal StoreSOH, decimal DcSOH)> GetOfferDetailSummaryAsync(int offerId, int offerNo = 1)
        {
            await using var conn = new NpgsqlConnection(_context.Database.GetConnectionString());
            await conn.OpenAsync();

            const string sql = @"
                SELECT 
                    MIN(""partNo"") AS partNumber,
                    COALESCE(SUM(""stockOnHandStore""), 0) AS totalStore,
                    COALESCE(SUM(""stockOnHandDc""), 0) AS totalDC
                FROM public.""teventofferdetail""
                WHERE ""offerId"" = @offerId
                  AND ""offerNo"" = @offerNo
                  AND (""isSkuActive"" = TRUE OR ""isSkuActive"" IS NULL);";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("offerId", offerId);
            cmd.Parameters.AddWithValue("offerNo", offerNo);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var partNo = reader.IsDBNull(0) ? null : reader.GetString(0);
                var storeSoh = reader.IsDBNull(1) ? 0m : Convert.ToDecimal(reader.GetValue(1));
                var dcSoh = reader.IsDBNull(2) ? 0m : Convert.ToDecimal(reader.GetValue(2));
                return (partNo, storeSoh, dcSoh);
            }

            return (null, 0m, 0m);
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

                // Recalculate detail lines and header financials via stored procedure
                await CalculateOfferDetailAndHeaderAsync(offer);

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
            var items = new List<OfferSkuOverviewDto>();
            int totalCount = 0;

            var connStr = _context.Database.GetConnectionString();
            await using var conn = new NpgsqlConnection(connStr);
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(@"
                SELECT * FROM public.fn_get_spendx_dataview(
                    @p_offer_id,
                    @p_offer_no,
                    @p_skus,
                    @p_part_numbers,
                    @p_brands,
                    @p_part_descriptions,
                    @p_sort_edprice_desc,
                    @p_sort_catfcst_desc,
                    @p_page_number,
                    @p_page_size
                )", conn);

            cmd.Parameters.AddWithValue("p_offer_id", request.EventOfferId);
            cmd.Parameters.AddWithValue("p_offer_no", request.OfferNo);
            cmd.Parameters.AddWithValue("p_skus", (object?)request.SKUs?.ToArray() ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p_part_numbers", (object?)request.PartNumbers?.ToArray() ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p_brands", (object?)request.Brands?.ToArray() ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p_part_descriptions", (object?)request.Descriptions?.ToArray() ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p_sort_edprice_desc", (object?)request.SortOnEdPriceDesc ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p_sort_catfcst_desc", (object?)request.SortOnCatFcstDesc ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p_page_number", request.PageNumber > 0 ? request.PageNumber : 1);
            cmd.Parameters.AddWithValue("p_page_size", request.PageSize > 0 ? request.PageSize : 100);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                if (totalCount == 0 && !reader.IsDBNull(reader.GetOrdinal("total_count")))
                {
                    totalCount = reader.GetInt32(reader.GetOrdinal("total_count"));
                }

                items.Add(new OfferSkuOverviewDto
                {
                    Sku = reader.IsDBNull(reader.GetOrdinal("sku")) ? string.Empty : reader.GetString(reader.GetOrdinal("sku")),
                    PartNumber = reader.IsDBNull(reader.GetOrdinal("part_number")) ? string.Empty : reader.GetString(reader.GetOrdinal("part_number")),
                    Brand = reader.IsDBNull(reader.GetOrdinal("brand")) ? string.Empty : reader.GetString(reader.GetOrdinal("brand")),
                    Description = reader.IsDBNull(reader.GetOrdinal("description")) ? string.Empty : reader.GetString(reader.GetOrdinal("description")),
                    EdPrice = reader.IsDBNull(reader.GetOrdinal("edprice")) ? 0m : reader.GetDecimal(reader.GetOrdinal("edprice")),
                    EdPriceGst = reader.IsDBNull(reader.GetOrdinal("edpricegst")) ? 0m : reader.GetDecimal(reader.GetOrdinal("edpricegst")),
                    LeCost = reader.IsDBNull(reader.GetOrdinal("lecost")) ? 0m : reader.GetDecimal(reader.GetOrdinal("lecost")),
                    NatAvgCost = reader.IsDBNull(reader.GetOrdinal("natavgcost")) ? 0m : reader.GetDecimal(reader.GetOrdinal("natavgcost")),
                    CatFcst = reader.IsDBNull(reader.GetOrdinal("catfcst")) ? 0 : reader.GetInt32(reader.GetOrdinal("catfcst")),
                    IncrFcst = reader.IsDBNull(reader.GetOrdinal("incrfcst")) ? 0 : reader.GetInt32(reader.GetOrdinal("incrfcst")),
                    EdUnits = reader.IsDBNull(reader.GetOrdinal("edunits")) ? 0m : reader.GetDecimal(reader.GetOrdinal("edunits")),
                    FcstCost = reader.IsDBNull(reader.GetOrdinal("fcstcost")) ? 0m : reader.GetDecimal(reader.GetOrdinal("fcstcost")),
                    FcstSales = reader.IsDBNull(reader.GetOrdinal("fcstsales")) ? 0m : reader.GetDecimal(reader.GetOrdinal("fcstsales")),
                    FcstTmValue = reader.IsDBNull(reader.GetOrdinal("fcsttmvalue")) ? 0m : reader.GetDecimal(reader.GetOrdinal("fcsttmvalue")),
                    FcstTmPercent = reader.IsDBNull(reader.GetOrdinal("fcsttmpercent")) ? 0m : reader.GetDecimal(reader.GetOrdinal("fcsttmpercent")),
                    SohStore = reader.IsDBNull(reader.GetOrdinal("sohstore")) ? 0 : reader.GetInt32(reader.GetOrdinal("sohstore")),
                    SohDc = reader.IsDBNull(reader.GetOrdinal("sohdc")) ? 0 : reader.GetInt32(reader.GetOrdinal("sohdc")),
                    TotalTieUp = reader.IsDBNull(reader.GetOrdinal("totaltieup")) ? 0 : reader.GetInt32(reader.GetOrdinal("totaltieup")),
                    TieUpCost = reader.IsDBNull(reader.GetOrdinal("tieupcost")) ? 0m : reader.GetDecimal(reader.GetOrdinal("tieupcost")),
                    ScanSupportValue = reader.IsDBNull(reader.GetOrdinal("scansupportvalue")) ? 0m : reader.GetDecimal(reader.GetOrdinal("scansupportvalue")),
                    ScanSupportPercent = reader.IsDBNull(reader.GetOrdinal("scansupportpercent")) ? 0m : reader.GetDecimal(reader.GetOrdinal("scansupportpercent")),
                    DisplayInd = !reader.IsDBNull(reader.GetOrdinal("displayind")) && reader.GetBoolean(reader.GetOrdinal("displayind")),
                    Clearance = reader.IsDBNull(reader.GetOrdinal("clearance")) ? null : reader.GetString(reader.GetOrdinal("clearance")),
                    Showroom = reader.IsDBNull(reader.GetOrdinal("showroom")) ? null : reader.GetString(reader.GetOrdinal("showroom")),
                    isCatFcstLocked = reader.IsDBNull(reader.GetOrdinal("iscatfcstlocked")) ? null : reader.GetBoolean(reader.GetOrdinal("iscatfcstlocked")),
                    isSkuEdited = reader.IsDBNull(reader.GetOrdinal("isskuedited")) ? null : reader.GetBoolean(reader.GetOrdinal("isskuedited")),
                    isSkuActive = reader.IsDBNull(reader.GetOrdinal("isskuactive")) ? null : reader.GetBoolean(reader.GetOrdinal("isskuactive"))
                });
            }

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
            const string sql = "CALL public.sp_update_event_offer_detail_header(@p_offer_id, @p_offer_no, @p_offer_type_id, @p_save_percent, @p_incremental_percent, @p_space_purchase, @p_advertised_price_gst, @p_total_multibuy_price, @p_required_qty, @p_spend1, @p_save_pct1, @p_spend2, @p_save_pct2, @p_spend3, @p_save_pct3)";

            var parameters = new Dictionary<string, object?>
            {
                ["p_offer_id"] = request.EventOfferId,
                ["p_offer_no"] = request.OfferNo,
                ["p_offer_type_id"] = request.OfferTypeId,
                ["p_save_percent"] = request.SavePercent,
                ["p_incremental_percent"] = request.IncrementalPercent,
                ["p_space_purchase"] = request.SpacePurchase,
                ["p_advertised_price_gst"] = DBNull.Value,
                ["p_total_multibuy_price"] = DBNull.Value,
                ["p_required_qty"] = DBNull.Value,
                ["p_spend1"] = request.Spend1,
                ["p_save_pct1"] = request.SavePct1,
                ["p_spend2"] = request.Spend2,
                ["p_save_pct2"] = request.SavePct2,
                ["p_spend3"] = request.Spend3,
                ["p_save_pct3"] = request.SavePct3
            };

            return await ExecuteStoredProcedureAsync(sql, parameters, "SpendX data view calculation");
        }

        public async Task<bool> CalculateEventOfferHeaderAsync(int offerId, int offerNo, int offerTypeId)
        {
            const string sql = "CALL public.sp_update_event_offer_header(@p_offer_id, @p_offer_no, @p_offer_type_id)";

            var parameters = new Dictionary<string, object?>
            {
                ["p_offer_id"] = offerId,
                ["p_offer_no"] = offerNo,
                ["p_offer_type_id"] = offerTypeId
            };

            return await ExecuteStoredProcedureAsync(sql, parameters, "SpendX event offer header calculation");
        }

        public async Task<bool> CalculateOfferDetailAndHeaderAsync(TEventOffer offer)
        {
            const string sql = "CALL public.sp_update_event_offer_detail_header(@p_offer_id, @p_offer_no, @p_offer_type_id, @p_save_percent, @p_incremental_percent, @p_space_purchase, @p_advertised_price_gst, @p_total_multibuy_price, @p_required_qty, @p_spend1, @p_save_pct1, @p_spend2, @p_save_pct2, @p_spend3, @p_save_pct3)";

            var parameters = new Dictionary<string, object?>
            {
                ["p_offer_id"] = offer.OfferId,
                ["p_offer_no"] = offer.OfferNumber,
                ["p_offer_type_id"] = offer.OfferTypeId,
                ["p_save_percent"] = offer.SavePercent ?? offer.SavePct1,
                ["p_incremental_percent"] = offer.IncrementalPercentage,
                ["p_space_purchase"] = offer.SpacePurchase,
                ["p_advertised_price_gst"] = DBNull.Value,
                ["p_total_multibuy_price"] = DBNull.Value,
                ["p_required_qty"] = DBNull.Value,
                ["p_spend1"] = offer.Spend1,
                ["p_save_pct1"] = offer.SavePct1,
                ["p_spend2"] = offer.Spend2,
                ["p_save_pct2"] = offer.SavePct2,
                ["p_spend3"] = offer.Spend3,
                ["p_save_pct3"] = offer.SavePct3
            };

            return await ExecuteStoredProcedureAsync(sql, parameters, "SpendX detail and header calculation");
        }

        private async Task<bool> ExecuteStoredProcedureAsync(string commandText, IDictionary<string, object?> parameters, string operationName)
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
                cmd.CommandText = commandText;
                cmd.CommandTimeout = 600;

                foreach (var (name, value) in parameters)
                {
                    var param = cmd.CreateParameter();
                    param.ParameterName = name.StartsWith("@") ? name : "@" + name;
                    param.Value = value ?? DBNull.Value;
                    cmd.Parameters.Add(param);
                }

                await cmd.ExecuteNonQueryAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error while executing {Operation}: {Message}", operationName, ex.Message);
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
