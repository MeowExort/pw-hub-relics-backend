using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pw.Hub.Relics.Domain.Entities;
using Pw.Hub.Relics.Infrastructure.Data;
using Telegram.Bot;

namespace Pw.Hub.Relics.Api.Features.Telegram;

[ApiController]
[Route("api/telegram")]
public class TelegramController : ControllerBase
{
    private readonly RelicsDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TelegramController> _logger;
    private readonly ITelegramBotClient? _telegramBotClient;

    public TelegramController(
        RelicsDbContext dbContext,
        IConfiguration configuration,
        ILogger<TelegramController> logger,
        ITelegramBotClient? telegramBotClient = null)
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _logger = logger;
        _telegramBotClient = telegramBotClient;
    }

    /// <summary>
    /// Получить userId из JWT токена
    /// </summary>
    private string GetUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) 
                     ?? User.FindFirstValue("sub");
        
        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedAccessException("User ID not found in token");
        }
        
        return userId;
    }

    /// <summary>
    /// Генерирует deeplink ссылку для привязки Telegram аккаунта
    /// </summary>
    [HttpPost("binding/generate-link")]
    [Authorize(Policy = "UserPolicy")]
    public async Task<IActionResult> GenerateBindingLink(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var botUsername = _configuration["Telegram:BotUsername"];

        if (string.IsNullOrEmpty(botUsername))
        {
            _logger.LogError("Telegram:BotUsername is not configured");
            return StatusCode(500, new { error = "Telegram bot is not configured" });
        }

        // Проверяем, есть ли уже подтвержденная привязка
        var existingBinding = await _dbContext.TelegramBindings
            .FirstOrDefaultAsync(b => b.UserId == userId, cancellationToken);

        if (existingBinding != null && existingBinding.IsConfirmed)
        {
            return BadRequest(new { error = "Telegram account is already linked" });
        }

        // Генерируем новый токен
        var linkToken = GenerateSecureToken();
        var tokenExpiresAt = DateTime.UtcNow.AddMinutes(30);

        if (existingBinding != null)
        {
            // Обновляем существующую запись
            existingBinding.LinkToken = linkToken;
            existingBinding.TokenExpiresAt = tokenExpiresAt;
            existingBinding.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            // Создаем новую запись
            existingBinding = new TelegramBinding
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                LinkToken = linkToken,
                TokenExpiresAt = tokenExpiresAt,
                IsConfirmed = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _dbContext.TelegramBindings.Add(existingBinding);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Формируем deeplink: https://t.me/BotUsername?start=TOKEN
        var deepLink = $"https://t.me/{botUsername}?start={linkToken}";

        return Ok(new GenerateBindingLinkResponse
        {
            DeepLink = deepLink,
            ExpiresAt = tokenExpiresAt
        });
    }

    /// <summary>
    /// Получить статус привязки Telegram аккаунта
    /// </summary>
    [HttpGet("binding/status")]
    [Authorize(Policy = "UserPolicy")]
    public async Task<IActionResult> GetBindingStatus(CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        var binding = await _dbContext.TelegramBindings
            .FirstOrDefaultAsync(b => b.UserId == userId, cancellationToken);

        if (binding == null)
        {
            return Ok(new BindingStatusResponse
            {
                IsLinked = false,
                TelegramUsername = null,
                LinkedAt = null
            });
        }

        return Ok(new BindingStatusResponse
        {
            IsLinked = binding.IsConfirmed,
            TelegramUsername = binding.TelegramUsername,
            LinkedAt = binding.IsConfirmed ? binding.UpdatedAt : null
        });
    }

    /// <summary>
    /// Отвязать Telegram аккаунт
    /// </summary>
    [HttpDelete("binding")]
    [Authorize(Policy = "UserPolicy")]
    public async Task<IActionResult> UnlinkTelegram(CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        var binding = await _dbContext.TelegramBindings
            .FirstOrDefaultAsync(b => b.UserId == userId, cancellationToken);

        if (binding == null)
        {
            return NotFound(new { error = "No Telegram binding found" });
        }

        var chatId = binding.TelegramChatId;
        _dbContext.TelegramBindings.Remove(binding);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} unlinked Telegram account", userId);

        // Try to notify user in Telegram about unlinking
        if (_telegramBotClient != null && chatId != null)
        {
            try
            {
                await _telegramBotClient.SendMessage(
                    chatId: chatId.Value,
                    text: "ℹ️ Привязка Telegram к аккаунту PW Hub Relics отменена. Вы больше не будете получать уведомления.",
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send unlink notification to chat {ChatId}", chatId);
            }
        }

        return NoContent();
    }

    /// <summary>
    /// Генерирует криптографически безопасный токен
    /// </summary>
    private static string GenerateSecureToken()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
    }
}

public class GenerateBindingLinkResponse
{
    public string DeepLink { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
}

public class BindingStatusResponse
{
    public bool IsLinked { get; init; }
    public string? TelegramUsername { get; init; }
    public DateTime? LinkedAt { get; init; }
}
