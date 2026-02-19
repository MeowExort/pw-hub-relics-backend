using FluentValidation;

namespace Pw.Hub.Relics.Api.Features.PriceAnalytics.GetPriceTrends;

public class GetPriceTrendsQueryValidator : AbstractValidator<GetPriceTrendsQuery>
{
    public GetPriceTrendsQueryValidator()
    {
        RuleFor(x => x.StartDate)
            .NotEmpty()
            .WithMessage("Start date is required");

        RuleFor(x => x.EndDate)
            .NotEmpty()
            .WithMessage("End date is required");

        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.StartDate)
            .WithMessage("End date must be after start date");

        RuleFor(x => x)
            .Must(x => (x.EndDate - x.StartDate).TotalDays <= 31)
            .WithMessage("Period cannot exceed 1 month (31 days)");

        RuleFor(x => x.MainAttribute)
            .ChildRules(attr =>
            {
                attr.RuleFor(a => a!.Id).GreaterThan(0);
                attr.RuleFor(a => a!.MinValue).GreaterThan(0).When(a => a!.MinValue.HasValue);
                attr.RuleFor(a => a!.MaxValue).GreaterThan(0).When(a => a!.MaxValue.HasValue);
                attr.RuleFor(a => a!)
                    .Must(a => !a.MinValue.HasValue || !a.MaxValue.HasValue || a.MaxValue >= a.MinValue)
                    .WithMessage("MaxValue must be greater than or equal to MinValue");
            })
            .When(x => x.MainAttribute != null);

        RuleForEach(x => x.AdditionalAttributes)
            .ChildRules(attr =>
            {
                attr.RuleFor(a => a.Id).GreaterThan(0);
                attr.RuleFor(a => a.MinValue).GreaterThan(0).When(a => a.MinValue.HasValue);
                attr.RuleFor(a => a.MaxValue).GreaterThan(0).When(a => a.MaxValue.HasValue);
                attr.RuleFor(a => a)
                    .Must(a => !a.MinValue.HasValue || !a.MaxValue.HasValue || a.MaxValue >= a.MinValue)
                    .WithMessage("MaxValue must be greater than or equal to MinValue");
            })
            .When(x => x.AdditionalAttributes != null);

        RuleFor(x => x.RelicDefinitionId)
            .GreaterThan(0)
            .When(x => x.RelicDefinitionId.HasValue)
            .WithMessage("Relic definition ID must be greater than 0");

        RuleFor(x => x.SoulLevel)
            .InclusiveBetween(1, 5)
            .When(x => x.SoulLevel.HasValue)
            .WithMessage("Soul level must be between 1 and 5");

        RuleFor(x => x.SoulType)
            .InclusiveBetween(1, 2)
            .When(x => x.SoulType.HasValue)
            .WithMessage("Soul type must be 1 (Peace) or 2 (Tianya)");

        RuleFor(x => x.ServerId)
            .GreaterThan(0)
            .When(x => x.ServerId.HasValue)
            .WithMessage("Server ID must be greater than 0");

        RuleFor(x => x.GroupBy)
            .Must(g => g == null || g == "hour" || g == "day" || g == "week")
            .WithMessage("GroupBy must be 'hour', 'day', or 'week'");
    }
}
