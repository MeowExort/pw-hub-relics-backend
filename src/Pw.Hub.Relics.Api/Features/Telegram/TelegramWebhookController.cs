using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pw.Hub.Relics.Infrastructure.Data;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Pw.Hub.Relics.Api.Features.Telegram;

[ApiController]
[Route("api/telegram/webhook")]
public class TelegramWebhookController : ControllerBase
{
    private readonly RelicsDbContext _dbContext;
    private readonly ILogger<TelegramWebhookController> _logger;

    public TelegramWebhookController(
        RelicsDbContext dbContext,
        ILogger<TelegramWebhookController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Обрабатывает webhook от Telegram бота
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> HandleWebhook(
        [FromBody] Update update,
        CancellationToken cancellationToken)
    {
        try
        {
            if (update.Type != UpdateType.Message || update.Message?.Text == null)
            {
                return Ok();
            }

            var message = update.Message;
            var text = message.Text;

            // Обрабатываем команду /start с параметром (deeplink)
            if (text.StartsWith("/start "))
            {
                var linkToken = text.Substring("/start ".Length).Trim();
                await HandleStartCommand(message, linkToken, cancellationToken);
            }
            else if (text == "/start")
            {
                _logger.LogDebug("Received /start without parameter from chat {ChatId}", message.Chat.Id);
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Telegram webhook");
            return Ok(); // Всегда возвращаем OK, чтобы Telegram не повторял запрос
        }
    }

    /// <summary>
    /// Обрабатывает команду /start с токеном привязки
    /// </summary>
    private async Task HandleStartCommand(Message message, string linkToken, CancellationToken cancellationToken)
    {
        var chatId = message.Chat.Id;
        var username = message.From?.Username;

        _logger.LogInformation(
            "Processing binding request: ChatId={ChatId}, Username={Username}, Token={Token}",
            chatId, username, linkToken.Substring(0, Math.Min(8, linkToken.Length)) + "...");

        // Ищем привязку по токену
        var binding = await _dbContext.TelegramBindings
            .FirstOrDefaultAsync(b => b.LinkToken == linkToken, cancellationToken);

        if (binding == null)
        {
            _logger.LogWarning("Binding not found for token: {Token}", linkToken.Substring(0, Math.Min(8, linkToken.Length)) + "...");
            return;
        }

        // Проверяем, не истек ли токен
        if (binding.TokenExpiresAt < DateTime.UtcNow)
        {
            _logger.LogWarning("Token expired for binding {BindingId}", binding.Id);
            return;
        }

        // Проверяем, не привязан ли уже этот Telegram аккаунт к другому пользователю
        var existingBindingForChat = await _dbContext.TelegramBindings
            .FirstOrDefaultAsync(b => b.TelegramChatId == chatId && b.IsConfirmed && b.Id != binding.Id, cancellationToken);

        if (existingBindingForChat != null)
        {
            _logger.LogWarning(
                "Telegram chat {ChatId} is already linked to another user {UserId}",
                chatId, existingBindingForChat.UserId);
            return;
        }

        // Подтверждаем привязку
        binding.TelegramChatId = chatId;
        binding.TelegramUsername = username;
        binding.IsConfirmed = true;
        binding.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Successfully linked Telegram chat {ChatId} to user {UserId}",
            chatId, binding.UserId);
    }
}
