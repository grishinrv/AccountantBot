using Bot.Models;

namespace Bot.Services;

public interface IPeriodProviderService
{
    void RegisterTransitions(Dictionary<string, Func<CommandContext, Task>> commandTransitions);
    Task PeriodStartPrompt(CommandContext context, DateOnly month);
    Task PeriodEndPrompt(CommandContext context, DateOnly month);
    Task AnalyzePeriodStartInput(CommandContext context);
    Task<Period?> AnalyzePeriodEndInput(CommandContext context);
}