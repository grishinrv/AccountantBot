using Bot.Models;
using Bot.Services;
using Bot.Storage;
using Telegram.Bot;

namespace Bot.Commands;

/// <summary>
/// ny_kategori - Skapa en ny kategori
/// </summary>
public sealed class NewCategoryCommand : CommandBase
{
    public const string COMMAND_NAME =  "/ny_kategori";
    public override string Name => COMMAND_NAME;

    protected async override Task DefaultAction(IUserWorkflowManager manager, string userInput)
    {
        await using var context = Container.GetRequiredService<AccountantDbContext>();
        var category = context.Categories.FirstOrDefault(c => c.Name == userInput);
        if (category == null)
        {
            category = new Category
            {
                Name = userInput
            };
            context.Categories.Add(category);
            await context.SaveChangesAsync();
        }
        else
        {
            var bot = Container.GetRequiredService<TelegramBotClient>();
        }
    }
}