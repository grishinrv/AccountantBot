using System.Text;
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
    enum State
    {
        WaitingForPeriod = 0,
        WaitingForFieldsToInclude = 1,
        WaitingForFilter = 2
    }
    
    public const string COMMAND_NAME = "/visa_posta";
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
        _logger.LogDebug("Selected Include Options: {Options}, state: {State}", e.Options, CurrentState);
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
                if (Period != null)
                {
                    CurrentState = State.WaitingForFieldsToInclude;
                    await _includeOptionsProvider.PromptOptions(context);
                }
                break;
            case State.WaitingForFieldsToInclude:
                await _includeOptionsProvider.PromptOptions(context);
                break;
            case State.WaitingForFilter:
                if (decimal.TryParse(context.LatestInputFromUser, out var filter))
                {
                    _logger.LogDebug("Period start: {Start}, end: {End}", Period?.Start, Period?.End);
                    await ProcessPeriodFilter(context, FieldsToInclude, Period!, filter);
                    CurrentState = State.WaitingForPeriod;
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
        await foreach (var purchasesByDate in GetRecordsEnumeratedByDay(period.Start, period.End, filterValue))
        {
            if (purchasesByDate.Count > 0)
            {
                var text = GetRecordsFormatted(purchasesByDate, fields);
                await Bot.SendMessage(
                    chatId: context.ChatId,
                    text: text);
            }
        }
    }

    private static string GetRecordsFormatted(
        List<Purchase> purchasesByDate,
        Include fields)
    {
        var sb = new StringBuilder("Записи за ")
            .Append(purchasesByDate[0].Date.ToString("yyyy-MM-dd"))
            .Append(':')
            .AppendLine();

        for (var i = 1; i < purchasesByDate.Count; i++)
        {
            sb.Append("--");
            if (fields.HasFlag(Include.User))
            {
                sb.Append(purchasesByDate[i].User)
                    .Append(", ");
            }
            if (fields.HasFlag(Include.Time))
            {
                sb.Append(purchasesByDate[i].Date.ToString("HH:mm"))
                    .Append(", ");
            }

            sb.Append(purchasesByDate[i].Spent.ToString("F"))
                .Append("Є - ")
                .Append(purchasesByDate[i].Category.Name)
                .Append(", ");

            if (fields.HasFlag(Include.Comment) && !string.IsNullOrWhiteSpace(purchasesByDate[i].Comment))
            {
                sb.Append(purchasesByDate[i].Comment)
                    .Append(", ");
            }
            
            sb.AppendLine();
        }
        
        var text = sb.ToString();
        return text;
    }

    private async IAsyncEnumerable<List<Purchase>> GetRecordsEnumeratedByDay(DateOnly periodStart, DateOnly periodEnd, decimal filter)
    {
        var start = periodStart.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var end = periodEnd.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        while (start <= end)
        {
            var currentStart = start;
            var purchases = await GetPurchasesForDay(filter, currentStart);
            start = start.AddDays(1);
            yield return purchases;
        }
    }

    private async Task<List<Purchase>> GetPurchasesForDay(
        decimal filter, 
        DateTime currentStart)
    {
        var currentEnd = currentStart.AddDays(1);
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var purchases = await dbContext.Purchases
            .AsNoTracking()
            .Where(x => x.Date >= currentStart && x.Date < currentEnd && x.Spent >= filter)
            .Include(x => x.Category)
            .ToListAsync();
        purchases = purchases.OrderBy(x => x.Date).ToList();
        return purchases;
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