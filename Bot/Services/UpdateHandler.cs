using Bot.Models;
using Bot.Settings;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;

namespace Bot.Services;

public sealed class UpdateHandler
{
    private readonly IServiceProvider _container;
    private readonly ILogger<UpdateHandler> _logger;
    
    public UpdateHandler(
        IServiceProvider container, 
        ILogger<UpdateHandler> logger)
    {
        _container = container;
        _logger = logger;
    }

    public async Task HandleErrorAsync(Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogWarning("HandleError: {Exception}", exception);
        // Cooldown in case of network connection error
        if (exception is RequestException)
        {
            await Task.Delay(TimeSpan.FromSeconds(2));
        }
    }

    public async Task HandleUpdateAsync(Update update, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await (update switch
        {
            { Message: { } message }                        => OnMessage(message),
            // { EditedMessage: { } message }                  => OnMessage(message),
            { CallbackQuery: { } callbackQuery }            => OnCallbackQuery(callbackQuery),
            // { InlineQuery: { } inlineQuery }                => OnInlineQuery(inlineQuery),
            // { ChosenInlineResult: { } chosenInlineResult }  => OnChosenInlineResult(chosenInlineResult),
            // { Poll: { } poll }                              => OnPoll(poll),
            // { PollAnswer: { } pollAnswer }                  => OnPollAnswer(pollAnswer),
            // { ChannelPost: {} message } => OnMessage(message),
            // UpdateType.EditedChannelPost:
            // UpdateType.ShippingQuery:
            // UpdateType.PreCheckoutQuery:
            _                                               => UnknownUpdateHandlerAsync(update)
        });
    }

    private async Task OnCallbackQuery(CallbackQuery callbackQuery)
    {
        _logger.LogDebug("OnCallbackQuery, data: {CallbackQuery}, id: {Id}", callbackQuery.Data, callbackQuery.Id);

        var userName = callbackQuery.From.Username!;
        if (!Utils.AllowedUsers.Contains(userName))
        {
            _logger.LogDebug("Not allowed user");
        }
        else
        {
            var userManager = _container.GetKeyedService<IUserWorkflowManager>(userName)!;

            await userManager.HandleInput(new CommandContext
            {
                UserName = userName,
                ChatId = long.Parse(callbackQuery.Id),
                LatestInputFromUser = callbackQuery.Data!
            });
        }
    }

    private async Task OnMessage(Message msg)
    {
        _logger.LogDebug("Receive message type: {MessageType}, from: \"{From}\"", msg.Type, msg.From!);

        var userName = msg.From?.Username!;
        if (!Utils.AllowedUsers.Contains(userName))
        {
            _logger.LogDebug("Not allowed user");
        }
        else
        {
            var userManager = _container.GetKeyedService<IUserWorkflowManager>(userName)!;

            await userManager.HandleInput(new CommandContext
            {
                UserName = userName,
                ChatId = msg.Chat.Id,
                LatestInputFromUser = msg.Text!
            });
        }
    }

    // async Task<Message> Usage(Message msg)
    // {
    //     const string usage = """
    //             <b><u>Bot menu</u></b>:
    //             /photo          - send a photo
    //             /inline_buttons - send inline buttons
    //             /keyboard       - send keyboard buttons
    //             /remove         - remove keyboard buttons
    //             /request        - request location or contact
    //             /inline_mode    - send inline-mode results list
    //             /poll           - send a poll
    //             /poll_anonymous - send an anonymous poll
    //             /throw          - what happens if handler fails
    //         """;
    //     return await _bot.SendTextMessageAsync(msg.Chat, usage, parseMode: ParseMode.Html, replyMarkup: new ReplyKeyboardRemove());
    // }
  
    private Task UnknownUpdateHandlerAsync(Update update)
    {
        _logger.LogWarning("Unknown update type: {UpdateType}", update.Type);
        return Task.CompletedTask;
    }
}