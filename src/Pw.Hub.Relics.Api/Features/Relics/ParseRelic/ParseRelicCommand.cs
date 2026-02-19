using MediatR;

namespace Pw.Hub.Relics.Api.Features.Relics.ParseRelic;

public record ParseRelicCommand(string Server, byte[] Data) : IRequest<ParseRelicResult>;

public record ParseRelicResult(int CreatedCount, int UpdatedCount, string Message);
