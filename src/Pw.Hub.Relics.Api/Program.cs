using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using Pw.Hub.Relics.Api.BackgroundJobs;
using Pw.Hub.Relics.Infrastructure.Data;
using Pw.Hub.Relics.Infrastructure.Data.Seeding;
using Pw.Hub.Relics.Shared.Helpers;
using Prometheus;
using Telegram.Bot;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using HealthChecks.Prometheus.Metrics;

var builder = WebApplication.CreateBuilder(args);

// Database
var dataSourceBuilder = new NpgsqlDataSourceBuilder(builder.Configuration.GetConnectionString("DefaultConnection"));
dataSourceBuilder.EnableDynamicJson();
var dataSource = dataSourceBuilder.Build();

builder.Services.AddDbContext<RelicsDbContext>(options =>
    options.UseNpgsql(dataSource));

// MediatR
builder.Services.AddMediatR(cfg => 
    cfg.RegisterServicesFromAssembly(typeof(Pw.Hub.Relics.Api.Program).Assembly));

// FluentValidation
builder.Services.AddValidatorsFromAssembly(typeof(Pw.Hub.Relics.Api.Program).Assembly);

// Telegram Bot
var telegramBotToken = builder.Configuration["Telegram:BotToken"];
if (!string.IsNullOrEmpty(telegramBotToken))
{
    builder.Services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(telegramBotToken));
}

// Caching
builder.Services.AddMemoryCache();

// Background Jobs
builder.Services.AddHostedService<DeactivateExpiredListingsJob>();
builder.Services.AddHostedService<RefreshPriceHistoryJob>();
builder.Services.AddSingleton<INotificationProcessor, NotificationProcessorService>();

// Notification Queue (для оптимизированной обработки уведомлений)
builder.Services.AddSingleton<INotificationQueue, NotificationQueue>();
builder.Services.AddHostedService<NotificationBackgroundService>();

// Parse Relic Queue (для пакетной обработки парсинга)
builder.Services.AddSingleton<IParseRelicQueue, ParseRelicQueue>();
builder.Services.AddHostedService<ParseRelicBackgroundService>();

// Backfill job для заполнения AttributesHash у существующих записей
builder.Services.AddHostedService<BackfillAttributesHashJob>();

// Authentication (OpenID Connect / JWT Bearer)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Auth:Authority"];
        options.Audience = builder.Configuration["Auth:Audience"];
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true
        };
    });

// Authorization Policies
builder.Services.AddAuthorization(options =>
{
    // Политика для бота (запись реликвий)
    options.AddPolicy("BotPolicy", policy =>
        policy.RequireAssertion(context =>
        {
            var scopeClaim = context.User.FindFirst("scope")?.Value;
            if (string.IsNullOrEmpty(scopeClaim)) return false;
            var scopes = scopeClaim.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return scopes.Contains("relics:write");
        }));
    
    // Политика для пользователей (чтение реликвий)
    options.AddPolicy("UserPolicy", policy =>
        policy.RequireAssertion(context =>
        {
            var scopeClaim = context.User.FindFirst("scope")?.Value;
            if (string.IsNullOrEmpty(scopeClaim)) return false;
            var scopes = scopeClaim.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return scopes.Contains("relics:read");
        }));
});

// Controllers
builder.Services.AddControllers();

// Prometheus - сбор метрик HttpClient
builder.Services.AddHttpClient(Microsoft.Extensions.Options.Options.DefaultName)
    .UseHttpClientMetrics();

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Pw.Hub.Relics API",
        Version = "v1",
        Description = "API для управления реликвиями Perfect World"
    });
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Health Checks
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!, name: "PostgreSQL");

var app = builder.Build();

// Seed RelicDefinitions from JSON
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<RelicsDbContext>();
        
        // Migrate database
        await context.Database.MigrateAsync();

        var logger = services.GetRequiredService<ILogger<RelicDefinitionSeeder>>();
        var seeder = new RelicDefinitionSeeder(context, logger);
        
        var relicsJsonUri = builder.Configuration["Seeding:RelicsJsonUri"] ?? "https://cdn.pw-hub.ru/relics/relics.json";
        var iconBaseUri = builder.Configuration["Seeding:IconBaseUri"] ?? "https://cdn.pw-hub.ru/relics/icons";
        
        await seeder.SeedAsync(relicsJsonUri, iconBaseUri);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Pw.Hub.Relics.Api.Program>>();
        logger.LogError(ex, "An error occurred while migrating or seeding the database");
    }
}

// Load Equipment Addons data for addon value calculation
var equipmentAddonUri = builder.Configuration["Seeding:EquipmentAddonUri"] ?? "https://cdn.pw-hub.ru/relics/EQUIPMENT_ADDON.json";
try
{
    await EquipmentAddonHelper.LoadAddonsFromUriAsync(equipmentAddonUri);
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Pw.Hub.Relics.Api.Program>>();
    logger.LogWarning(ex, "Failed to load equipment addons from URI: {Uri}. Falling back to local file if exists.", equipmentAddonUri);
    
    var equipmentAddonPath = builder.Configuration["Seeding:EquipmentAddonJsonPath"] ?? "EQUIPMENT_ADDON.json";
    if (File.Exists(equipmentAddonPath))
    {
        EquipmentAddonHelper.LoadAddons(equipmentAddonPath);
    }
}

// Configure the HTTP request pipeline
//if (app.Environment.IsDevelopment())
//{
    app.UseSwagger();
    app.UseSwaggerUI();
//}

app.UseHttpsRedirection();
app.UseCors();

// Prometheus Metrics
app.UseMetricServer();
app.UseHttpMetrics();

// Health Checks (стандартные эндпоинты)
app.MapHealthChecks("/health");
app.MapHealthChecks("/ready");

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Дополнительный маппинг эндпоинта /metrics (хотя UseMetricServer по умолчанию слушает /metrics)
app.MapMetrics();

app.Run();

namespace Pw.Hub.Relics.Api
{
    public partial class Program { }
}
