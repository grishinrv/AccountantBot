using System.Text;
using Bot.Models;
using Bot.Storage;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace Bot.Commands;

/// <summary>
/// statistik - Få statistik per kategori
/// </summary>
public sealed class GetStatisticsCommand : PeriodRequestingCommandBase
{
    public const string COMMAND_NAME = "/statistik";
    public override string Name => COMMAND_NAME;

    public GetStatisticsCommand(
        IDbContextFactory<AccountantDbContext> dbContextFactory,
        TelegramBotClient bot) : base(dbContextFactory, bot)
    {
    }

    protected override async Task ProcessPeriod(CommandContext context, Period period)
    {
        var purchasesByCategory = await GetStatistics(period.Start, period.End);
        CommandState = WaitingForPeriodState.WAITING_START;
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

    private async Task<AmountByCategory[]> GetStatistics(DateOnly periodStart, DateOnly periodEnd)
    {
        periodEnd = periodEnd.AddDays(1);
        await using var dbContext = await DbContextFactory.CreateDbContextAsync();
        var purchasesByCategory = await dbContext.Purchases
            .AsNoTracking()
            .Where(x => x.Date >= periodStart && x.Date < periodEnd)
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