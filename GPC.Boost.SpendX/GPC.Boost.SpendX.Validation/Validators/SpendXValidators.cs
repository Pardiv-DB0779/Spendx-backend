using System.Linq;
using FluentValidation;
using GPC.Boost.SpendX.Dto.RequestDto;

namespace GPC.Boost.SpendX.Validation.Validators
{
    public class EventOfferRequestDtoValidator : AbstractValidator<EventOfferRequestDto>
    {
        public EventOfferRequestDtoValidator()
        {
            RuleFor(x => x.EventId)
                .GreaterThan(0)
                .WithMessage("EventId must be greater than zero.");

            RuleFor(x => x.OfferTypeId)
                .GreaterThan(0)
                .WithMessage("OfferTypeId must be greater than zero.");

            RuleFor(x => x.StartDate)
                .NotEmpty()
                .NotNull()
                .WithMessage("StartDate is required.");

            RuleFor(x => x.EndDate)
                .NotEmpty()
                .NotNull()
                .WithMessage("EndDate is required.")
                .GreaterThanOrEqualTo(x => x.StartDate)
                .WithMessage("EndDate must be on or after StartDate.");

            RuleFor(x => x.UserId)
                .NotEmpty()
                .NotNull()
                .WithMessage("UserId is required.");

            When(x => x.Details != null && x.Details.Count > 0, () =>
            {
                RuleFor(x => x.Details)
                    .NotNull()
                    .WithMessage("Details list cannot be null when provided.");

                RuleForEach(x => x.Details).ChildRules(detail =>
                {
                    detail.RuleFor(d => d.SKU)
                        .NotEmpty()
                        .NotNull()
                        .WithMessage("SKU is required for each detail.");

                    detail.RuleFor(d => d.PartNumber)
                        .NotEmpty()
                        .NotNull()
                        .WithMessage("PartNumber is required for each detail.");
                });
            });
        }
    }

    public class SpendXOverviewRequestDtoValidator : AbstractValidator<SpendXOverviewRequestDto>
    {
        public SpendXOverviewRequestDtoValidator()
        {
            RuleFor(x => x.OfferId)
                .GreaterThan(0)
                .WithMessage("OfferId must be greater than zero.");

            When(x => x.Overview != null && (x.Overview.Spend1 > 0 || x.Overview.SavePct1 > 0), () =>
            {
                RuleFor(x => x.Overview.Spend1)
                    .GreaterThan(0)
                    .WithMessage("Tier 1 Spend threshold must be greater than zero.")
                    .PrecisionScale(18, 2, false);

                RuleFor(x => x.Overview.SavePct1)
                    .InclusiveBetween(0.01m, 100m)
                    .WithMessage("Tier 1 Save percentage must be between 0.01% and 100%.")
                    .PrecisionScale(5, 2, false);
            });

            When(x => x.Overview != null && (x.Overview.Spend2.HasValue || x.Overview.SavePct2.HasValue), () =>
            {
                RuleFor(x => x.Overview.Spend2)
                    .NotNull()
                    .WithMessage("Tier 2 Spend threshold is required when Tier 2 discount is provided.")
                    .GreaterThan(x => x.Overview.Spend1)
                    .WithMessage("Tier 2 Spend must be strictly greater than Tier 1 Spend.")
                    .PrecisionScale(18, 2, false);

                RuleFor(x => x.Overview.SavePct2)
                    .NotNull()
                    .WithMessage("Tier 2 Save percentage is required when Tier 2 spend threshold is provided.")
                    .InclusiveBetween(0.01m, 100m)
                    .WithMessage("Tier 2 Save percentage must be between 0.01% and 100%.")
                    .GreaterThan(x => x.Overview.SavePct1)
                    .WithMessage("Tier 2 Save percentage must be greater than Tier 1 Save percentage.")
                    .PrecisionScale(5, 2, false);
            });

            When(x => x.Overview != null && (x.Overview.Spend3.HasValue || x.Overview.SavePct3.HasValue), () =>
            {
                RuleFor(x => x.Overview.Spend2)
                    .NotNull()
                    .WithMessage("Tier 2 must be configured before configuring Tier 3.");

                RuleFor(x => x.Overview.Spend3)
                    .NotNull()
                    .WithMessage("Tier 3 Spend threshold is required when Tier 3 discount is provided.")
                    .GreaterThan(x => x.Overview.Spend2)
                    .WithMessage("Tier 3 Spend must be strictly greater than Tier 2 Spend.")
                    .PrecisionScale(18, 2, false);

                RuleFor(x => x.Overview.SavePct3)
                    .NotNull()
                    .WithMessage("Tier 3 Save percentage is required when Tier 3 spend threshold is provided.")
                    .InclusiveBetween(0.01m, 100m)
                    .WithMessage("Tier 3 Save percentage must be between 0.01% and 100%.")
                    .GreaterThan(x => x.Overview.SavePct2)
                    .WithMessage("Tier 3 Save percentage must be greater than Tier 2 Save percentage.")
                    .PrecisionScale(5, 2, false);
            });
        }
    }
}
