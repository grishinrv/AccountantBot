using Telegram.Bot.Types.ReplyMarkups;

namespace Bot.TelegramUtils;

public static class KeyboardFactory
{
    public static ReplyKeyboardMarkup Create(params KeyboardButton[] buttons)
    {
        var menu = new List<KeyboardButton[]>();
        foreach (var button in buttons)
        {
            menu.Add(new[] { button });
        }
        
        var result = new ReplyKeyboardMarkup(menu);
        return result;
    }
}