using System;
using System.Collections.Generic;

namespace GPC.Boost.SpendX.Dto.RequestDto
{
    public class ProductSearchRequestDto
    {
        public List<string>? SKUs { get; set; }
        public List<string>? UnSelectedSKUs { get; set; }
        public List<string>? ItemClass1 { get; set; }
        public List<string>? ItemClass2 { get; set; }
        public List<string>? ItemClass3 { get; set; }
        public List<string>? ItemClass4 { get; set; }
        public List<string>? PartNumber { get; set; }
        public List<string>? SupplierId { get; set; }
        public List<string>? SupplierName { get; set; }
        public List<string>? ProductBrand { get; set; }
        public List<string>? NotItemClass1 { get; set; }
        public List<string>? NotItemClass2 { get; set; }
        public List<string>? NotItemClass3 { get; set; }
        public List<string>? NotItemClass4 { get; set; }
        public List<string>? NotSupplierId { get; set; }
        public List<string>? NotProductBrand { get; set; }
    }

    public class EventOfferRequestDto
    {
        public ProductSearchRequestDto? Search { get; set; }
        public string? Country { get; set; } = "AU";
        public string? IC1 { get; set; }
        public string? CategoryManager { get; set; }
        public string? OfferType { get; set; } = "SPEND X GET Y% OFF";
        public int OfferTypeId { get; set; } = 18; // 18: SpendX, 118: SpendX Loyal
        public string? OfferName { get; set; }
        public string? StartDate { get; set; }
        public string? EndDate { get; set; }
        public string? StartTime { get; set; }
        public string? EndTime { get; set; }
        public string? OfferDescription { get; set; }
        public string? CategoryCostType { get; set; }
        public int EventId { get; set; } = 1;
        public int PageNumber { get; set; } = 1;
        public int PositionId { get; set; } = 1;
        public int OfferNo { get; set; } = 1;
        public int? OfferId { get; set; }
        public string? UserId { get; set; }
        public string? Username { get => UserId; set => UserId = value; }
        public string? CreatedBy { get => UserId; set => UserId = value; }
        public string? PageSelection { get; set; }

        // Brand-based selection (Can come from root or from Search.ProductBrand)
        public string? ProductBrand { get; set; }

        // Optional list of explicitly selected SKUs or unselected SKUs
        public List<string>? SelectedSKUs { get; set; }
        public List<string>? UnSelectedSKUs { get; set; }

        public List<EventOfferDetailItemDto>? Details { get; set; }
    }

    public class EventOfferDetailItemDto
    {
        public string? SKU { get; set; }
        public string? PartNumber { get; set; }
        public string? PartNo { get => PartNumber; set => PartNumber = value; }
        public string? ClearanceInd { get; set; }
        public string? ShowroomInd { get; set; }
        public string? IC1 { get; set; }
    }

    public class GetEventOfferRequestDto
    {
        public int EventId { get; set; }
        public int? OfferId { get; set; }
        public int? OfferNo { get; set; }
        public int PageId { get; set; }
        public int PagePosition { get; set; }
        public string? OfferName { get; set; }
        public string? OfferType { get; set; }
        public int OfferTypeId { get; set; }
        public string? Country { get; set; }
    }
}
