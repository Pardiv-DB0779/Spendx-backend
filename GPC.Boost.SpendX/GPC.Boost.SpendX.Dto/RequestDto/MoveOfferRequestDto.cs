using System;

namespace GPC.Boost.SpendX.Dto.RequestDto
{
    public class MoveOfferRequestDto
    {
        public int EventId { get; set; }
        public int OfferId { get; set; }
        public int SourcePageId { get; set; }
        public int SourcePagePosition { get; set; }
        public int TargetPageId { get; set; }
        public int TargetPagePosition { get; set; }
    }
}
