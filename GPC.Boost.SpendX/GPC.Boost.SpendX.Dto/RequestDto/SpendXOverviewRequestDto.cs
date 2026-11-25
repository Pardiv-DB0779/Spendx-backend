using GPC.Boost.SpendX.Dto.ResponseDto;

namespace GPC.Boost.SpendX.Dto.RequestDto
{
    public class SpendXOverviewRequestDto
    {
        public int EventId { get; set; }
        public int OfferId { get; set; }
        public int OfferNo { get; set; } = 1;
        public int PageId { get; set; }
        public int PositionId { get; set; }
        public int OfferTypeId { get; set; }
        public string? OfferName { get; set; }
        public string? Country { get; set; }
        public string? UserId { get; set; }
        public string? Username { get => UserId; set => UserId = value; }
        public string? CreatedBy { get => UserId; set => UserId = value; }
        public bool? IsReserved { get; set; }

        public SpendXOverviewDto Overview { get; set; } = new();
        public SpendXOfferInfoDto OfferInfo { get; set; } = new();
        public SpendXPricingDto Pricing { get; set; } = new();
    }
}
