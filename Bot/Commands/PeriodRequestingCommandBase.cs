using System.Globalization;
using System.Text;
using Bot.Models;
using Bot.Storage;
using Bot.TelegramUtils;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace Bot.Commands;

public abstract class PeriodRequestingCommandBase : CommandBase
{
    protected readonly IDbContextFactory<AccountantDbContext> DbContextFactory;
    protected DateTime? _startTime;
    private DateTime _currentMonth;
    protected static readonly CultureInfo RussianCulture = CultureInfo.CreateSpecificCulture("ru");

    protected PeriodRequestingCommandBase(IDbContextFactory<AccountantDbContext> dbContextFactory, TelegramBotClient bot) 
        : base(bot)
    {
        DbContextFactory = dbContextFactory;
        Transitions.Add(KeyboardFactory.LEAF_OVER_LEFT_CALLBACK, MonthLeadOverLeft);
        Transitions.Add(KeyboardFactory.LEAF_OVER_RIGHT_CALLBACK, MonthLeadOverRight);
    }

    protected short CommandState { get; set; } = WaitingForPeriodState.WAITING_START;

    protected override async Task OnInitializedAsync(CommandContext context)
    {
        await PeriodStartPrompt(context, DateTime.Now);
    }

    protected override async Task DefaultAction(CommandContext context)
    {
        switch (CommandState)
        {
            case WaitingForPeriodState.WAITING_START:
                await AnalyzePeriodStartInput(context);
                break;
            case WaitingForPeriodState.WAITING_END:
                await AnalyzePeriodEndInput(context);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    
    protected async Task PeriodStartPrompt(CommandContext context, DateTime month)
    {
        CommandState = WaitingForPeriodState.WAITING_START;
        var dateTime = month;
        _currentMonth = dateTime;
        var sb = new StringBuilder("Välj startdatum för perioden:")
            .AppendLine()
            .Append(dateTime.ToString("MMMM", GetStatisticsCommand.RussianCulture))
            .Append(' ')
            .Append(dateTime.Year);
        
        await Bot.SendMessage(
            chatId: context.ChatId,
            text: sb.ToString(),
            parseMode: ParseMode.Html,
            replyMarkup: KeyboardFactory.GetCalendar(dateTime));
    }

    private async Task PeriodEndPrompt(CommandContext context, DateTime month)
    {
        CommandState =  WaitingForPeriodState.WAITING_END;
        var dateTime = month;
        _currentMonth = dateTime;
        var sb = new StringBuilder("Välj slutdatum för perioden:")
            .AppendLine()
            .Append(dateTime.ToString("MMMM", GetStatisticsCommand.RussianCulture))
            .Append(' ')
            .Append(dateTime.Year);
        
        await Bot.SendMessage(
            chatId: context.ChatId,
            text: sb.ToString(),
            parseMode: ParseMode.Html,
            replyMarkup: KeyboardFactory.GetCalendar(dateTime));
    }

    protected async Task MonthLeadOverLeft(CommandContext context)
    {
        switch (CommandState)
        {
            case WaitingForPeriodState.WAITING_START:
                await PeriodStartPrompt(context, _currentMonth.AddMonths(-1));
                break;
            case WaitingForPeriodState.WAITING_END:
                await PeriodEndPrompt(context, _currentMonth.AddMonths(-1));
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    protected async Task MonthLeadOverRight(CommandContext context)
    {
        switch (CommandState)
        {
            case WaitingForPeriodState.WAITING_START:
                await PeriodStartPrompt(context, _currentMonth.AddMonths(1));
                break;
            case WaitingForPeriodState.WAITING_END:
                await PeriodEndPrompt(context, _currentMonth.AddMonths(1));
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private async Task AnalyzePeriodStartInput(CommandContext context)
    {
        if (DateTime.TryParse(context.LatestInputFromUser, out var day))
        {
            _startTime = day;
            await PeriodEndPrompt(context, _currentMonth);
        }
        else 
        {
            await PeriodStartPrompt(context, _currentMonth);
        }
    }

    private async Task AnalyzePeriodEndInput(CommandContext context)
    {
        if (DateTime.TryParse(context.LatestInputFromUser, out var day))
        {
            await GetStatistics(context, _startTime!.Value, day);
        }
        else 
        {
            await PeriodEndPrompt(context, _currentMonth);
        }
    }

    protected abstract Task GetStatistics(CommandContext context, DateTime periodStart, DateTime periodEnd);
}