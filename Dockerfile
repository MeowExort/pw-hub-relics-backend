# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY Pw.Hub.Relics.sln .
COPY src/Pw.Hub.Relics.Api/Pw.Hub.Relics.Api.csproj src/Pw.Hub.Relics.Api/
COPY src/Pw.Hub.Relics.Domain/Pw.Hub.Relics.Domain.csproj src/Pw.Hub.Relics.Domain/
COPY src/Pw.Hub.Relics.Infrastructure/Pw.Hub.Relics.Infrastructure.csproj src/Pw.Hub.Relics.Infrastructure/
COPY src/Pw.Hub.Relics.Shared/Pw.Hub.Relics.Shared.csproj src/Pw.Hub.Relics.Shared/

# Restore dependencies
RUN dotnet restore src/Pw.Hub.Relics.Api/Pw.Hub.Relics.Api.csproj

# Copy source code
COPY src/ src/

# Build and publish
RUN dotnet publish src/Pw.Hub.Relics.Api/Pw.Hub.Relics.Api.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Create non-root user for security
RUN adduser --disabled-password --gecos "" appuser

# Copy published app
COPY --from=build /app/publish .

# Change ownership and switch to non-root user
RUN chown -R appuser:appuser /app
USER appuser

# Expose port
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl --fail http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "Pw.Hub.Relics.Api.dll"]
