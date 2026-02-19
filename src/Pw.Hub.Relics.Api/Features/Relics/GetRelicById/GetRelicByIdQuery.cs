using MediatR;
using Pw.Hub.Relics.Api.Features.Relics.SearchRelics;

namespace Pw.Hub.Relics.Api.Features.Relics.GetRelicById;

/// <summary>
/// Запрос на получение реликвии по ID
/// </summary>
public record GetRelicByIdQuery(Guid Id) : IRequest<RelicListingDto?>;
