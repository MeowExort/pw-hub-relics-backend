using System.Text;
using Microsoft.EntityFrameworkCore;
using Pw.Hub.Relics.Domain.Entities;
using Pw.Hub.Relics.Domain.Enums;
using Pw.Hub.Relics.Infrastructure.Data;
using Pw.Hub.Relics.Shared.Helpers;
using Telegram.Bot;

namespace Pw.Hub.Relics.Api.BackgroundJobs;

/// <summary>
/// Сервис для обработки уведомлений при создании нового лота
/// </summary>
public interface INotificationProcessor
{
    Task ProcessNewListingAsync(RelicListing listing, CancellationToken cancellationToken = default);
}

public class NotificationProcessorService : INotificationProcessor
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NotificationProcessorService> _logger;
    private readonly ITelegramBotClient? _telegramBotClient;

    public NotificationProcessorService(
        IServiceProvider serviceProvider,
        ILogger<NotificationProcessorService> logger,
        ITelegramBotClient? telegramBotClient = null)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _telegramBotClient = telegramBotClient;
    }

    public async Task ProcessNewListingAsync(RelicListing listing, CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<RelicsDbContext>();

            // Загрузить связанные данные если не загружены
            if (listing.RelicDefinition == null)
            {
                listing = await dbContext.RelicListings
                    .Include(r => r.RelicDefinition)
                    .FirstOrDefaultAsync(r => r.Id == listing.Id, cancellationToken) ?? listing;
            }

            // Получить все активные фильтры
            var filters = await dbContext.NotificationFilters
                .Where(f => f.IsEnabled)
                .ToListAsync(cancellationToken);

            foreach (var filter in filters)
            {
                if (MatchesFilter(listing, filter))
                {
                    await SendNotificationAsync(filter, listing, cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing notifications for listing {ListingId}", listing.Id);
        }
    }

    private bool MatchesFilter(RelicListing listing, NotificationFilter filter)
    {
        // Проверка сервера
        if (filter.ServerId.HasValue && filter.ServerId.Value != listing.ServerId)
            return false;

        // Проверка типа души
        if (filter.SoulType.HasValue && filter.SoulType.Value != listing.RelicDefinition.SoulType)
            return false;

        // Проверка расы
        if (filter.Race.HasValue && filter.Race.Value != listing.RelicDefinition.Race)
            return false;

        // Проверка уровня души
        if (filter.SoulLevel.HasValue && filter.SoulLevel.Value != listing.RelicDefinition.SoulLevel)
            return false;

        // Проверка типа слота
        if (filter.SlotTypeId.HasValue && filter.SlotTypeId.Value != listing.RelicDefinition.SlotTypeId)
            return false;

        // Проверка цены
        if (filter.MinPrice.HasValue && listing.Price < filter.MinPrice.Value)
            return false;

        if (filter.MaxPrice.HasValue && listing.Price > filter.MaxPrice.Value)
            return false;

        // Проверка основной характеристики
        if (filter.MainAttributeId.HasValue)
        {
            var mainAttr = listing.JsonAttributes.FirstOrDefault(a => a.Category == AttributeCategory.Main);
            if (mainAttr == null || mainAttr.AttributeDefinitionId != filter.MainAttributeId.Value)
                return false;
        }

        // Проверка дополнительных характеристик
        if (filter.RequiredAdditionalAttributeIds is { Count: > 0 })
        {
            var additionalAttrIds = listing.JsonAttributes
                .Where(a => a.Category == AttributeCategory.Additional)
                .Select(a => a.AttributeDefinitionId)
                .ToHashSet();

            foreach (var requiredAttrId in filter.RequiredAdditionalAttributeIds)
            {
                if (!additionalAttrIds.Contains(requiredAttrId))
                    return false;
            }
        }

        return true;
    }

    private async Task SendNotificationAsync(
        NotificationFilter filter, 
        RelicListing listing, 
        CancellationToken cancellationToken)
    {
        var message = BuildNotificationMessage(listing);
        
        _logger.LogInformation(
            "Resolving chat for user {UserId} for listing {ListingId}",
            filter.UserId,
            listing.Id);

        if (_telegramBotClient != null)
        {
            try
            {
                // Resolve chat id via user's Telegram binding
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<RelicsDbContext>();

                var binding = await dbContext.TelegramBindings
                    .Where(b => b.UserId == filter.UserId && b.IsConfirmed && b.TelegramChatId != null)
                    .OrderByDescending(b => b.UpdatedAt)
                    .FirstOrDefaultAsync(cancellationToken);

                if (binding?.TelegramChatId is long chatId)
                {
                    await _telegramBotClient.SendMessage(
                        chatId: chatId,
                        text: message,
                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                        cancellationToken: cancellationToken);

                    _logger.LogDebug("Notification sent successfully to chat {ChatId}", chatId);
                }
                else
                {
                    _logger.LogInformation(
                        "No confirmed Telegram binding with chat id for user {UserId}. Skipping notification.",
                        filter.UserId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send Telegram notification for user {UserId}", filter.UserId);
            }
        }
        else
        {
            _logger.LogWarning("Telegram bot client is not configured. Message: {Message}", message);
        }
    }

    private string BuildNotificationMessage(RelicListing listing)
    {
        var mainAttr = listing.JsonAttributes.FirstOrDefault(a => a.Category == AttributeCategory.Main);
        var additionalAttrs = listing.JsonAttributes.Where(a => a.Category == AttributeCategory.Additional).ToList();

        var message = new StringBuilder();
        message.AppendLine("🔔 Новая реликвия!");
        message.AppendLine();
        message.AppendLine($"📦 {listing.RelicDefinition.Name}");
        message.AppendLine($"💰 Цена: {PriceHelper.FormatPrice(listing.Price)}");
        message.AppendLine($"⚡ Заточка: +{listing.EnhancementLevel}");
        message.AppendLine($"🔮 Опыт: {listing.AbsorbExperience}");

        if (mainAttr != null)
        {
            message.AppendLine();
            message.AppendLine($"📊 Основная: ID {mainAttr.AttributeDefinitionId} = {mainAttr.Value}");
        }

        if (additionalAttrs.Count > 0)
        {
            message.AppendLine("📈 Дополнительные:");
            foreach (var attr in additionalAttrs)
            {
                message.AppendLine($"  • ID {attr.AttributeDefinitionId} = {attr.Value}");
            }
        }

        return message.ToString();
    }
}
