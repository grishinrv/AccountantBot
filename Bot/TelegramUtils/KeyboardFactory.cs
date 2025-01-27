using Telegram.Bot.Types.ReplyMarkups;

namespace Bot.TelegramUtils;

public static class KeyboardFactory
{
    public const string LEAF_OVER_LEFT_CALLBACK = "<--";
    public const string LEAF_OVER_RIGHT_CALLBACK = "-->";

    private static int ToIndex(this DayOfWeek day)
    {
        switch (day)
        {
            case DayOfWeek.Sunday:
                return 6;
            case DayOfWeek.Monday:
                return 0;
            case DayOfWeek.Tuesday:
                return 1;
            case DayOfWeek.Wednesday:
                return 2;
            case DayOfWeek.Thursday:
                return 3;
            case DayOfWeek.Friday:
                return 4;
            case DayOfWeek.Saturday:
                return 5;
            default:
                throw new ArgumentOutOfRangeException(nameof(day), day, null);
        }
    }
    
    private static readonly InlineKeyboardButton[] DaysOfWeek =
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
    
    private static readonly InlineKeyboardButton Empty = new (){ Text = "--" };

    private static readonly InlineKeyboardButton[] ScrollButtons =
    [
        new() { Text = LEAF_OVER_LEFT_CALLBACK, CallbackData = LEAF_OVER_LEFT_CALLBACK },
        new() { Text = LEAF_OVER_RIGHT_CALLBACK, CallbackData = LEAF_OVER_RIGHT_CALLBACK }
    ];
    
    public static ReplyKeyboardMarkup Create(params KeyboardButton[] buttons)
    {
        var buttonRows = buttons
            .Select((value, index) => new { value, index })
            .GroupBy(x => x.index / 2)
            .Select(g => g.Select(x => x.value).ToArray())
            .ToList();
        
        return new ReplyKeyboardMarkup(buttonRows);
    }

    public static InlineKeyboardMarkup GetCalendar(DateTime date)
    {
        var monthStart = new DateTime(date.Year, date.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        var calendar = new List<InlineKeyboardButton[]>{ DaysOfWeek };
        
        var current = monthStart;
        var dayOfWeekIndex = current.DayOfWeek.ToIndex();

        var week = new InlineKeyboardButton[7];
        for (var i = 0; i < dayOfWeekIndex; i++)
        {
            week[i] = Empty;
        }

        while (current <= monthEnd)
        {  
            week[dayOfWeekIndex] = new InlineKeyboardButton{ Text = current.Day.ToString(), CallbackData = current.ToString("yyyy-MM-dd") };
            if (dayOfWeekIndex == 6)
            {
                calendar.Add(week);
            }
            current = current.AddDays(1);
            dayOfWeekIndex = current.DayOfWeek.ToIndex();
            if (dayOfWeekIndex == 0 && current != monthStart)
            {
                week = new InlineKeyboardButton[7];
            }
        }
        
        if (dayOfWeekIndex < 6)
        {
            for (var i = dayOfWeekIndex; i < 7; i++)
            {
                week[i] = Empty;
            }
            calendar.Add(week);
        }
        
        calendar.Add(ScrollButtons);
        
        return new InlineKeyboardMarkup(calendar);
    }
}