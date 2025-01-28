using Bot.Models;

namespace Bot.Services;

public interface IPeriodProviderService
{
    void RegisterTransitions(Dictionary<string, Func<CommandContext, Task>> commandTransitions);
    Task PeriodStartPrompt(CommandContext context, DateOnly month);
    Task<Period?> HandlePeriodWorkflow(CommandContext context);
}