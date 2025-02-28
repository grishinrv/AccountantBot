using System.Text;
using Bot.Models;
using Bot.Storage;
using Bot.Utils;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Bot.Commands;

/// <summary>
/// ny_kategori - Skapa en ny kategori
/// </summary>
public sealed class NewCategoryCommand : CommandBase
{
    public const string COMMAND_NAME =  "/ny_kategori";
    private const string BUTTON_NEW_CATEGORY = "Ja, skapa en ny";
    public override string Name => COMMAND_NAME;

    private readonly ILogger<NewCategoryCommand> _logger;
    private readonly IDbContextFactory<AccountantDbContext> _dbContextFactory;
    
    public NewCategoryCommand(
        ILogger<NewCategoryCommand> logger,
        TelegramBotClient bot, 
        IDbContextFactory<AccountantDbContext> dbContextFactory)
        : base(bot)
    {
        _logger = logger;
        _dbContextFactory = dbContextFactory;
        Transitions.Add(BUTTON_NEW_CATEGORY, NewCategoryPrompt);
    }

    protected override async Task OnInitializedAsync(CommandContext context)
    {
        await NewCategoryPrompt(context);
    }

    private async Task NewCategoryPrompt(CommandContext context)
    {
        await Bot.SendMessage(
            chatId: context.ChatId,
            text: "Ange ett namn för den nya kategorin:",
            parseMode: ParseMode.Html,
            replyMarkup: KeyboardFactory.Create(
                new KeyboardButton
                {
                    Text = BUTTON_CANCEL
                }));
    }

    protected override async Task DefaultAction(CommandContext context)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        string outPut;
        var category = dbContext.Categories.FirstOrDefault(c => c.Name == context.LatestInputFromUser);
        if (category == null)
        {
            category = new Category
            {
                Name = context.LatestInputFromUser
            };
            dbContext.Categories.Add(category);
            await dbContext.SaveChangesAsync();
            _logger.LogDebug("Category saved with Id: {Id}", category.Id);
            
            outPut = new StringBuilder("Kategori \"")
                .Append(context.LatestInputFromUser)
                .Append("\" finns redan. Skapa en annan kategori?")
                .ToString();
        }
        else
        {
            outPut = new StringBuilder("Kategori \"")
                .Append(context.LatestInputFromUser)
                .Append("\" framgångsrikt skapat. Skapa en annan kategori?")
                .ToString();
        }
        
        await Bot.SendMessage(
            chatId: context.ChatId,
            text: outPut,
            parseMode: ParseMode.Html,
            replyMarkup: KeyboardFactory.Create(
                new KeyboardButton
                {
                    Text = BUTTON_NEW_CATEGORY 
                },
                new KeyboardButton
                {
                    Text = BUTTON_CANCEL
                })
        );
    }
}