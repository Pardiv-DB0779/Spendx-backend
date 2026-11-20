using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GPC.Boost.SpendX.Data;
using GPC.Boost.SpendX.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

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

                    // Replace details or update
                    var existingDetails = await _context.TEventOfferDetails
                        .Where(d => d.OfferId == offer.OfferId && d.OfferNo == offer.OfferNumber)
                        .ToListAsync();
                    _context.TEventOfferDetails.RemoveRange(existingDetails);
                    await _context.SaveChangesAsync();
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

                mudMap.OfferId = offer.OfferId;
                mudMap.OfferNumber = offer.OfferNumber;
                mudMap.OfferName = offer.OfferName;
                mudMap.OfferType = offer.OfferType;
                mudMap.Brand = offer.Brand;
                mudMap.Loyalty = offer.IsRewards ?? false;
                mudMap.SavePercent = offer.SavePct1;
                mudMap.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Recalculate header rollup
                decimal gst = offer.Country.ToUpper() == "NZ" ? 0.15m : 0.10m;
                await RecalculateOfferHeaderInternalAsync(offer.OfferId, offer.OfferNumber, gst);

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

        private async Task<bool> RecalculateOfferHeaderInternalAsync(int offerId, int offerNo, decimal gst)
        {
            try
            {
                var offer = await _context.TEventOffers.FirstOrDefaultAsync(o => o.OfferId == offerId && o.OfferNumber == offerNo);
                if (offer == null) return false;

                var details = await _context.TEventOfferDetails
                    .Where(d => d.OfferId == offerId && d.OfferNo == offerNo && (d.IsSkuActive == true || d.IsSkuActive == null))
                    .ToListAsync();

                if (!details.Any()) return true;

                // BRD Item 1: Calculate Advertised Price based on highest nominal tier discount %
                decimal maxDiscountPct = Math.Max(offer.SavePct1, Math.Max(offer.SavePct2 ?? 0m, offer.SavePct3 ?? 0m));
                if (maxDiscountPct > 0)
                {
                    foreach (var d in details)
                    {
                        if (d.EverydayPrice.HasValue && d.EverydayPrice.Value > 0)
                        {
                            d.AdvertisedPrice = Math.Round(d.EverydayPrice.Value * (1m - (maxDiscountPct / 100m)), 2);
                            d.AdvertisedPriceGst = d.EverydayPriceGst.HasValue 
                                ? Math.Round(d.EverydayPriceGst.Value * (1m - (maxDiscountPct / 100m)), 2) 
                                : Math.Round(d.AdvertisedPrice.Value * (1 + gst), 2);
                            d.CalculatedSavePercentage = maxDiscountPct;
                            d.CalculatedSaveValue = Math.Round(d.EverydayPrice.Value - d.AdvertisedPrice.Value, 2);
                            d.ExtendedAdvertisedPrice = Math.Round((d.ForecastUnits ?? 0) * d.AdvertisedPrice.Value, 2);

                            decimal cost = d.NationalAverageCost ?? d.LeCost ?? 0m;
                            d.ForecastSales = Math.Round((d.ForecastUnits ?? 0) * d.AdvertisedPrice.Value, 2);
                            d.ForecastCost = Math.Round((d.ForecastUnits ?? 0) * cost, 2);
                            d.ForecastTradeMarginDollar = Math.Round((d.ForecastSales ?? 0) - (d.ForecastCost ?? 0), 2);
                            d.ForecastTradeMarginPercent = (d.ForecastSales ?? 0) > 0 ? Math.Round(((d.ForecastTradeMarginDollar ?? 0) / d.ForecastSales!.Value) * 100, 2) : 0m;

                            decimal incUnits = d.IncrementalForecast ?? 0;
                            d.IncrementalSales = Math.Round(incUnits * d.AdvertisedPrice.Value, 2);
                            d.IncrementalTradeDollar = Math.Round(incUnits * (d.AdvertisedPrice.Value - cost), 2);
                        }
                    }
                }

                offer.EverydayUnits = details.Sum(d => d.EverydayUnits ?? 0);
                offer.ForecastUnits = (int)Math.Round(details.Sum(d => d.ForecastUnits ?? 0));
                offer.IncrementalUnits = offer.ForecastUnits - (int)Math.Round(offer.EverydayUnits ?? 0);

                offer.ForecastCost = Math.Round(details.Sum(d => d.ForecastCost ?? 0), 2);
                offer.ForecastSales = Math.Round(details.Sum(d => d.ForecastSales ?? 0), 2);

                // TotalSupplierIncome = SpacePurchase + TotalScanSupport (BRD Item 2)
                decimal scanSupportTotal = details.Sum(d => (d.ScanSupportValue ?? 0) * (d.ForecastUnits ?? 0));
                offer.TotalScanSupportDollar = Math.Round(scanSupportTotal, 2);
                offer.TotalSupplierIncome = Math.Round((offer.SpacePurchase ?? 0) + scanSupportTotal, 2);

                // ForecastTradeMargin$ = (ForecastSales - ForecastCost) + TotalSupplierIncome (BRD Item 2)
                decimal productMargin = (offer.ForecastSales ?? 0) - (offer.ForecastCost ?? 0);
                offer.ForecastTradeMargin = Math.Round(productMargin + (offer.TotalSupplierIncome ?? 0), 2);
                offer.ForecastTradeMarginDollar = offer.ForecastTradeMargin;

                if (offer.ForecastSales > 0)
                {
                    offer.ForecastTmPercent = Math.Round(((offer.ForecastTradeMargin ?? 0) / (offer.ForecastSales ?? 1)) * 100, 2);
                    offer.ForecastTradeMarginPercent = offer.ForecastTmPercent;
                }

                offer.IncrementalSales = details.Sum(d => d.IncrementalSales ?? 0);
                offer.IncrementalSalesDollar = offer.IncrementalSales;
                offer.IncrementalTm = details.Sum(d => d.IncrementalTradeDollar ?? 0);
                offer.IncrementalTmDollar = offer.IncrementalTm;

                var firstDetail = details.FirstOrDefault(d => d.AdvertisedPrice.HasValue && d.AdvertisedPrice.Value > 0) ?? details.FirstOrDefault();
                if (firstDetail != null)
                {
                    offer.EverydayPrice = firstDetail.EverydayPrice;
                    offer.EverydayPriceGst = firstDetail.EverydayPriceGst;
                    offer.AdvertisedPrice = firstDetail.AdvertisedPrice;
                    offer.AdvertisedPriceGst = firstDetail.AdvertisedPriceGst;
                    offer.SaveValue = firstDetail.CalculatedSaveValue;
                    offer.SavePercent = offer.SavePct1;
                    offer.CalculatedSavePercent = firstDetail.CalculatedSavePercentage;
                    offer.CalculatedSaveValue = firstDetail.CalculatedSaveValue;
                }

                offer.UpdatedAt = DateTime.UtcNow;

                // Also keep tmudmap pricing synchronized
                var mudMap = await _context.TMudMaps
                    .FirstOrDefaultAsync(m => m.EventId == offer.EventId && m.PageId == offer.Page && m.PositionId == offer.PagePosition);
                if (mudMap != null)
                {
                    mudMap.EverydayPrice = offer.EverydayPrice;
                    mudMap.AdvertisedPrice = offer.AdvertisedPrice;
                    mudMap.SaveValue = offer.SaveValue;
                    mudMap.SavePercent = offer.SavePct1;
                    mudMap.Loyalty = offer.IsRewards ?? false;
                    mudMap.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RecalculateOfferHeaderInternalAsync: {Message}", ex.Message);
                return false;
            }
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
                    mudMap.OfferName = offer.OfferName;
                    mudMap.OfferType = offer.OfferType;
                    mudMap.Brand = offer.Brand;
                    mudMap.Clearance = offer.IsClearance ?? false;
                    mudMap.New = offer.IsNew ?? false;
                    mudMap.Loyalty = offer.IsRewards ?? false;
                    mudMap.OfferId = offer.OfferId;
                    mudMap.OfferNumber = offer.OfferNumber;
                    mudMap.SavePercent = offer.SavePct1;
                    mudMap.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                // Recalculate header financials in case SpacePurchase or Tier discounts changed (Item 2 & 7)
                decimal gst = offer.Country.ToUpper() == "NZ" ? 0.15m : 0.10m;
                await RecalculateOfferHeaderInternalAsync(offer.OfferId, offer.OfferNumber, gst);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update offer overview: {Message}", ex.Message);
                return false;
            }
        }
    }
}
