using System.Text;
using Bot.Models;
using Bot.Services;
using Bot.Storage;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace Bot.Commands;

/// <summary>
/// visa_posta - Visa inlägg
/// </summary>
public sealed class ListRecordsCommand : CommandBase
{
    [Flags]
    enum Include
    {
        Default = 0,
        User = 1 << 0,
        Comment = 1 << 1,
        Time = 1 << 2
    }

    enum State
    {
        WaitingForPeriod = 0,
        WaitingForFieldsToInclude = 1,
        WaitingForFilter = 2
    }
    
    public const string COMMAND_NAME = "visa_posta";
    public override string Name => COMMAND_NAME;
    private readonly IPeriodProviderService _periodProvider;
    private readonly IDbContextFactory<AccountantDbContext> _dbContextFactory;
    private State CurrentState { get; set; } = State.WaitingForPeriod; 
    private Period? Period { get; set; }
    
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

    protected override async Task OnInitializedAsync(CommandContext context)
    {
        await _periodProvider.PeriodStartPrompt(context, DateOnly.FromDateTime(DateTime.Now));
    }

    protected override async Task DefaultAction(CommandContext context)
    {
        switch (CurrentState)
        {
            case State.WaitingForPeriod:
                Period = await _periodProvider.HandlePeriodWorkflow(context);
                if (Period != null)
                {
                    await ProcessPeriod(context, Period);
                }
                break;
            case State.WaitingForFieldsToInclude:
                break;
            case State.WaitingForFilter:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private async Task ProcessPeriod(CommandContext context, Period period)
    {
        var purchasesByCategory = await GetRecords(period.Start, period.End);
        var text = GetRecorsFormatted(purchasesByCategory, period.Start, period.End);
        await Bot.SendMessage(
            chatId: context.ChatId,
            text: text,
            parseMode: ParseMode.Markdown);
    }

    private static string GetRecorsFormatted(AmountByCategory[] purchasesByCategory, DateOnly periodStart, DateOnly periodEnd)
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

    private async Task<AmountByCategory[]> GetRecords(DateOnly periodStart, DateOnly periodEnd)
    {
        var start = periodStart.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var end = periodEnd.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var purchasesByCategory = await dbContext.Purchases
            .AsNoTracking()
            .Where(x => x.Date >= start && x.Date < end)
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