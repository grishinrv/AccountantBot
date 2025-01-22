using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;

namespace Bot.Services;

public sealed class UpdateHandler
{
    private readonly TelegramBotClient _bot;
    private readonly ILogger<UpdateHandler> _logger;

    public UpdateHandler(TelegramBotClient bot, ILogger<UpdateHandler> logger)
    {
        _bot = bot;
        _logger = logger;
    }

    public async Task HandleErrorAsync(Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogInformation("HandleError: {Exception}", exception);
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
            { EditedMessage: { } message }                  => OnMessage(message),
            // { CallbackQuery: { } callbackQuery }            => OnCallbackQuery(callbackQuery),
            // { InlineQuery: { } inlineQuery }                => OnInlineQuery(inlineQuery),
            // { ChosenInlineResult: { } chosenInlineResult }  => OnChosenInlineResult(chosenInlineResult),
            // { Poll: { } poll }                              => OnPoll(poll),
            // { PollAnswer: { } pollAnswer }                  => OnPollAnswer(pollAnswer),
            { ChannelPost: {} message } => OnMessage(message),
            // UpdateType.EditedChannelPost:
            // UpdateType.ShippingQuery:
            // UpdateType.PreCheckoutQuery:
            _                                               => UnknownUpdateHandlerAsync(update)
        });
    }

    private Task OnMessage(Message msg)
    {
        _logger.LogDebug("Receive message type: {MessageType}", msg.Type);
        if (msg.Text is not { } messageText)
            return Task.CompletedTask;
    
        _logger.LogDebug("Message text: {MessageText}, sender: {Sender}", 
            msg.Text, 
            msg.Contact?.UserId.ToString() ?? "<Contact_null>");
        
        _logger.LogDebug("Update, sender chat - {ChatTitle}, id - {ChatId}", 
            msg.Chat.Title ?? "<Title_null>",
            msg.Chat.Id);
        
        return Task.CompletedTask;

        // Message sentMessage = await (messageText.Split(' ')[0] switch
        // {
        //     "/photo" => SendPhoto(msg),
        //     "/inline_buttons" => SendInlineKeyboard(msg),
        //     "/keyboard" => SendReplyKeyboard(msg),
        //     "/remove" => RemoveKeyboard(msg),
        //     "/request" => RequestContactAndLocation(msg),
        //     "/inline_mode" => StartInlineQuery(msg),
        //     "/poll" => SendPoll(msg),
        //     "/poll_anonymous" => SendAnonymousPoll(msg),
        //     "/throw" => FailingHandler(msg),
        //     _ => Usage(msg)
        // });
        // _logger.LogInformation("The message was sent with id: {SentMessageId}", sentMessage.MessageId);
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
        _logger.LogInformation("Unknown update type: {UpdateType}", update.Type);
        return Task.CompletedTask;
    }
}