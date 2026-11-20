using System;

namespace GPC.Boost.SpendX.Domain.Models
{
    public class TOfferType
    {
        public string Country { get; set; } = null!;
        public int OfferTypeId { get; set; }
        public string OfferType { get; set; } = null!;
        public string? HybrisPillBackgroundColor { get; set; }
        public string? HybrisPillTextColor { get; set; }
        public string? HybrisDefaultPillText { get; set; }
        public string? HybrisDefaultStickerText { get; set; }
        public string? HybrisDefaultStickerBackgroundColor { get; set; }
        public string? HybrisDefaultStickerTextColor { get; set; }
        public string? HybrisActionText { get; set; }
        public string? HybrisActionBackgroundColor { get; set; }
        public string? HybrisActionTextColor { get; set; }
        public string? HybrisSaveAmountText { get; set; }
        public string? HybrisSaveAmountTextColor { get; set; }
        public string? HybrisCartMessage { get; set; }
        public string? HybrisDrMessageColor { get; set; }
        public string? HybrisMessageColor { get; set; }
        public string? HybrisDefaultImage { get; set; }
        public string? HybrisLoyaltyPillBackgroundColor { get; set; }
        public string? HybrisLoyaltyPillTextColor { get; set; }
        public string? HybrisLoyaltyPillText { get; set; }
        public string? HybrisLoyaltyStickerBackgroundColor { get; set; }
        public string? HybrisLoyaltyStickerTextColor { get; set; }
        public string? HybrisLoyaltyStickerText { get; set; }
        public string? HybrisLoyaltyOfferId { get; set; }
        public string? HybrisLoyaltyCartMessage { get; set; }
        public string? HybrisLoyaltyDrMessageColor { get; set; }
        public int AggregateType { get; set; }
        public int AmountThresholdShareUse { get; set; }
        public int HybrisTargetGroupUse { get; set; }
        public int AggregateTypeOverride { get; set; }
        public bool ReplaceImageCopyIndicator { get; set; }
        public int MaximumSkuCount { get; set; }
        public string? HybrisLoyaltyMessageColor { get; set; }
    }
}
