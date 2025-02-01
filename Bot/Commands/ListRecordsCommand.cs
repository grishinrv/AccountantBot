using System.Text;
using Bot.Models;
using Bot.Services;
using Bot.Storage;
using Bot.Utils;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace Bot.Commands;

/// <summary>
/// visa_posta - Visa inlägg
/// </summary>
public sealed class ListRecordsCommand : CommandBase
{
    enum State
    {
        WaitingForPeriod = 0,
        WaitingForFieldsToInclude = 1,
        WaitingForFilter = 2
    }
    
    public const string COMMAND_NAME = "visa_posta";
    public override string Name => COMMAND_NAME;
    private readonly ILogger<ListRecordsCommand> _logger;
    private readonly IPeriodProviderService _periodProvider;
    private readonly IDbContextFactory<AccountantDbContext> _dbContextFactory;
    private readonly IOptionsProviderService<Include> _includeOptionsProvider;
    private State CurrentState { get; set; } = State.WaitingForPeriod; 
    private Period? Period { get; set; }
    private Include FieldsToInclude { get; set; } = Include.Default;
    
    public ListRecordsCommand(
        ILogger<ListRecordsCommand> logger,
        IPeriodProviderService periodProvider,
        IOptionsProviderService<Include> includeOptionsProvider,
        IDbContextFactory<AccountantDbContext> dbContextFactory,
        TelegramBotClient bot) 
        : base(bot)
    {
        _logger = logger;
        _periodProvider = periodProvider;
        _includeOptionsProvider = includeOptionsProvider;
        _dbContextFactory = dbContextFactory;
        _periodProvider.RegisterTransitions(Transitions);
        _includeOptionsProvider.RegisterTransitions(Transitions);
        _includeOptionsProvider.OptionsSelected += IncludeOptionsProviderOnOptionsSelected;
    }

    private void IncludeOptionsProviderOnOptionsSelected(object? sender, SelectedOptionsEventArgs<Include> e)
    {
        if (CurrentState == State.WaitingForFieldsToInclude)
        {
            FieldsToInclude = e.Options;
            CurrentState = State.WaitingForFilter;
            Task.Run(() => PromptFilter(e.Context));
        }
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
                break;
            case State.WaitingForFieldsToInclude:
                await _includeOptionsProvider.PromptOptions(context);
                break;
            case State.WaitingForFilter:
                if (decimal.TryParse(context.LatestInputFromUser, out var filter))
                {
                    CurrentState = State.WaitingForPeriod;
                    await ProcessPeriodFilter(context, FieldsToInclude, Period!, filter);
                    Period = null;
                }
                else
                {
                    await PromptFilter(context);
                }
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private async Task ProcessPeriodFilter(
        CommandContext context, 
        Include fields,
        Period period, 
        decimal filterValue)
    {
        var purchasesByCategory = await GetRecords(fields, period.Start, period.End, filterValue);
        var text = GetRecordsFormatted(purchasesByCategory, period.Start, period.End);
        await Bot.SendMessage(
            chatId: context.ChatId,
            text: text,
            parseMode: ParseMode.Markdown);
    }

    private static string GetRecordsFormatted(AmountByCategory[] purchasesByCategory, DateOnly periodStart, DateOnly periodEnd)
    {
        var total = purchasesByCategory.Sum(x => x.Amount);
        var sb = new StringBuilder("Записи c ")
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

    private async Task<AmountByCategory[]> GetRecords(Include fields, DateOnly periodStart, DateOnly periodEnd, decimal filter)
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

    private async Task PromptFilter(CommandContext context)
    {
        try
        {
            await Bot.SendMessage(
                chatId: context.ChatId,
                text: "Ange minimibeloppet för filtrering:");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to prompt filter");
        }
    }
}