using System.Text;
using Bot.Models;
using Bot.Utils;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace Bot.Services;

public sealed class PeriodProviderService : IPeriodProviderService
{
    enum State
    {
        WaitingForStartProvided = 0,
        WaitingForEndProvided = 1
    }
    
    private readonly TelegramBotClient _bot;
    private DateOnly? _periodStartDate;
    private DateOnly _currentMonth;
    private State CurrentState { get; set; } = State.WaitingForStartProvided;
    
    public PeriodProviderService(TelegramBotClient bot)
    {
        _bot = bot;
    }

    public void RegisterTransitions(Dictionary<string, Func<CommandContext, Task>> commandTransitions)
    {
        commandTransitions.Add(KeyboardFactory.LEAF_OVER_LEFT_CALLBACK, MonthLeadOverLeft);
        commandTransitions.Add(KeyboardFactory.LEAF_OVER_RIGHT_CALLBACK, MonthLeadOverRight);
    }
    
    public async Task PeriodStartPrompt(CommandContext context, DateOnly month)
    {
        CurrentState = State.WaitingForStartProvided;
        var date = month;
        _currentMonth = date;
        var text = new StringBuilder("Välj startdatum för perioden:")
                .AppendLine()
                .Append(date.ToString("MMMM", CultureHelper.RussianCulture))
                .Append(' ')
                .Append(date.Year)
                .ToString();
        
        await SendCalendar(context, date, text);
    }

    private async Task SendCalendar(CommandContext context, DateOnly date, string text)
    {
        if (context.CallBackMessageId != null)
        {
            await _bot.EditMessageText(
                chatId: context.ChatId,
                messageId: context.CallBackMessageId.Value,
                text: text,
                parseMode: ParseMode.Html,
                replyMarkup: KeyboardFactory.GetCalendar(date));
        }
        else
        {
            await _bot.SendMessage(
                chatId: context.ChatId,
                text: text,
                parseMode: ParseMode.Html,
                replyMarkup: KeyboardFactory.GetCalendar(date));
        }
    }

    public async Task<Period?> HandlePeriodWorkflow(CommandContext context)
    {
        switch (CurrentState)
        {
            case State.WaitingForStartProvided:
                await AnalyzePeriodStartInput(context);
                return null;
            case State.WaitingForEndProvided:
                var period = await AnalyzePeriodEndInput(context);
                if (period != null)
                {
                    CurrentState = State.WaitingForStartProvided;
                    await PeriodStartPrompt(context, _currentMonth);
                }
                return period;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private async Task PeriodEndPrompt(CommandContext context, DateOnly month)
    {
        CurrentState = State.WaitingForEndProvided;
        var date = month;
        _currentMonth = date;
        var text = new StringBuilder("Välj slutdatum för perioden:")
                .AppendLine()
                .Append(date.ToString("MMMM", CultureHelper.RussianCulture)) 
                .Append(' ')
                .Append(date.Year)
                .ToString();
        
        await SendCalendar(context, date, text);
    }

    private async Task MonthLeadOverLeft(CommandContext context)
    {
        switch (CurrentState)
        {
            case State.WaitingForStartProvided:
                await PeriodStartPrompt(context, _currentMonth.AddMonths(-1));
                break;
            case State.WaitingForEndProvided:
                await PeriodEndPrompt(context, _currentMonth.AddMonths(-1));
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private async Task MonthLeadOverRight(CommandContext context)
    {
        switch (CurrentState)
        {
            case State.WaitingForStartProvided:
                await PeriodStartPrompt(context, _currentMonth.AddMonths(1));
                break;
            case State.WaitingForEndProvided:
                await PeriodEndPrompt(context, _currentMonth.AddMonths(1));
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    
    private async Task AnalyzePeriodStartInput(CommandContext context)
    {
        if (DateOnly.TryParse(context.LatestInputFromUser, out var day))
        {
            _periodStartDate = day;
            await PeriodEndPrompt(context, _currentMonth);
        }
        else 
        {
            await PeriodStartPrompt(context, _currentMonth);
        }
    }

    private async Task<Period?> AnalyzePeriodEndInput(CommandContext context)
    {
        if (DateOnly.TryParse(context.LatestInputFromUser, out var day))
        {
            return new Period
            {
                Start = _periodStartDate!.Value,
                End = day
            };
        }
        await PeriodEndPrompt(context, _currentMonth);
        return null;
    }
}