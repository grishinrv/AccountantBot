using System.Text;
using Bot.Models;
using Bot.Services;
using Bot.Storage;
using Bot.TelegramUtils;
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
    public override string Name => COMMAND_NAME;

    protected async override Task DefaultAction(IUserWorkflowManager manager, CommandContext context)
    {
        var bot = Container.GetRequiredService<TelegramBotClient>();
        await using var dbContext = Container.GetRequiredService<AccountantDbContext>();
        var outPut = string.Empty;
        var category = dbContext.Categories.FirstOrDefault(c => c.Name == context.LatestInputFromUser);
        if (category == null)
        {
            category = new Category
            {
                Name = context.LatestInputFromUser
            };
            dbContext.Categories.Add(category);
            await dbContext.SaveChangesAsync();
            outPut = new StringBuilder("Kategori \"")
                .Append(context.LatestInputFromUser)
                .Append("\" finns redan. Vänligen ange ett annat kategorinamn")
                .ToString();
        }
        else
        {
            outPut = new StringBuilder("Kategori \"")
                .Append(context.LatestInputFromUser)
                .Append("\" framgångsrikt skapat. Skapa en annan kategori?")
                .ToString();
        }
        
        await bot.SendMessage(
            chatId: context.ChatId,
            text: outPut,
            parseMode: ParseMode.Html,
            replyMarkup: InlineKeyboardFactory.Create(
                new InlineKeyboardButton
                {
                    Text = "Avboka",
                    CallbackData = RootCommand.COMMAND_NAME,
                    Pay = false
                })
        );
    }
}