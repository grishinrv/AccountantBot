using Bot.Utils;
using Shouldly;

namespace Bot.Tests;

public sealed class KeyboardFactoryTests
{
    [Fact]
    public void LastDayOfMonthIsSaturday()
    {
        var november = new DateOnly(2024, 11, 1);
        var calendar = KeyboardFactory.GetCalendar(november);
        var inlineKeyboardButtons = calendar.InlineKeyboard.ToArray();
        inlineKeyboardButtons.Length.ShouldBe(7);
        var lastWeek = inlineKeyboardButtons[5].ToArray();
        lastWeek.Length.ShouldBe(7);
        lastWeek[6].Text.ShouldBe(KeyboardFactory.DATE_PLACEHOLDER);
        lastWeek[5].Text.ShouldBe("30");
        lastWeek[5].CallbackData.ShouldBe("2024-11-30");
    }
    
    [Fact]
    public void LastDayOfMonthIsSunday()
    {
        var november = new DateOnly(2024, 06, 1);
        var calendar = KeyboardFactory.GetCalendar(november);
        var inlineKeyboardButtons = calendar.InlineKeyboard.ToArray();
        inlineKeyboardButtons.Length.ShouldBe(7);
        var lastWeek = inlineKeyboardButtons[5].ToArray();
        lastWeek.Length.ShouldBe(7);
        lastWeek[6].Text.ShouldBe("30");
        lastWeek[6].CallbackData.ShouldBe("2024-06-30");
    }
}