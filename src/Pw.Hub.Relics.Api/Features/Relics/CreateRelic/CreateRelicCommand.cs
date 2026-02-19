using MediatR;

namespace Pw.Hub.Relics.Api.Features.Relics.CreateRelic;

public record CreateRelicCommand : IRequest<CreateRelicResult>
{
    public int SoulLevel { get; init; }
    public int SoulType { get; init; }
    public int SlotTypeId { get; init; }
    public int Race { get; init; }
    public int AbsorbExperience { get; init; }
    public RelicAttributeInput MainAttribute { get; init; } = null!;
    public List<RelicAttributeInput>? AdditionalAttributes { get; init; }
    public int EnhancementLevel { get; init; }
    public long SellerCharacterId { get; init; }
    public int ShopPosition { get; init; }
    public long Price { get; init; }
    public int ServerId { get; init; }
}

public record RelicAttributeInput
{
    public int AttributeDefinitionId { get; init; }
    public int Value { get; init; }
}

public record CreateRelicResult(Guid Id, bool IsUpdated);
