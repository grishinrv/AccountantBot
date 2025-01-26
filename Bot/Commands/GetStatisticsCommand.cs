using System.Text;
using Bot.Models;
using Bot.Storage;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace Bot.Commands;

/// <summary>
/// statistik - få statistik per kategori
/// </summary>
public sealed class GetStatisticsCommand : CommandBase
{
    public const string COMMAND_NAME = "/statistik";
    private readonly IDbContextFactory<AccountantDbContext> _dbContextFactory;
    public override string Name => COMMAND_NAME;
    
    public GetStatisticsCommand(
        IDbContextFactory<AccountantDbContext> dbContextFactory,
        TelegramBotClient bot) : base(bot)
    {
        _dbContextFactory = dbContextFactory;
    }

    protected override async Task OnInitializedAsync(CommandContext context)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        DateTime now = DateTime.UtcNow;
        var periodStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var periodEnd = periodStart.AddMonths(1).AddSeconds(-1);
        
        var purchasesByCategory = await dbContext.Purchases
            .Where(x => x.Date >= periodStart && x.Date <= periodEnd)
            .GroupBy(x => x.Category.Name)
            .Select(g => new AmountByCategory
            {
                Name = g.Key, 
                Amount = g.Sum(p => p.Spent)
            })
            .ToArrayAsync();
        
        var total = purchasesByCategory.Sum(x => x.Amount);
        var sb = new StringBuilder("Статистика за текущий месяц - всего")
            .Append(total.ToString("F"))
            .Append('Є')
            .AppendLine()
            .AppendLine();
        purchasesByCategory = purchasesByCategory.OrderByDescending(x => x.Amount).ToArray();
        
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

        await Bot.SendMessage(
            chatId: context.ChatId,
            text: sb.ToString(),
            parseMode: ParseMode.Markdown);
    }
}