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
    
    private static readonly KeyboardButton Empty = new (){ Text = "--" };
    
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

        var calendar = new List<KeyboardButton[]>{ DaysOfWeek };
        
        var current = monthStart;
        var dayOfWeekIndex = (int)current.DayOfWeek;
        var firstWeek = new KeyboardButton[7];
        try
        {


        for (var i = 0; i < 7; i++)
        {
            if (i < dayOfWeekIndex)
            {
                firstWeek[i] = Empty;
            }
            else
            {
                firstWeek[i] = new KeyboardButton{ Text = current.Day.ToString() };
                current = current.AddDays(1);
            }
        }

        calendar.Add(firstWeek);
        
        var week = new KeyboardButton[7];
        while (current < monthEnd)
        { 
            dayOfWeekIndex = (int)current.DayOfWeek;
            week[dayOfWeekIndex] = new KeyboardButton{ Text = current.Day.ToString() };
            if (dayOfWeekIndex == 6)
            {
                calendar.Add(week);
            }
            else if (dayOfWeekIndex == 0)
            {
                week = new KeyboardButton[7];
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
        
        return new ReplyKeyboardMarkup(calendar);
        }
        catch (Exception e)
        {
            throw new ApplicationException($"{current}, {dayOfWeekIndex}, {e.Message}");
        }
    }
}