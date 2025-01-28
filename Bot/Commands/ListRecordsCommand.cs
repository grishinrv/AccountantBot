using Bot.Models;
using Bot.Services;
using Bot.Storage;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;

namespace Bot.Commands;

/// <summary>
/// visa_posta - Visa inlägg
/// </summary>
public sealed class ListRecordsCommand : CommandBase
{
    public const string COMMAND_NAME = "visa_posta";
    public override string Name => COMMAND_NAME;
    private readonly IPeriodProviderService _periodProvider;
    private readonly IDbContextFactory<AccountantDbContext> _dbContextFactory;

    public ListRecordsCommand(
        IPeriodProviderService periodProvider,
        IDbContextFactory<AccountantDbContext> dbContextFactory,
        TelegramBotClient bot) 
        : base(bot)
    {
        _periodProvider = periodProvider;
        _dbContextFactory = dbContextFactory;
        _periodProvider.RegisterTransitions(Transitions);
    }

    private Task GetStatistics(CommandContext context, DateTime periodStart, DateTime periodEnd)
    {
        throw new NotImplementedException();
    }
}