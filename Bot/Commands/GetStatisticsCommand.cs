using Telegram.Bot;

namespace Bot.Commands;

/// <summary>
/// statistik - få statistik per kategori
/// </summary>
public sealed class GetStatisticsCommand : CommandBase
{
    private const string COMMAND_TEXT = "/statistik";
    public override string Name => COMMAND_TEXT;
    
    public GetStatisticsCommand(TelegramBotClient bot) : base(bot)
    {
    }
}