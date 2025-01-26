using Telegram.Bot.Types.ReplyMarkups;

namespace Bot.TelegramUtils;

public static class KeyboardFactory
{
    private static readonly KeyboardButton[] DaysOfWeek =
    [
        new()
        {
            Text = "Пн"
        },
        new()
        {
            Text = "Вт"
        },
        new()
        {
            Text = "Ср"
        },
        new()
        {
            Text = "Чт"
        },
        new()
        {
            Text = "Пт"
        },
        new()
        {
            Text = "Сб"
        },
        new()
        {
            Text = "Вс"
        }
    ];
    
    private static readonly KeyboardButton Empty = new (){ Text = string.Empty };
    
    public static ReplyKeyboardMarkup Create(params KeyboardButton[] buttons)
    {
        var buttonRows = buttons
            .Select((value, index) => new { value, index })
            .GroupBy(x => x.index / 2)
            .Select(g => g.Select(x => x.value).ToArray())
            .ToList();
        
        return new ReplyKeyboardMarkup(buttonRows);
    }

    public static ReplyKeyboardMarkup GetCalendar(DateTime date)
    {
        var monthStart = new DateTime(date.Year, date.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        var calendar = new List<IList<KeyboardButton>>{ DaysOfWeek };
        
        var current = monthStart;
        var dayOfWeekIndex = (int)current.DayOfWeek;
        var firstWeek = new List<KeyboardButton>(dayOfWeekIndex);
        for (var i = 0; i < dayOfWeekIndex; i++)
        {
            firstWeek[i] = Empty;
        }

        for (var i = dayOfWeekIndex; i < 7; i++)
        {
            firstWeek.Add(new KeyboardButton{ Text = current.Day.ToString() });
            current = current.AddDays(1);
        }
        calendar.Add(firstWeek);
        
        
        // while (current < monthEnd)
        // {
        //     current = current.AddDays(1);
        // }
        //
        
        return new ReplyKeyboardMarkup(calendar);
    }
}