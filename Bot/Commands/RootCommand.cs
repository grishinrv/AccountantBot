using Bot.Models;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace Bot.Commands;

public sealed class RootCommand : CommandBase
{
    public const string COMMAND_NAME =  "/start";

    public RootCommand(TelegramBotClient bot) : base(bot)
    {
    }

    public override string Name => COMMAND_NAME;

    protected override async Task OnInitializedAsync(CommandContext context)
    {
        await Bot.SendMessage(
            chatId: context.ChatId,
            text: "Använd menyn för att interagera med boten",
            replyMarkup: new ReplyKeyboardRemove());
    }
}