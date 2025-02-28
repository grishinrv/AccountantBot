using Bot.Models;
using Telegram.Bot.Types.ReplyMarkups;

namespace Bot.Utils;

public static class KeyboardFactory
{
    public const string DUMMY_CALLBACK = "-=-";
    public const string PREVIOUS_MONTH_CALLBACK = "<-";
    public const string PREVIOUS_YEAR_CALLBACK = "<--";
    public const string NEXT_MONTH_CALLBACK = "->";
    public const string NEXT_YEAR_CALLBACK = "-->";
    public const string DATE_PLACEHOLDER = "--";
    public const string CALLBACK_APPLY = "OK";

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
    
    private static readonly InlineKeyboardButton Empty = new (){ Text = DATE_PLACEHOLDER, CallbackData = DUMMY_CALLBACK };

    private static readonly InlineKeyboardButton[] ScrollButtons =
    [
        new() { Text = PREVIOUS_YEAR_CALLBACK, CallbackData = PREVIOUS_YEAR_CALLBACK },
        new() { Text = PREVIOUS_MONTH_CALLBACK, CallbackData = PREVIOUS_MONTH_CALLBACK },
        new() { Text = NEXT_MONTH_CALLBACK, CallbackData = NEXT_MONTH_CALLBACK },
        new() { Text = NEXT_YEAR_CALLBACK, CallbackData = NEXT_YEAR_CALLBACK }
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

    public static InlineKeyboardMarkup GetCalendar(DateOnly date)
    {
        var monthStart = new DateOnly(date.Year, date.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        var calendar = new List<InlineKeyboardButton[]>{ DaysOfWeek };
        
        var current = monthStart;
        var dayOfWeekIndex = current.DayOfWeek.ToIndex();

        var week = new InlineKeyboardButton[7];
        for (var i = 0; i < dayOfWeekIndex; i++)
        {
            week[i] = Empty;
        }

        var nextMonthStart = monthStart.AddMonths(1);
        while (current <= monthEnd)
        {  
            week[dayOfWeekIndex] = new InlineKeyboardButton{ Text = current.Day.ToString(), CallbackData = current.ToString("yyyy-MM-dd") };
            if (dayOfWeekIndex == 6)
            {
                calendar.Add(week);
            }
            current = current.AddDays(1);
            dayOfWeekIndex = current.DayOfWeek.ToIndex();
            if (dayOfWeekIndex == 0 && current != monthStart && current != nextMonthStart)
            {
                week = new InlineKeyboardButton[7];
            }
        }
        
        if (dayOfWeekIndex > 0)
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

    private static readonly InlineKeyboardButton ButtonOk = new (){ Text = CALLBACK_APPLY, CallbackData = CALLBACK_APPLY };
    
    public static InlineKeyboardMarkup GetCheckBoxList(CheckboxItemModel[] optionButtons)
    {
        var buttonRows = optionButtons
            .Select(x => new InlineKeyboardButton
                {
                    Text = GetCheckBoxText(x),
                    CallbackData = x.Callback,
                    Pay = false
                })
            .Append(ButtonOk)
            .Select((value, index) => new { value, index })
            .GroupBy(x => x.index / 2)
            .Select(g => g.Select(x => x.value).ToArray())
            .ToList();
        
        return new InlineKeyboardMarkup(buttonRows);
    }

    private static string GetCheckBoxText(CheckboxItemModel item)
    {
        return item.IsChecked ? item.DisplayName + "*" : item.DisplayName;
    }
}