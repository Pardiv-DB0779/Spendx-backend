using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GPC.Boost.SpendX.Domain.Enums;
using GPC.Boost.SpendX.Domain.Models;
using GPC.Boost.SpendX.Dto.RequestDto;
using GPC.Boost.SpendX.Dto.ResponseDto;
using GPC.Boost.SpendX.Repository;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace GPC.Boost.SpendX.Service
{
    public interface ISpendXService
    {
        Task<CreateEventOfferResponseDto> AddEventOfferAsync(EventOfferRequestDto request);
        Task<SpendXOfferDto> GetEventOfferAsync(GetEventOfferRequestDto request);
        Task<SpendXOverviewSaveResponseDto> SaveEventOfferOverviewAsync(SpendXOverviewRequestDto request);
        Task<List<ProductSearchResultDto>> SearchProductsByBrandAsync(string brand, string country);
        Task<DataViewResponseDto> GetDataViewAsync(DataViewRequestDto request);
        Task<DataviewCalculationsResponseDto> PostDataViewAsync(DataViewResponseDto request);
        Task<MoveOfferResponseDto> MoveOfferAsync(MoveOfferRequestDto request);
        Task<PricingExtractResponseDto> GetPricingExtractAsync(int eventId, string? userId);
    }

    public class SpendXService : ISpendXService
    {
        private readonly ISpendXRepository _repository;
        private readonly ILogger<SpendXService> _logger;

        public SpendXService(ISpendXRepository repository, ILogger<SpendXService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<CreateEventOfferResponseDto> AddEventOfferAsync(EventOfferRequestDto request)
        {
            var response = new CreateEventOfferResponseDto();
            try
            {
                if (request == null)
                {
                    response.Error = new ErrorDto { ErrorCode = "INVALID_REQUEST", Message = "Request payload is null." };
                    return response;
                }

                var brands = new List<string>();
                if (!string.IsNullOrWhiteSpace(request.ProductBrand))
                {
                    brands.Add(request.ProductBrand.Trim());
                }
                if (request.Search?.ProductBrand != null)
                {
                    foreach (var b in request.Search.ProductBrand)
                    {
                        if (!string.IsNullOrWhiteSpace(b) && !brands.Any(x => string.Equals(x, b.Trim(), StringComparison.OrdinalIgnoreCase)))
                        {
                            brands.Add(b.Trim());
                        }
                    }
                }

                var brandDisplay = brands.Any() ? string.Join(", ", brands) : string.Empty;
                var country = !string.IsNullOrWhiteSpace(request.Country) ? request.Country : "AU";

                if (!brands.Any())
                {
                    response.Error = new ErrorDto
                    {
                        ErrorCode = "INVALID_REQUEST",
                        Message = "At least one product brand must be specified (via ProductBrand or Search.ProductBrand)."
                    };
                    return response;
                }

                _logger.LogInformation("AddEventOfferAsync called for Brands={Brands}, Country={Country}, OfferId={OfferId}", brandDisplay, country, request.OfferId);

                // 1. Fetch eligible products from tproducts based on brands and optional ItemClass4
                var brandProducts = await _repository.GetProductsByBrandsAsync(brands, country, request.Search?.ItemClass4);
                if (!brandProducts.Any())
                {
                    response.Error = new ErrorDto
                    {
                        ErrorCode = "NO_PRODUCTS_FOUND",
                        Message = $"No active products found for brand(s) '{brandDisplay}' in country '{country}'."
                    };
                    return response;
                }

                // If explicit SKUs were passed in request (SelectedSKUs, Details list, or Search.SKUs), filter
                var eligibleProducts = brandProducts;
                var explicitSkus = request.SelectedSKUs 
                    ?? request.Details?.Where(d => !string.IsNullOrWhiteSpace(d.SKU)).Select(d => d.SKU!).ToList()
                    ?? request.Search?.SKUs;

                if (explicitSkus != null && explicitSkus.Any())
                {
                    var selectedSet = explicitSkus.ToHashSet(StringComparer.OrdinalIgnoreCase);
                    eligibleProducts = brandProducts.Where(p => selectedSet.Contains(p.Sku)).ToList();
                }

                // Remove any unselected SKUs (from root or Search.UnSelectedSKUs)
                var unselectedList = (request.UnSelectedSKUs != null && request.UnSelectedSKUs.Any())
                    ? request.UnSelectedSKUs
                    : request.Search?.UnSelectedSKUs;

                if (unselectedList != null && unselectedList.Any())
                {
                    var unselectedSet = unselectedList.ToHashSet(StringComparer.OrdinalIgnoreCase);
                    eligibleProducts = eligibleProducts.Where(p => !unselectedSet.Contains(p.Sku)).ToList();
                }

                if (!eligibleProducts.Any())
                {
                    response.Error = new ErrorDto
                    {
                        ErrorCode = "NO_ELIGIBLE_SKUS",
                        Message = "No eligible SKUs remaining after applying brand and selection filters."
                    };
                    return response;
                }

                // 2. Check if update or create
                bool isUpdate = request.OfferId.HasValue && request.OfferId.Value > 0;
                TEventOffer offer;

                if (isUpdate)
                {
                    var existingOffer = await _repository.GetOfferByIdAsync(request.OfferId!.Value, request.OfferNo);
                    offer = existingOffer ?? new TEventOffer { OfferId = request.OfferId.Value };
                }
                else
                {
                    offer = new TEventOffer();
                }

                // Populate offer header & handle Loyalty Variant (Item 5)
                bool isLoyal = request.OfferTypeId == (int)OfferType.SpendXGetYLoyal || request.IsRewards;
                offer.EventId = request.EventId;
                offer.OfferNumber = request.OfferNo > 0 ? request.OfferNo : 1;
                offer.OfferTypeId = isLoyal ? (int)OfferType.SpendXGetYLoyal : (int)OfferType.SpendXGetY;
                offer.OfferType = isLoyal ? "SPEND X GET Y% OFF - LOYAL" : "SPEND X GET Y% OFF";
                offer.CommercialOfferType = offer.OfferType;

                string offerName = !string.IsNullOrWhiteSpace(request.OfferName) ? request.OfferName.Trim() : $"{brandDisplay} Spend & Save";
                if (isLoyal)
                {
                    if (!offerName.EndsWith(" - Loyal", StringComparison.OrdinalIgnoreCase))
                    {
                        offerName += " - Loyal";
                    }
                }
                else
                {
                    if (offerName.EndsWith(" - Loyal", StringComparison.OrdinalIgnoreCase))
                    {
                        offerName = offerName.Substring(0, offerName.Length - " - Loyal".Length).Trim();
                    }
                }
                offer.OfferName = offerName;
                offer.OfferDescription = request.OfferDescription;
                offer.Brand = brandDisplay;
                offer.Country = country;
                offer.IsRewards = isLoyal;
                offer.Page = request.PageNumber;
                offer.PagePosition = request.PositionId;

                // SpendX multi-tier logic: Tier 2 and Tier 3 require IsTiered == true
                bool isTiered = request.IsTiered ?? (request.Spend2.HasValue && request.Spend2.Value > 0);
                offer.Spend1 = request.Spend1;
                offer.SavePct1 = request.SavePct1;
                if (isTiered)
                {
                    offer.Spend2 = request.Spend2;
                    offer.SavePct2 = request.SavePct2;
                    offer.Spend3 = request.Spend3;
                    offer.SavePct3 = request.SavePct3;
                }
                else
                {
                    offer.Spend2 = null;
                    offer.SavePct2 = null;
                    offer.Spend3 = null;
                    offer.SavePct3 = null;
                }

                offer.IncrementalPercentage = request.IncrementalPercentage;
                offer.SpacePurchase = request.SpacePurchase;
                offer.UpdatedBy = request.UserId;
                offer.UpdatedAt = DateTime.UtcNow;

                // 3. Populate teventofferdetail records copying attributes from tproducts (matching PROD AutoMapper pattern; calculations done in stored procedure)
                var details = eligibleProducts.Select(p => new TEventOfferDetail
                {
                    Sku = p.Sku,
                    PartNo = p.PartNo ?? p.Sku,
                    Description = p.Description,
                    Brand = p.Brand,
                    ItemClass1 = p.ItemClass1,
                    ItemClass2 = p.ItemClass2,
                    ItemClass3 = p.ItemClass3,
                    ItemClass4 = p.ItemClass4,
                    EverydayPrice = p.EverydayPrice,
                    EverydayPriceGst = p.EverydayPriceGst,
                    LeCost = p.LeCost,
                    LatestEffectiveCost = p.LeCost,
                    NationalAverageCost = p.NationalAverageCost ?? p.LeCost,
                    CategoryCost = p.NationalAverageCost ?? p.LeCost,
                    VendorCostPerEach = p.VendorCostPerEach,
                    StockOnHandStore = p.StockOnHandStore,
                    StockOnHandDc = p.StockOnHandDc,
                    ClearanceIndicator = p.Clearance,
                    G0 = p.G0,
                    G1 = p.G1,
                    G2 = p.G2,
                    G3 = p.G3,
                    G4 = p.G4,
                    G5 = p.G5,
                    PricePoint6 = p.PricePoint6,
                    PricePoint6IncludingGst = p.PricePoint6IncludingGst,
                    IsSkuActive = true,
                    IsSkuEdited = false,
                    DisplayIndicator = true,
                    Page = request.PageNumber,
                    PagePosition = request.PositionId
                }).ToList();

                // 4. Save to Repository (Pass isReserved flag only if user explicitly requested)
                var (success, offerId) = await _repository.SaveOrUpdateSpendXOfferAsync(offer, details, isUpdate, request.IsReserved);
                if (success)
                {
                    // Save or update search history in tEventOfferSearchHistory
                    if (request.Search != null)
                    {
                        try
                        {
                            var existingHistory = await _repository.GetEventOfferSearchHistoryAsync(
                                request.EventId, request.PageNumber, request.PositionId, offerId, offer.OfferNumber);

                            if (existingHistory != null)
                            {
                                existingHistory.SearchJson = JsonConvert.SerializeObject(request.Search);
                                existingHistory.SearchedAt = DateTime.UtcNow;
                                await _repository.UpdateEventOfferSearchHistoryAsync(existingHistory);
                            }
                            else
                            {
                                var newHistory = new TEventOfferSearchHistory
                                {
                                    EventId = request.EventId,
                                    PageId = request.PageNumber,
                                    PositionId = request.PositionId,
                                    EventOfferId = offerId,
                                    OfferNumber = offer.OfferNumber,
                                    SearchJson = JsonConvert.SerializeObject(request.Search),
                                    UserId = request.UserId,
                                    Country = country,
                                    SearchedAt = DateTime.UtcNow
                                };
                                await _repository.AddEventOfferSearchHistoryAsync(newHistory);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to persist search history for OfferId={OfferId}", offerId);
                        }
                    }

                    response.OfferId = offerId;
                    response.OfferNo = offer.OfferNumber;
                    response.SkuCount = details.Count;
                    response.Message = !isUpdate
                        ? $"Event Offer Created Successfully with Offer ID: {offerId} Offer No: {offer.OfferNumber}"
                        : $"Event Offer updated Successfully with Offer ID: {offerId} Offer No: {offer.OfferNumber}";
                    return response;
                }

                response.Error = new ErrorDto { ErrorCode = "SAVE_FAILED", Message = "Failed to persist offer in database." };
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in AddEventOfferAsync: {Message}", ex.Message);
                response.Error = new ErrorDto { ErrorCode = "INTERNAL_ERROR", Message = "An unexpected error occurred while processing the event offer." };
                return response;
            }
        }



        public async Task<SpendXOfferDto> GetEventOfferAsync(GetEventOfferRequestDto request)
        {
            var response = new SpendXOfferDto();
            try
            {
                if (request == null)
                {
                    response.Error = new ErrorDto { ErrorCode = "INVALID_REQUEST", Message = "Request payload is null." };
                    return response;
                }

                TEventOffer? offer = null;
                if (request.OfferId.HasValue && request.OfferId.Value > 0)
                {
                    offer = await _repository.GetOfferByIdAsync(request.OfferId.Value, request.OfferNo ?? 1);
                }
                else if (request.PageId > 0 && request.PagePosition > 0)
                {
                    offer = await _repository.GetOfferByPositionAsync(request.EventId, request.PageId, request.PagePosition);
                }

                if (offer == null)
                {
                    response.Error = new ErrorDto { ErrorCode = "NOT_FOUND", Message = $"SpendX Offer (OfferId: {request.OfferId}, Position: {request.PagePosition}) not found." };
                    return response;
                }

                var (repPartNo, storeSoh, dcSoh) = await _repository.GetOfferDetailSummaryAsync(offer.OfferId, offer.OfferNumber);

                decimal gst = GetGstRate(offer.Country);
                bool isTiered = offer.Spend2.HasValue && offer.Spend2.Value > 0;

                response.OfferId = offer.OfferId;
                response.OfferNo = offer.OfferNumber;
                response.PageId = offer.Page;
                response.PositionId = offer.PagePosition;
                response.Country = offer.Country;
                response.GST = gst;

                // Overview
                response.Overview = new SpendXOverviewDto
                {
                    OfferType = offer.OfferType,
                    OfferName = offer.OfferName,
                    Brand = offer.Brand,
                    IsRewards = offer.IsRewards ?? false,
                    IsTiered = isTiered,
                    RepresentativePartNumber = repPartNo,
                    Spend1 = offer.Spend1,
                    SavePct1 = offer.SavePct1,
                    Spend2 = isTiered ? offer.Spend2 : null,
                    SavePct2 = isTiered ? offer.SavePct2 : null,
                    Spend3 = isTiered ? offer.Spend3 : null,
                    SavePct3 = isTiered ? offer.SavePct3 : null,
                    ForecastUnits = offer.ForecastUnits ?? 0,
                    IncrementalUnits = offer.IncrementalUnits ?? 0,
                    ForecastCost = offer.ForecastCost ?? 0,
                    ForecastSales = offer.ForecastSales ?? 0,
                    ForecastTmDollar = offer.ForecastTradeMargin ?? 0,
                    ForecastTmPercent = offer.ForecastTmPercent ?? 0,
                    TotalSupplierIncome = offer.TotalSupplierIncome ?? 0
                };

                // Tiers
                response.Tiers = new List<SpendXTierDto>
                {
                    new SpendXTierDto { TierNumber = 1, SpendThreshold = offer.Spend1, DiscountPercent = offer.SavePct1 }
                };
                if (isTiered && offer.Spend2.HasValue && offer.SavePct2.HasValue)
                {
                    response.Tiers.Add(new SpendXTierDto { TierNumber = 2, SpendThreshold = offer.Spend2.Value, DiscountPercent = offer.SavePct2.Value });
                }
                if (isTiered && offer.Spend3.HasValue && offer.SavePct3.HasValue)
                {
                    response.Tiers.Add(new SpendXTierDto { TierNumber = 3, SpendThreshold = offer.Spend3.Value, DiscountPercent = offer.SavePct3.Value });
                }

                // Forecasting
                response.Forecasting = new SpendXForecastingDto
                {
                    ForecastUnits = offer.ForecastUnits ?? 0,
                    IncrementalUnits = offer.IncrementalUnits ?? 0,
                    EverydayUnits = offer.EverydayUnits ?? 0,
                    StoreSOH = storeSoh,
                    DcSOH = dcSoh
                };

                // Pricing
                response.Pricing = new SpendXPricingDto
                {
                    IsTiered = isTiered,
                    Spend1 = offer.Spend1,
                    SavePct1 = offer.SavePct1,
                    Spend2 = isTiered ? offer.Spend2 : null,
                    SavePct2 = isTiered ? offer.SavePct2 : null,
                    Spend3 = isTiered ? offer.Spend3 : null,
                    SavePct3 = isTiered ? offer.SavePct3 : null,
                    SpacePurchase = offer.SpacePurchase,
                    ForecastCost = offer.ForecastCost ?? 0,
                    ForecastSales = offer.ForecastSales ?? 0,
                    ForecastTmDollar = offer.ForecastTradeMargin ?? 0,
                    ForecastTmPercent = offer.ForecastTmPercent ?? 0
                };

                // Merchandising Callouts
                response.OfferInfo = new SpendXOfferInfoDto
                {
                    Clearance = offer.IsClearance ?? false,
                    Exclusive = offer.IsExclusive ?? false,
                    New = offer.IsNew ?? false,
                    LowestPrice = offer.IsLowestPrice ?? false,
                    LimitedQuantity = (offer.IsLimitQuantity ?? 0) > 0,
                    OnlineOnly = offer.IsOnlineOnly ?? false,
                    NotAvailOnline = offer.IsNotAvailableOnline ?? false,
                    NoRainCheck = offer.IsRaincheck ?? false,
                    OrderYoursToday = offer.IsOrderYoursToday ?? false,
                    NotAvailableAllStores = offer.IsNotAvailableAllStores ?? false,
                    LimitedStoreStock = offer.IsLimitedStoreStock ?? false,
                    WhileStocksLast = offer.IsWhileStockLast ?? false,
                    StoreStockOnly = offer.IsStoreStockOnly ?? false,
                    ImageReference = offer.ImageReference,
                    CopyReference = offer.CopyReference,
                    Other = offer.OtherDetails
                };

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetEventOfferAsync: {Message}", ex.Message);
                response.Error = new ErrorDto { ErrorCode = "INTERNAL_ERROR", Message = ex.Message };
                return response;
            }
        }

        public async Task<SpendXOverviewSaveResponseDto> SaveEventOfferOverviewAsync(SpendXOverviewRequestDto request)
        {
            var response = new SpendXOverviewSaveResponseDto();
            try
            {
                if (request == null)
                {
                    response.Error = new ErrorDto { ErrorCode = "INVALID_REQUEST", Message = "Request payload is null." };
                    return response;
                }

                var offer = await _repository.GetOfferByIdAsync(request.OfferId, request.OfferNo);
                if (offer == null)
                {
                    response.Error = new ErrorDto { ErrorCode = "NOT_FOUND", Message = $"Offer {request.OfferId} not found." };
                    return response;
                }

                // Item 5: Loyalty Variant Swap & Auto-Naming (Respect user toggle)
                bool isLoyal = request.Overview.IsRewards;
                offer.IsRewards = isLoyal;
                offer.OfferTypeId = isLoyal ? (int)OfferType.SpendXGetYLoyal : (int)OfferType.SpendXGetY;
                offer.OfferType = isLoyal ? "SPEND X GET Y% OFF - LOYAL" : "SPEND X GET Y% OFF";
                offer.CommercialOfferType = offer.OfferType;

                string offerName = !string.IsNullOrWhiteSpace(request.Overview.OfferName) 
                    ? request.Overview.OfferName.Trim() 
                    : (offer.OfferName ?? "Spend & Save");

                if (isLoyal)
                {
                    if (!offerName.EndsWith(" - Loyal", StringComparison.OrdinalIgnoreCase))
                    {
                        offerName += " - Loyal";
                    }
                }
                else
                {
                    if (offerName.EndsWith(" - Loyal", StringComparison.OrdinalIgnoreCase))
                    {
                        offerName = offerName.Substring(0, offerName.Length - " - Loyal".Length).Trim();
                    }
                }
                offer.OfferName = offerName;

                // Overview & Tiers (Respect IsTiered toggle: if false, clear Tiers 2 & 3)
                bool isTiered = request.Overview.IsTiered ?? (request.Overview.Spend2.HasValue && request.Overview.Spend2.Value > 0);
                offer.Spend1 = request.Overview.Spend1;
                offer.SavePct1 = request.Overview.SavePct1;
                if (isTiered)
                {
                    offer.Spend2 = request.Overview.Spend2;
                    offer.SavePct2 = request.Overview.SavePct2;
                    offer.Spend3 = request.Overview.Spend3;
                    offer.SavePct3 = request.Overview.SavePct3;
                }
                else
                {
                    offer.Spend2 = null;
                    offer.SavePct2 = null;
                    offer.Spend3 = null;
                    offer.SavePct3 = null;
                }

                // Pricing overrides
                if (request.Pricing?.SpacePurchase.HasValue == true)
                {
                    offer.SpacePurchase = request.Pricing.SpacePurchase;
                }

                // Promo Flags
                if (request.OfferInfo != null)
                {
                    offer.IsClearance = request.OfferInfo.Clearance;
                    offer.IsExclusive = request.OfferInfo.Exclusive;
                    offer.IsNew = request.OfferInfo.New;
                    offer.IsLowestPrice = request.OfferInfo.LowestPrice;
                    offer.IsLimitQuantity = request.OfferInfo.LimitedQuantity ? 1 : 0;
                    offer.IsOnlineOnly = request.OfferInfo.OnlineOnly;
                    offer.IsNotAvailableOnline = request.OfferInfo.NotAvailOnline;
                    offer.IsRaincheck = request.OfferInfo.NoRainCheck;
                    offer.IsOrderYoursToday = request.OfferInfo.OrderYoursToday;
                    offer.IsNotAvailableAllStores = request.OfferInfo.NotAvailableAllStores;
                    offer.IsLimitedStoreStock = request.OfferInfo.LimitedStoreStock;
                    offer.IsWhileStockLast = request.OfferInfo.WhileStocksLast;
                    offer.IsStoreStockOnly = request.OfferInfo.StoreStockOnly;
                    offer.ImageReference = request.OfferInfo.ImageReference;
                    offer.CopyReference = request.OfferInfo.CopyReference;
                    offer.OtherDetails = request.OfferInfo.Other;
                }

                offer.Page = request.PageId != 0 ? request.PageId : offer.Page;
                offer.PagePosition = request.PositionId != 0 ? request.PositionId : offer.PagePosition;
                offer.UpdatedBy = request.UserId;
                offer.UpdatedAt = DateTime.UtcNow;

                bool success = await _repository.UpdateOfferHeaderOverviewAsync(offer);
                if (success)
                {
                    // Item 7: Return updated financial rollup directly
                    var updatedOffer = await _repository.GetOfferByIdAsync(request.OfferId, request.OfferNo);
                    return new SpendXOverviewSaveResponseDto
                    {
                        Message = "SpendX offer overview and merchandising flags saved successfully.",
                        ForecastSales = updatedOffer?.ForecastSales ?? 0m,
                        ForecastCost = updatedOffer?.ForecastCost ?? 0m,
                        ForecastTmDollar = updatedOffer?.ForecastTradeMargin ?? 0m,
                        ForecastTmPercent = updatedOffer?.ForecastTmPercent ?? 0m,
                        TotalSupplierIncome = updatedOffer?.TotalSupplierIncome ?? 0m
                    };
                }

                response.Error = new ErrorDto { ErrorCode = "SAVE_FAILED", Message = "Failed to update offer overview." };
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SaveEventOfferOverviewAsync: {Message}", ex.Message);
                response.Error = new ErrorDto { ErrorCode = "INTERNAL_ERROR", Message = ex.Message };
                return response;
            }
        }

        public async Task<List<ProductSearchResultDto>> SearchProductsByBrandAsync(string brand, string country)
        {
            var brandList = brand.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
            var products = await _repository.GetProductsByBrandsAsync(brandList, country, null);

            return products.Select(p => new ProductSearchResultDto
            {
                Sku = p.Sku,
                PartNumber = p.PartNo ?? p.Sku,
                Description = p.Description ?? string.Empty,
                Brand = p.Brand ?? string.Empty,
                Country = p.Country,
                EverydayPrice = p.EverydayPrice ?? 0m,
                EverydayPriceGst = p.EverydayPriceGst ?? 0m,
                ItemClass1 = p.ItemClass1,
                ItemClass4 = p.ItemClass4,
                AverageMonthlySales = p.AverageMonthlySales,
                StockOnHandStore = p.StockOnHandStore,
                StockOnHandDc = p.StockOnHandDc
            }).ToList();
        }

        public async Task<DataViewResponseDto> GetDataViewAsync(DataViewRequestDto request)
        {
            var response = new DataViewResponseDto();
            try
            {
                if (request == null)
                {
                    response.Error = new ErrorDto { ErrorCode = "INVALID_REQUEST", Message = "Request cannot be null." };
                    return response;
                }

                var offer = await _repository.GetOfferByIdAsync(request.EventOfferId, request.OfferNo);
                if (offer == null)
                {
                    response.Error = new ErrorDto { ErrorCode = "NOT_FOUND", Message = $"SpendX offer {request.EventOfferId} not found." };
                    return response;
                }

                // Check if calculation needs to be refreshed across all 3 tiers and planning inputs
                bool needsRecalc = (request.isOfferEdited ?? false)
                    || (request.IncrementalPercent.HasValue && request.IncrementalPercent.Value != offer.IncrementalPercentage)
                    || (request.SpacePurchase.HasValue && request.SpacePurchase.Value != offer.SpacePurchase)
                    || (request.Spend1.HasValue && request.Spend1.Value != offer.Spend1)
                    || (request.SavePct1.HasValue && request.SavePct1.Value != offer.SavePct1)
                    || (request.Spend2.HasValue && request.Spend2.Value != offer.Spend2)
                    || (request.SavePct2.HasValue && request.SavePct2.Value != offer.SavePct2)
                    || (request.Spend3.HasValue && request.Spend3.Value != offer.Spend3)
                    || (request.SavePct3.HasValue && request.SavePct3.Value != offer.SavePct3);

                if (needsRecalc)
                {
                    await _repository.CalculateDataViewAsync(request);
                    offer = await _repository.GetOfferByIdAsync(request.EventOfferId, request.OfferNo) ?? offer;
                }

                var (items, totalCount) = await _repository.GetDataViewAsync(request);

                response.EventId = request.EventId;
                response.OfferId = offer.OfferId;
                response.OfferNo = offer.OfferNumber;
                response.PageId = offer.Page;
                response.PositionId = offer.PagePosition;
                response.OfferTypeId = offer.OfferTypeId;
                response.IsTiered = offer.Spend2.HasValue && offer.Spend2.Value > 0;
                response.Spend1 = offer.Spend1;
                response.SavePct1 = offer.SavePct1;
                response.Spend2 = offer.Spend2;
                response.SavePct2 = offer.SavePct2;
                response.Spend3 = offer.Spend3;
                response.SavePct3 = offer.SavePct3;
                response.IncrementalPercent = offer.IncrementalPercentage;
                response.SpacePurchase = offer.SpacePurchase;
                response.PaginationInfo = new PaginationInfo
                {
                    TotalCount = totalCount,
                    PageNumber = request.PageNumber > 0 ? request.PageNumber : 1,
                    PageSize = request.PageSize > 0 ? request.PageSize : 100
                };
                response.EventOfferDataView = items;

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetDataViewAsync: {Message}", ex.Message);
                response.Error = new ErrorDto { ErrorCode = "INTERNAL_ERROR", Message = ex.Message };
                return response;
            }
        }

        public async Task<DataviewCalculationsResponseDto> PostDataViewAsync(DataViewResponseDto request)
        {
            var response = new DataviewCalculationsResponseDto();
            try
            {
                if (request == null || request.EventOfferDataView == null || !request.EventOfferDataView.Any())
                {
                    response.Error = new ErrorDto { ErrorCode = "EMPTY_DATA", Message = "EventOfferDataView list cannot be empty." };
                    return response;
                }

                var currentDetails = await _repository.GetOfferDetailsAsync(request.OfferId, request.OfferNo);
                if (currentDetails == null || !currentDetails.Any())
                {
                    response.Error = new ErrorDto { ErrorCode = "NOT_FOUND", Message = $"No detail records found for OfferId {request.OfferId}." };
                    return response;
                }

                var detailLookup = currentDetails.ToDictionary(d => d.Sku, StringComparer.OrdinalIgnoreCase);

                foreach (var item in request.EventOfferDataView)
                {
                    if (detailLookup.TryGetValue(item.Sku, out var d))
                    {
                        d.Categoryforecast = item.CatFcst;
                        d.ForecastUnits = item.CatFcst;
                        d.IncrementalForecast = item.IncrFcst;
                        d.IsCategoryForecastLocked = item.isCatFcstLocked;
                        d.DisplayIndicator = item.DisplayInd;
                        d.IsSkuEdited = true;
                        d.ScanSupportValue = item.ScanSupportValue;
                        d.ScanSupportPercent = item.ScanSupportPercent;

                        // Recompute line economics using Advertised Price (BRD Item 1)
                        decimal cost = d.NationalAverageCost ?? d.VendorCostPerEach ?? 0m;
                        decimal price = d.AdvertisedPrice ?? d.EverydayPrice ?? 0m;
                        d.ForecastCost = Math.Round((d.Categoryforecast ?? 0) * cost, 2);
                        d.ForecastSales = Math.Round((d.Categoryforecast ?? 0) * price, 2);
                        d.ForecastTradeMarginDollar = Math.Round((d.ForecastSales ?? 0m) - (d.ForecastCost ?? 0m), 2);
                        d.ForecastTradeMarginPercent = d.ForecastSales > 0 ? Math.Round(((d.ForecastTradeMarginDollar ?? 0m) / d.ForecastSales.Value) * 100m, 2) : 0m;
                        d.ScanSupportDollar = Math.Round((d.ScanSupportValue ?? 0m) * (d.Categoryforecast ?? 0), 2);
                    }
                }

                // Atomically update details and recalculate header rollups in a single transaction
                await _repository.SaveDataViewDetailsAndRecalculateHeaderAsync(currentDetails, request.OfferId, request.OfferNo, request.OfferTypeId);

                var updatedOffer = await _repository.GetOfferByIdAsync(request.OfferId, request.OfferNo);

                return new DataviewCalculationsResponseDto
                {
                    Message = "SpendX data view updated successfully.",
                    ForecastSales = updatedOffer?.ForecastSales ?? 0m,
                    ForecastCost = updatedOffer?.ForecastCost ?? 0m,
                    ForecastTmDollar = updatedOffer?.ForecastTradeMargin ?? 0m,
                    ForecastTmPercent = updatedOffer?.ForecastTmPercent ?? 0m,
                    TotalSupplierIncome = updatedOffer?.TotalSupplierIncome ?? 0m
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in PostDataViewAsync: {Message}", ex.Message);
                response.Error = new ErrorDto { ErrorCode = "INTERNAL_ERROR", Message = ex.Message };
                return response;
            }
        }

        public async Task<MoveOfferResponseDto> MoveOfferAsync(MoveOfferRequestDto request)
        {
            _logger.LogInformation("Starting MoveOfferAsync for EventId={EventId}, OfferId={OfferId}, from Page={SourcePageId} Pos={SourcePos} to Page={TargetPageId} Pos={TargetPos}",
                request.EventId, request.OfferId, request.SourcePageId, request.SourcePagePosition, request.TargetPageId, request.TargetPagePosition);

            var response = new MoveOfferResponseDto();
            try
            {
                var offer = await _repository.GetOfferByIdAsync(request.OfferId);
                if (offer == null || offer.EventId != request.EventId)
                {
                    _logger.LogWarning("Offer {OfferId} not found in Event {EventId}", request.OfferId, request.EventId);
                    response.Error = new ErrorDto
                    {
                        ErrorCode = "NOT_FOUND",
                        Message = $"Offer {request.OfferId} not found in Event {request.EventId}."
                    };
                    return response;
                }

                var success = await _repository.MoveOfferTransactionAsync(request);
                if (success)
                {
                    response.Message = $"Offer {request.OfferId} moved successfully from Page: {request.SourcePageId} and Position: {request.SourcePagePosition} to Page: {request.TargetPageId} and Position: {request.TargetPagePosition}.";
                    _logger.LogInformation("Successfully completed MoveOfferAsync for EventId={EventId}, OfferId={OfferId}", request.EventId, request.OfferId);
                    return response;
                }
                else
                {
                    response.Error = new ErrorDto
                    {
                        ErrorCode = "UPDATE_FAILED",
                        Message = "Error updating the records in the database."
                    };
                    return response;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in MoveOfferAsync: {Message}", ex.Message);
                response.Error = new ErrorDto
                {
                    ErrorCode = "SERVER_ERROR",
                    Message = "An unexpected error occurred while moving the offer."
                };
                return response;
            }
        }

        public async Task<PricingExtractResponseDto> GetPricingExtractAsync(int eventId, string? userId)
        {
            _logger.LogInformation("Generating pricing extract for EventId={EventId}, UserId={UserId}", eventId, userId);
            var response = new PricingExtractResponseDto { EventId = eventId };

            try
            {
                var rows = await _repository.GetPricingExtractAsync(eventId);
                response.TotalCount = rows.Count;
                response.SpendXGetYPercent = rows.Where(r => !r.IsRewards).ToList();
                response.SpendXGetYPercentLoyal = rows.Where(r => r.IsRewards).ToList();
                response.Message = $"Pricing extract generated successfully. Total records: {rows.Count} (SpendX: {response.SpendXGetYPercent.Count}, SpendX Loyal: {response.SpendXGetYPercentLoyal.Count}).";
                _logger.LogInformation("Pricing extract completed for EventId={EventId}: Total={TotalCount}", eventId, response.TotalCount);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating pricing extract for EventId={EventId}: {Message}", eventId, ex.Message);
                response.Error = new ErrorDto
                {
                    ErrorCode = "PRICING_EXTRACT_ERROR",
                    Message = "An error occurred while generating the pricing extract."
                };
                return response;
            }
        }

        private static decimal GetGstRate(string? country)
        {
            return string.Equals(country, "NZ", StringComparison.OrdinalIgnoreCase) ? 0.15m : 0.10m;
        }
    }
}
