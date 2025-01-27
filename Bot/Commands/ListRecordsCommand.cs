using Telegram.Bot;

namespace Bot.Commands;

/// <summary>
/// visa_posta - Visa inlägg
/// </summary>
public sealed class ListRecordsCommand : CommandBase
{
    public const string COMMAND_NAME = "visa_posta";
    public override string Name => COMMAND_NAME;

    public ListRecordsCommand(TelegramBotClient bot) : base(bot)
    {
    }

}