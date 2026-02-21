using MediatR;

namespace Pw.Hub.Relics.Api.Features.Relics.SearchRelics;

/// <summary>
/// Запрос на поиск реликвий с фильтрацией
/// </summary>
public record SearchRelicsQuery : IRequest<SearchRelicsResponse>
{
    /// <summary>
    /// Тип души (1=Покоя, 2=Тяньюя)
    /// </summary>
    public int? SoulType { get; init; }
    
    /// <summary>
    /// ID типа слота
    /// </summary>
    public int? SlotTypeId { get; init; }
    
    /// <summary>
    /// Раса (1-6)
    /// </summary>
    public int? Race { get; init; }
    
    /// <summary>
    /// Уровень души (1-5)
    /// </summary>
    public int? SoulLevel { get; init; }
    
    /// <summary>
    /// ID основной характеристики
    /// </summary>
    public int? MainAttributeId { get; init; }
    
    /// <summary>
    /// Характеристики для фильтрации
    /// </summary>
    public List<RelicAttributeFilterDto>? AdditionalAttributes { get; init; }
    
    /// <summary>
    /// Минимальная цена (серебро)
    /// </summary>
    public long? MinPrice { get; init; }
    
    /// <summary>
    /// Максимальная цена (серебро)
    /// </summary>
    public long? MaxPrice { get; init; }
    
    /// <summary>
    /// ID сервера
    /// </summary>
    public int? ServerId { get; init; }
    
    /// <summary>
    /// Номер страницы (default: 1)
    /// </summary>
    public int PageNumber { get; init; } = 1;
    
    /// <summary>
    /// Размер страницы (default: 20, max: 100)
    /// </summary>
    public int PageSize { get; init; } = 20;
}

/// <summary>
/// Ответ на поиск реликвий
/// </summary>
public record SearchRelicsResponse
{
    public required List<RelicListingDto> Items { get; init; }
    public int TotalCount { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
}

/// <summary>
/// DTO лота реликвии
/// </summary>
public record RelicListingDto
{
    public Guid Id { get; init; }
    public required RelicDefinitionDto RelicDefinition { get; init; }
    public int AbsorbExperience { get; init; }
    public required RelicAttributeDto MainAttribute { get; init; }
    public required List<RelicAttributeDto> AdditionalAttributes { get; init; }
    public int EnhancementLevel { get; init; }
    public long Price { get; init; }
    public required string PriceFormatted { get; init; }
    public required ServerDto Server { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// DTO определения реликвии
/// </summary>
public record RelicDefinitionDto
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public int SoulLevel { get; init; }
    public int SoulType { get; init; }
    public required SlotTypeDto SlotType { get; init; }
    public int Race { get; init; }
    public string? IconUri { get; init; }
}

/// <summary>
/// DTO типа слота
/// </summary>
public record SlotTypeDto(int Id, string Name);

/// <summary>
/// DTO сервера
/// </summary>
public record ServerDto(int Id, string Name, string Key);

/// <summary>
/// DTO характеристики реликвии
/// </summary>
public record RelicAttributeDto
{
    public required AttributeDefinitionDto AttributeDefinition { get; init; }
    public int Value { get; init; }
}

/// <summary>
/// DTO определения характеристики
/// </summary>
public record AttributeDefinitionDto(int Id, string Name);

/// <summary>
/// Фильтр по характеристикам
/// </summary>
public record RelicAttributeFilterDto(int Id, int? MinValue);
