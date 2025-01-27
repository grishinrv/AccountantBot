using Bot.Models;
using Bot.Storage;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;

namespace Bot.Commands;

/// <summary>
/// visa_posta - Visa inlägg
/// </summary>
public sealed class ListRecordsCommand : PeriodRequestingCommandBase
{
    public const string COMMAND_NAME = "visa_posta";
    public override string Name => COMMAND_NAME;

    public ListRecordsCommand(IDbContextFactory<AccountantDbContext> dbContextFactory, TelegramBotClient bot) 
        : base(dbContextFactory, bot)
    {
    }

    protected override Task GetStatistics(CommandContext context, DateTime periodStart, DateTime periodEnd)
    {
        throw new NotImplementedException();
    }
}