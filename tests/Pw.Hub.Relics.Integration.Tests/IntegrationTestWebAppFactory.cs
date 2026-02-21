using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Pw.Hub.Relics.Api;
using Pw.Hub.Relics.Infrastructure.Data;
using Testcontainers.PostgreSql;
using Xunit;

namespace Pw.Hub.Relics.Integration.Tests;

public class IntegrationTestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:15-alpine")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<RelicsDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            services.RemoveAll<NpgsqlDataSource>();

            var connectionString = _dbContainer.GetConnectionString();
            var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
            dataSourceBuilder.EnableDynamicJson();
            var dataSource = dataSourceBuilder.Build();

            services.AddSingleton(dataSource); // Register dataSource for reuse if needed
            services.AddDbContext<RelicsDbContext>(options =>
                options.UseNpgsql(dataSource));
        });
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await _dbContainer.StopAsync();
    }
}
