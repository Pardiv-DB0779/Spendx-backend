using System;

namespace GPC.Boost.SpendX.Domain.Models
{
    public class TMudMap
    {
        public int MudMapId { get; set; }
        public int EventId { get; set; } = 1;
        public string Country { get; set; } = "AU";
        public int PageId { get; set; }
        public int PositionId { get; set; }
        public int? OfferId { get; set; }
        public int? OfferNumber { get; set; }
        public string? OfferName { get; set; }
        public string? OfferType { get; set; }
        public string? Brand { get; set; }
        public decimal? EverydayPrice { get; set; }
        public decimal? AdvertisedPrice { get; set; }
        public decimal? SaveValue { get; set; }
        public decimal? SavePercent { get; set; }
        public bool IsReserved { get; set; } = false;
        public bool? Clearance { get; set; } = false;
        public bool? New { get; set; } = false;
        public bool? Loyalty { get; set; } = false;
        public bool IsActive { get; set; } = true;
        public string? UserName { get; set; }
        public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
