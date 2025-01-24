using Telegram.Bot;

namespace Bot.Commands;

/// <summary>
/// ny_post - Registrera det förbrukade beloppet
/// </summary>
public sealed class NewRecordCommand : CommandBase
{
    public const string COMMAND_NAME =  "/ny_post";
    public override string Name => COMMAND_NAME;
    public NewRecordCommand(TelegramBotClient bot) : base(bot)
    {
    }
    
}