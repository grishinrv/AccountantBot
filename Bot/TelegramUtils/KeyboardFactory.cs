using Telegram.Bot.Types.ReplyMarkups;

namespace Bot.TelegramUtils;

public static class KeyboardFactory
{
    public const string DUMMY_CALLBACK = "-=-";
    
    private static readonly InlineKeyboardButton[] DaysOfWeek =
    [
        new()
        {
            Text = "Пн",
            CallbackData = DUMMY_CALLBACK
        },
        new()
        {
            Text = "Вт",
            CallbackData = DUMMY_CALLBACK 
        },
        new()
        {
            Text = "Ср",
            CallbackData = DUMMY_CALLBACK 
        },
        new()
        {
            Text = "Чт",
            CallbackData = DUMMY_CALLBACK 
        },
        new()
        {
            Text = "Пт",
            CallbackData = DUMMY_CALLBACK 
        },
        new()
        {
            Text = "Сб",
            CallbackData = DUMMY_CALLBACK 
        },
        new()
        {
            Text = "Вс",
            CallbackData = DUMMY_CALLBACK 
        }
    ];
    
    private static readonly InlineKeyboardButton Empty = new (){ Text = string.Empty, CallbackData = DUMMY_CALLBACK };
    
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
        var dayOfWeekIndex = (int)current.DayOfWeek;
        var firstWeek = new InlineKeyboardButton[7];
        Console.WriteLine($"Day of week: {dayOfWeekIndex}");
        for (var i = 0; i < 7; i++)
        {
            if (i < dayOfWeekIndex)
            {
                firstWeek[i] = Empty;
            }
            else
            {
                firstWeek[i] = new InlineKeyboardButton{ Text = current.Day.ToString(), CallbackData = current.ToString("yyyy-MM-dd") };
                current = current.AddDays(1);
            }
        }

        calendar.Add(firstWeek);
        
        var week = new InlineKeyboardButton[7];
        while (current < monthEnd)
        { 
            dayOfWeekIndex = (int)current.DayOfWeek;
            week[dayOfWeekIndex] = new InlineKeyboardButton{ Text = current.Day.ToString(), CallbackData = current.ToString("yyyy-MM-dd") };
            if (dayOfWeekIndex == 6)
            {
                calendar.Add(week);
            }
            else if (dayOfWeekIndex == 0)
            {
                week = new InlineKeyboardButton[7];
            }
            current = current.AddDays(1);
        }

        // if (dayOfWeekIndex != 6)
        // {
        //     for (var i = dayOfWeekIndex; i < 7; i++)
        //     {
        //         if (i < dayOfWeekIndex)
        //         {
        //             week[i] = Empty;
        //         }
        //     }
        //     calendar.Add(week);
        // }
        
        return new InlineKeyboardMarkup(calendar);
    }
}