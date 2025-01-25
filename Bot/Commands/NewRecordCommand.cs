using System.Text;
using Bot.Models;
using Bot.Storage;
using Bot.TelegramUtils;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Bot.Commands;

/// <summary>
/// ny_post - Registrera det förbrukade beloppet
/// </summary>
public sealed class NewRecordCommand : CommandBase
{
    enum NewRecordCommandState
    {
        WaitingForCategory = 0,
        WaitingForAmount = 1,
        WaitingForComment = 2
    }
    
    private NewRecordCommandState State { get; set; } = NewRecordCommandState.WaitingForCategory;
    private Purchase? _purchase;
    
    public const string COMMAND_NAME =  "/ny_post";
    private const string BUTTON_NEW_RECORD = "Ja, skapa en ny";
    private const string BUTTON_SAVE_WITHOUT_COMMENT = "Spara utan kommentar";
    public override string Name => COMMAND_NAME;
    
    private readonly IDbContextFactory<AccountantDbContext> _dbContextFactory;
    
    public NewRecordCommand(TelegramBotClient bot, IDbContextFactory<AccountantDbContext> dbContextFactory) : base(bot)
    {
        _dbContextFactory = dbContextFactory;
        Transitions.Add(BUTTON_NEW_RECORD, NewRecordPrompt);
        Transitions.Add(BUTTON_SAVE_WITHOUT_COMMENT, SaveRecord);
    }

    protected override async Task OnInitializedAsync(CommandContext context)
    {
        await NewRecordPrompt(context);
    }

    private static string GetCategoryLabel(Category category)
    {
        return new StringBuilder(category.Id.ToString())
            .Append(". ")
            .Append(category.Name)
            .ToString();
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
    
    private async Task NewRecordPrompt(CommandContext context)
    {
        State = NewRecordCommandState.WaitingForCategory;
        using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var categories = await dbContext.Categories.ToArrayAsync();
        var buttons = categories
            .Select(x => new KeyboardButton
            {
                Text = GetCategoryLabel(x)
            })
            .Union(
            [
                new KeyboardButton
                    {
                        Text = BUTTON_CANCEL
                    }
            ])
            .ToArray();

        _purchase = new Purchase
        {
            User = context.UserName
        };
        await Bot.SendMessage(
            chatId: context.ChatId,
            text: "Välj kategori:",
            parseMode: ParseMode.Html,
            replyMarkup: KeyboardFactory.Create(buttons));
    }

    protected override async Task DefaultAction(CommandContext context)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        switch (State)
        {
            case NewRecordCommandState.WaitingForCategory:
                await AssignCategoryWorkflow(context);
                break;
            case NewRecordCommandState.WaitingForAmount:
                await AssignAmountWorkflow(context);
                break;
            case NewRecordCommandState.WaitingForComment:
                await AssignCommentWorkflow(context);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private async Task AssignCategoryWorkflow(CommandContext context)
    {
        if (TrySetCategoryId(_purchase!, context.LatestInputFromUser, out var selectedCategoryName))
        {
            State = NewRecordCommandState.WaitingForAmount;
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
            _purchase!.Spent = amount;
            State = NewRecordCommandState.WaitingForComment;
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
        _purchase!.Comment = context.LatestInputFromUser;
        await SaveRecord(context);
    }

    private async Task SaveRecord(CommandContext context)
    {
        using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        dbContext.Purchases.Add(_purchase!);
        await dbContext.SaveChangesAsync();
        State = NewRecordCommandState.WaitingForCategory;
        await Bot.SendMessage(
            chatId: context.ChatId,
            text: "Posten har sparats",
            replyMarkup: new ReplyKeyboardRemove());
        await NewRecordPrompt(context);
    }
}