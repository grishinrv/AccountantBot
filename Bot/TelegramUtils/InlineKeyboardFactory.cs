using Telegram.Bot.Types.ReplyMarkups;

namespace Bot.TelegramUtils;

public static class InlineKeyboardFactory
{
    public static InlineKeyboardMarkup Create(params InlineKeyboardButton[] buttons)
    {
        var menu = new List<InlineKeyboardButton[]>();
        foreach (var button in buttons)
        {
            menu.Add(new[] { button });
        }
        
        var result = new InlineKeyboardMarkup(menu);
        return result;
    }
}