using FluentValidation;

namespace Pw.Hub.Relics.Api.Features.Relics.CreateRelic;

public class CreateRelicCommandValidator : AbstractValidator<CreateRelicCommand>
{
    public CreateRelicCommandValidator()
    {
        RuleFor(x => x.SoulLevel)
            .InclusiveBetween(1, 5)
            .WithMessage("Soul level must be between 1 and 5");

        RuleFor(x => x.SoulType)
            .InclusiveBetween(1, 2)
            .WithMessage("Soul type must be 1 (Peace) or 2 (Tianya)");

        RuleFor(x => x.SlotTypeId)
            .GreaterThan(0)
            .WithMessage("Slot type ID must be greater than 0");

        RuleFor(x => x.Race)
            .InclusiveBetween(1, 6)
            .WithMessage("Race must be between 1 and 6");

        RuleFor(x => x.AbsorbExperience)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Absorb experience must be non-negative");

        RuleFor(x => x.MainAttribute)
            .NotNull()
            .WithMessage("Main attribute is required");

        RuleFor(x => x.MainAttribute.AttributeDefinitionId)
            .GreaterThan(0)
            .When(x => x.MainAttribute != null)
            .WithMessage("Main attribute definition ID must be greater than 0");

        RuleFor(x => x.MainAttribute.Value)
            .GreaterThan(0)
            .When(x => x.MainAttribute != null)
            .WithMessage("Main attribute value must be greater than 0");

        RuleFor(x => x.AdditionalAttributes)
            .Must(attrs => attrs == null || attrs.Count <= 4)
            .WithMessage("Maximum 4 additional attributes allowed");

        RuleForEach(x => x.AdditionalAttributes)
            .ChildRules(attr =>
            {
                attr.RuleFor(a => a.AttributeDefinitionId)
                    .GreaterThan(0)
                    .WithMessage("Additional attribute definition ID must be greater than 0");

                attr.RuleFor(a => a.Value)
                    .GreaterThan(0)
                    .WithMessage("Additional attribute value must be greater than 0");
            });

        RuleFor(x => x.EnhancementLevel)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Enhancement level must be non-negative");

        RuleFor(x => x.SellerCharacterId)
            .GreaterThan(0)
            .WithMessage("Seller character ID must be greater than 0");

        RuleFor(x => x.ShopPosition)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Shop position must be non-negative");

        RuleFor(x => x.Price)
            .GreaterThan(0)
            .WithMessage("Price must be greater than 0");

        RuleFor(x => x.ServerId)
            .GreaterThan(0)
            .WithMessage("Server ID must be greater than 0");
    }
}
