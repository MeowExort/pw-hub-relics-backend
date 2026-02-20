using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Pw.Hub.Relics.Api.BackgroundJobs;
using Pw.Hub.Relics.Infrastructure.Data;
using Pw.Hub.Relics.Infrastructure.Data.Seeding;
using Telegram.Bot;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<RelicsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

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
        policy.RequireClaim("scope", "relics:write"));
    
    // Политика для пользователей (чтение реликвий)
    options.AddPolicy("UserPolicy", policy =>
        policy.RequireClaim("scope", "relics:write"));
});

// Controllers
builder.Services.AddControllers();

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

// Configure the HTTP request pipeline
//if (app.Environment.IsDevelopment())
//{
    app.UseSwagger();
    app.UseSwaggerUI();
//}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

namespace Pw.Hub.Relics.Api
{
    public partial class Program { }
}
