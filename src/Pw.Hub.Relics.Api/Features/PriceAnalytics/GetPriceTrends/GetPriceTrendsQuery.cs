using MediatR;

namespace Pw.Hub.Relics.Api.Features.PriceAnalytics.GetPriceTrends;

/// <summary>
/// Запрос на получение тенденций цен
/// </summary>
public record GetPriceTrendsQuery : IRequest<GetPriceTrendsResponse>
{
    /// <summary>
    /// Фильтр основной характеристики
    /// </summary>
    public AttributeFilterDto? MainAttribute { get; init; }
    
    /// <summary>
    /// Фильтры дополнительных характеристик
    /// </summary>
    public List<AttributeFilterDto>? AdditionalAttributes { get; init; }
    
    /// <summary>
    /// ID реликвии
    /// </summary>
    public int? RelicDefinitionId { get; init; }
    
    /// <summary>
    /// Уровень души
    /// </summary>
    public int? SoulLevel { get; init; }
    
    /// <summary>
    /// Тип души
    /// </summary>
    public int? SoulType { get; init; }
    
    /// <summary>
    /// Начало периода (обязательный)
    /// </summary>
    public DateTime StartDate { get; init; }
    
    /// <summary>
    /// Конец периода (обязательный, макс. 1 месяц от startDate)
    /// </summary>
    public DateTime EndDate { get; init; }
    
    /// <summary>
    /// ID сервера
    /// </summary>
    public int? ServerId { get; init; }
    
    /// <summary>
    /// Группировка (hour, day, week)
    /// </summary>
    public string? GroupBy { get; init; }
}

/// <summary>
/// Ответ с тенденциями цен
/// </summary>
public record GetPriceTrendsResponse
{
    public required FiltersDto Filters { get; init; }
    public required PeriodDto Period { get; init; }
    public required List<DataPointDto> DataPoints { get; init; }
    public required StatisticsDto Statistics { get; init; }
}

public record FiltersDto
{
    public AttributeInfoDto? MainAttribute { get; init; }
    public List<AttributeInfoDto>? AdditionalAttributes { get; init; }
    public RelicDefinitionInfoDto? RelicDefinition { get; init; }
    public int? SoulLevel { get; init; }
    public int? SoulType { get; init; }
}

public record AttributeInfoDto(int Id, string Name);

public record RelicDefinitionInfoDto(int Id, string Name);

public record AttributeFilterDto(int Id, int? MinValue = null, int? MaxValue = null);

public record PeriodDto(DateTime Start, DateTime End);

public record DataPointDto
{
    public DateTime Timestamp { get; init; }
    public long AveragePrice { get; init; }
    public long MinPrice { get; init; }
    public long MaxPrice { get; init; }
    public int Count { get; init; }
}

public record StatisticsDto
{
    public long OverallAverage { get; init; }
    public long OverallMin { get; init; }
    public long OverallMax { get; init; }
    public int TotalListings { get; init; }
    public long PriceChange { get; init; }
    public double PriceChangePercent { get; init; }
}
