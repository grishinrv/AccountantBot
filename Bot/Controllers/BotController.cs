using Bot.Services;
using Bot.Settings;
using Microsoft.AspNetCore.Mvc;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Bot.Controllers;

[ApiController]
[Route("acc_bot")]
public class BotController(ILogger<BotController> logger) : ControllerBase
{
    [HttpGet("setWebhook")]
    public async Task<IActionResult> SetWebHook([FromServices] TelegramBotClient bot, CancellationToken ct)
    {
        var allowedMessages= new []{ UpdateType.Message, UpdateType.ChannelPost };
        var webhookUrl = new Uri(Utils.WebHookUrl).AbsoluteUri;
        
        logger.LogDebug("Setting webhook for {Url}", webhookUrl);
        
        try
        {
            await bot.SetWebhook(
                url: webhookUrl, 
                allowedUpdates: allowedMessages, 
                cancellationToken: ct,
                secretToken: Utils.BotToken);
        }
        catch (Exception e)
        {
            logger.LogError("Error setting webhook - {Error}", e.Message);
            throw;
        }
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> Post(
        [FromBody] Update update, 
        [FromServices] UpdateHandler handleUpdateService, 
        CancellationToken ct)
    {
        if (!string.Equals(Request.Headers["X-Telegram-Bot-Api-Secret-Token"], Utils.BotToken))
        {
            logger.LogInformation("Bot Secret Token is missing, Forbidden");
            return StatusCode(403);
        }
        
        try
        {
            await handleUpdateService.HandleUpdateAsync(update, ct);
        }
        catch (Exception exception)
        {
            await handleUpdateService.HandleErrorAsync(exception, ct);
        }
        return Ok();
    }

    // [HttpPost("jobcompleted")]
    // public async Task<IActionResult> JobCompletedCallback(
    //     // [FromBody] Payload job,
    //     [FromServices] TelegramBotClient bot, 
    //     CancellationToken ct)
    // {
    //     await bot.SendTextMessageAsync(config.ChatId, string.Format("Job '{0}' {1}. Branch: '{2}', commit {3}, message: '{4}', PR: {5}."), 
    //         cancellationToken: 
    //         ct);
    //     return Ok();
    // }
}