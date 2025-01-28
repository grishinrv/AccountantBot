using Bot.Models;
using Bot.Services;
using Bot.Storage;
using Bot.Utils;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;

namespace Bot.Commands;

public abstract class PeriodRequestingCommandBase : CommandBase
{
    protected readonly IDbContextFactory<AccountantDbContext> DbContextFactory;
    private readonly IPeriodProviderService PeriodProvider;
    
    protected PeriodRequestingCommandBase(
        IPeriodProviderService periodProvider,
        IDbContextFactory<AccountantDbContext> dbContextFactory,
        TelegramBotClient bot) 
        : base(bot)
    {
        PeriodProvider = periodProvider;
        DbContextFactory = dbContextFactory;
        Transitions.Add(KeyboardFactory.LEAF_OVER_LEFT_CALLBACK, PeriodProvider.MonthLeadOverLeft);
        Transitions.Add(KeyboardFactory.LEAF_OVER_RIGHT_CALLBACK, PeriodProvider.MonthLeadOverRight);
    }

    protected short CommandState { get; set; } = WaitingForPeriodState.WAITING_START;

    protected override async Task OnInitializedAsync(CommandContext context)
    {
        await PeriodProvider.PeriodStartPrompt(context, DateOnly.FromDateTime(DateTime.Now));
    }

    protected override async Task DefaultAction(CommandContext context)
    {
        switch (CommandState)
        {
            case WaitingForPeriodState.WAITING_START:
                await PeriodProvider.AnalyzePeriodStartInput(context);
                break;
            case WaitingForPeriodState.WAITING_END:
                var period = await PeriodProvider.AnalyzePeriodEndInput(context);
                if (period != null)
                {
                    await ProcessPeriod(context, period);
                }
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    protected abstract Task ProcessPeriod(CommandContext context, Period period);
}