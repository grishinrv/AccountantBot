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
    
    private readonly ILogger<PeriodProviderService> _logger;
    private readonly TelegramBotClient _bot;
    private DateOnly? _periodStartDate;
    private DateOnly _currentMonth;
    private State CurrentState { get; set; } = State.WaitingForStartProvided;
    
    public PeriodProviderService(ILogger<PeriodProviderService> logger, TelegramBotClient bot)
    {
        _logger = logger;
        _bot = bot;
    }

    public void RegisterTransitions(Dictionary<string, Func<CommandContext, Task>> commandTransitions)
    {
        commandTransitions.Add(KeyboardFactory.PREVIOUS_MONTH_CALLBACK, MonthLeafOverLeft);
        commandTransitions.Add(KeyboardFactory.PREVIOUS_YEAR_CALLBACK, YearLeafOverLeft);
        commandTransitions.Add(KeyboardFactory.NEXT_MONTH_CALLBACK, MonthLeafOverRight);
        commandTransitions.Add(KeyboardFactory.NEXT_YEAR_CALLBACK, YearLeafOverRight);
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

    private async Task MonthLeafOverLeft(CommandContext context)
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
    
    private async Task YearLeafOverLeft(CommandContext context)
    {
        switch (CurrentState)
        {
            case State.WaitingForStartProvided:
                await PeriodStartPrompt(context, _currentMonth.AddYears(-1));
                break;
            case State.WaitingForEndProvided:
                await PeriodEndPrompt(context, _currentMonth.AddYears(-1));
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private async Task MonthLeafOverRight(CommandContext context)
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
    
    private async Task YearLeafOverRight(CommandContext context)
    {
        switch (CurrentState)
        {
            case State.WaitingForStartProvided:
                await PeriodStartPrompt(context, _currentMonth.AddYears(1));
                break;
            case State.WaitingForEndProvided:
                await PeriodEndPrompt(context, _currentMonth.AddYears(1));
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
        _logger.LogDebug("Parsing period end: {Input}", context.LatestInputFromUser);
        if (DateTime.TryParse(context.LatestInputFromUser, out var day))
        {
            _logger.LogDebug("Parsed period end: {Day}", day);
            return new Period
            {
                Start = _periodStartDate!.Value,
                End = DateOnly.FromDateTime(day)
            };
        }
        await PeriodEndPrompt(context, _currentMonth);
        return null;
    }
}