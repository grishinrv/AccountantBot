using System.Text;
using Bot.Models;
using Bot.Services;
using Bot.Storage;
using Bot.Utils;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Bot.Commands;

/// <summary>
/// korrigera_post - Korrigera inmatningen
/// </summary>
public sealed class CorrectRecordCommand : CommandBase
{
    public const string COMMAND_NAME = "/korrigera_post";
    public override string Name => COMMAND_NAME;
    private const string BUTTON_SAVE_WITHOUT_COMMENT = "Spara utan kommentar";
    private readonly ILogger<CorrectRecordCommand> _logger;
    private readonly IPeriodProviderService _periodProvider;
    private readonly IDbContextFactory<AccountantDbContext> _dbContextFactory;
    private readonly IOptionsProviderService<Include> _includeOptionsProvider;
    private CorrectRecordCommandState CurrentState { get; set; } = CorrectRecordCommandState.WaitingForPeriod; 
    private Period? Period { get; set; }
    private Purchase? EditedPurchase { get; set; }
    private Include FieldsToInclude { get; set; } = Include.Default;
    
    public CorrectRecordCommand(
        ILogger<CorrectRecordCommand> logger,
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
        if (CurrentState == CorrectRecordCommandState.WaitingForFieldsToInclude)
        {
            FieldsToInclude = e.Options;
            CurrentState = CorrectRecordCommandState.WaitingForFilter;
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
            case CorrectRecordCommandState.WaitingForPeriod:
                Period = await _periodProvider.HandlePeriodWorkflow(context);
                if (Period != null)
                {
                    CurrentState = CorrectRecordCommandState.WaitingForFieldsToInclude;
                    await _includeOptionsProvider.PromptOptions(context);
                }
                break;
            case CorrectRecordCommandState.WaitingForFieldsToInclude:
                await _includeOptionsProvider.PromptOptions(context);
                break;
            case CorrectRecordCommandState.WaitingForFilter:
                if (decimal.TryParse(context.LatestInputFromUser, out var filter))
                {
                    _logger.LogDebug("Period start: {Start}, end: {End}", Period?.Start, Period?.End);
                    await ProcessPeriodFilter(context, FieldsToInclude, Period!, filter);
                    CurrentState = CorrectRecordCommandState.WaitingForPeriod;
                    Period = null;
                }
                else
                {
                    await PromptFilter(context);
                    CurrentState = CorrectRecordCommandState.WaitingForId;
                }
                break;
            case CorrectRecordCommandState.WaitingForId:
                if (int.TryParse(context.LatestInputFromUser, out var pickedId))
                {
                    await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
                    EditedPurchase = dbContext.Purchases.AsNoTracking().FirstOrDefault(x => x.Id == pickedId);
                    if (EditedPurchase != null)
                    {
                        CurrentState = CorrectRecordCommandState.WaitingForCategory;
                    }
                }
                break;
            case CorrectRecordCommandState.WaitingForCategory:
                await AssignCategoryWorkflow(context);
                break;
            case CorrectRecordCommandState.WaitingForAmount:
                await AssignAmountWorkflow(context);
                break;
            case CorrectRecordCommandState.WaitingForComment:
                await AssignCommentWorkflow(context);
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
        
        await Bot.SendMessage(
            chatId: context.ChatId,
            text: "Skriva in ID");
        CurrentState = CorrectRecordCommandState.WaitingForId;
    }

    private static string GetRecordsFormatted(
        List<Purchase> purchasesByDate,
        Include fields)
    {
        var sb = new StringBuilder("Записи за ")
            .Append(purchasesByDate[0].Date.ToString("yyyy-MM-dd"))
            .Append(':')
            .AppendLine();

        for (var i = 0; i < purchasesByDate.Count; i++)
        {
            sb
                .Append("-- id: ")
                .Append(purchasesByDate[i].Id)
                .Append(", ");
            
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
    

    private static bool TrySetCategoryId(Purchase purchase, string userInput, out string categoryName)
    {
        categoryName = string.Empty;
        var separatorIndex = userInput.IndexOf('.');
        var result = int.TryParse(userInput.Substring(0, separatorIndex), out var categoryId);
        if (result)
        {
            purchase.CategoryId = categoryId;
            categoryName = userInput.Substring(separatorIndex);
        }
        return result;
    }

    private async Task AssignCategoryWorkflow(CommandContext context)
    {
        if (TrySetCategoryId(EditedPurchase!, context.LatestInputFromUser, out var selectedCategoryName))
        {
            CurrentState = CorrectRecordCommandState.WaitingForAmount;
            await Bot.SendMessage(
                chatId: context.ChatId,
                text: $"Kategori \"{selectedCategoryName}\" vald. Кange beloppet:",
                replyMarkup: new ReplyKeyboardRemove());
        }
        else
        {
            await Bot.SendMessage(
                chatId: context.ChatId,
                text: "Ogiltig kategori, använd knapparna för att välja en kategori");
        }
    }

    private async Task AssignAmountWorkflow(CommandContext context)
    {
        if (decimal.TryParse(context.LatestInputFromUser, out var amount))
        {
            EditedPurchase!.Spent = amount;
            CurrentState = CorrectRecordCommandState.WaitingForComment;
            await Bot.SendMessage(
                chatId: context.ChatId,
                text: $"Beloppet \"{amount}\" har sparats, eventuella kommentarer?",
                parseMode: ParseMode.Html,
                replyMarkup: KeyboardFactory.Create(
                    new KeyboardButton
                    {
                        Text = BUTTON_SAVE_WITHOUT_COMMENT  
                    },
                    new KeyboardButton
                    {
                        Text = BUTTON_CANCEL  
                    })
                );
        }
    }
    
    private async Task AssignCommentWorkflow(CommandContext context)
    {
        EditedPurchase!.Comment = context.LatestInputFromUser;
        await SaveRecord(context);
    }

    private async Task SaveRecord(CommandContext context)
    {
        if (EditedPurchase != null)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            dbContext.Purchases.Update(EditedPurchase);
            await dbContext.SaveChangesAsync();
            CurrentState = CorrectRecordCommandState.WaitingForCategory;
            await Bot.SendMessage(
                chatId: context.ChatId,
                text: "Posten har sparats",
                replyMarkup: new ReplyKeyboardRemove());
        }
    }
}