using System.Globalization;
using System.Text;
using Bot.Models;
using Bot.Storage;
using Bot.TelegramUtils;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace Bot.Commands;

/// <summary>
/// statistik - få statistik per kategori
/// </summary>
public sealed class GetStatisticsCommand : CommandBase
{
    enum State
    {
        Initial = 0,
        WaitingForPeriodStart = 1,
        WaitingForPeriodEnd = 2
    }
    
    public const string COMMAND_NAME = "/statistik";
    private readonly IDbContextFactory<AccountantDbContext> _dbContextFactory;
    public override string Name => COMMAND_NAME;
    private State CommandState { get; set; }  = State.Initial;
    private static readonly CultureInfo RussianCulture = CultureInfo.CreateSpecificCulture("ru");
    private DateTime? _startTime = null;
    private DateTime _currentMonth;
    
    public GetStatisticsCommand(
        IDbContextFactory<AccountantDbContext> dbContextFactory,
        TelegramBotClient bot) : base(bot)
    {
        _dbContextFactory = dbContextFactory;
        Transitions.Add(KeyboardFactory.LEAF_OVER_LEFT_CALLBACK, MonthLeadOverLeft);
        Transitions.Add(KeyboardFactory.LEAF_OVER_RIGHT_CALLBACK, MonthLeadOverRight);
    }

    protected override async Task OnInitializedAsync(CommandContext context)
    {
        await PeriodStartPrompt(context, DateTime.Now);
    }

    private async Task PeriodStartPrompt(CommandContext context, DateTime month)
    {
        CommandState = State.WaitingForPeriodStart;
        var dateTime = month;
        _currentMonth = dateTime;
        var sb = new StringBuilder("Välj startdatum för perioden:")
            .AppendLine()
            .Append(dateTime.ToString("MMMM", RussianCulture))
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
        CommandState = State.WaitingForPeriodEnd;
        var dateTime = month;
        _currentMonth = dateTime;
        var sb = new StringBuilder("Välj slutdatum för perioden:")
            .AppendLine()
            .Append(dateTime.ToString("MMMM", RussianCulture))
            .Append(' ')
            .Append(dateTime.Year);
        
        await Bot.SendMessage(
            chatId: context.ChatId,
            text: sb.ToString(),
            parseMode: ParseMode.Html,
            replyMarkup: KeyboardFactory.GetCalendar(dateTime));
    }

    protected override async Task DefaultAction(CommandContext context)
    {
        switch (CommandState)
        {
            case State.Initial:
                await base.DefaultAction(context);
                break;
            case State.WaitingForPeriodStart:
                await AnalyzePeriodStartInput(context);
                break;
            case State.WaitingForPeriodEnd:
                await AnalyzeEndStartInput(context);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private async Task MonthLeadOverLeft(CommandContext context)
    {
        switch (CommandState)
        {
            case State.WaitingForPeriodStart:
                await PeriodStartPrompt(context, _currentMonth.AddMonths(-1));
                break;
            case State.WaitingForPeriodEnd:
                await PeriodEndPrompt(context, _currentMonth.AddMonths(-1));
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    
    private async Task MonthLeadOverRight(CommandContext context)
    {
        switch (CommandState)
        {
            case State.WaitingForPeriodStart:
                await PeriodStartPrompt(context, _currentMonth.AddMonths(1));
                break;
            case State.WaitingForPeriodEnd:
                await PeriodEndPrompt(context, _currentMonth.AddMonths(1));
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private async Task AnalyzePeriodStartInput(CommandContext context)
    {
        if (int.TryParse(context.LatestInputFromUser, out var day))
        {
            _startTime = new DateTime(_currentMonth.Year, _currentMonth.Month, day);
            await PeriodEndPrompt(context, _currentMonth);
        }
        else 
        {
            await PeriodStartPrompt(context, _currentMonth);
        }
    }
    
    private async Task AnalyzeEndStartInput(CommandContext context)
    {
        if (int.TryParse(context.LatestInputFromUser, out var day))
        {
            var endDate = new DateTime(_currentMonth.Year, _currentMonth.Month, day).AddDays(1).AddSeconds(-1);
            await GetStatistics(context, _startTime!.Value, endDate);
        }
        else 
        {
            await PeriodEndPrompt(context, _currentMonth);
        }
    }
    
    private async Task GetStatistics(CommandContext context, DateTime periodStart, DateTime periodEnd)
    {
        var purchasesByCategory = await GetStatistics(periodStart, periodEnd);
        
        var text = GetStatisticsFormatted(purchasesByCategory, periodStart, periodEnd);
        await Bot.SendMessage(
            chatId: context.ChatId,
            text: text,
            parseMode: ParseMode.Markdown);
    }

    private static string GetStatisticsFormatted(AmountByCategory[] purchasesByCategory, DateTime periodStart, DateTime periodEnd)
    {
        var total = purchasesByCategory.Sum(x => x.Amount);
        var sb = new StringBuilder("Статистика c ")
            .Append(periodStart.ToString("yyyy-MM-dd"))
            .Append(" по ")
            .Append(periodEnd.ToString("yyyy-MM-dd"))
            .Append(" - всего ")
            .Append(total.ToString("F"))
            .Append('Є')
            .AppendLine()
            .AppendLine();
        
        foreach (var item in purchasesByCategory)
        {
            var percentage = (double)(item.Amount / total * 100);
            var barLength = (int)(percentage / 4);
          
            sb.Append(item.Name)
                .Append(": ")
                .Append(item.Amount)
                .AppendLine("Є")
                .Append('(')
                .Append(percentage.ToString("F2"))
                .Append("%)")
                .AppendLine();
            
            for (var i = 0; i < barLength; i++)
            {
                sb.Append('■');
            }
            
            sb.AppendLine();
        }

        var text = sb.ToString();
        return text;
    }

    private async Task<AmountByCategory[]> GetStatistics(DateTime periodStart, DateTime periodEnd)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var purchasesByCategory = await dbContext.Purchases
            .Where(x => x.Date >= periodStart && x.Date <= periodEnd)
            .GroupBy(x => x.Category.Name)
            .Select(g => new AmountByCategory
            {
                Name = g.Key, 
                Amount = g.Sum(p => p.Spent)
            })
            .ToArrayAsync();
        
        purchasesByCategory = purchasesByCategory.OrderByDescending(x => x.Amount).ToArray();
        return purchasesByCategory;
    }
}