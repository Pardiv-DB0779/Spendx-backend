using System;

namespace GPC.Boost.SpendX.Domain.Models
{
    public class TEventOfferSearchHistory
    {
        public int Id { get; set; }
        public int? EventId { get; set; }
        public int? PageId { get; set; }
        public int? PositionId { get; set; }
        public int? EventOfferId { get; set; }
        public string? SearchJson { get; set; }
        public string? UserId { get; set; }
        public DateTime? SearchedAt { get; set; }
        public string? Country { get; set; }
        public int? OfferNumber { get; set; }
    }
}
