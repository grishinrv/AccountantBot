using Bot.Models;

namespace Bot.Services;

public interface IPeriodProviderService
{
    Task PeriodStartPrompt(CommandContext context, DateOnly month);
    Task PeriodEndPrompt(CommandContext context, DateOnly month);
    Task MonthLeadOverLeft(CommandContext context);
    Task MonthLeadOverRight(CommandContext context);
    Task AnalyzePeriodStartInput(CommandContext context);
    Task<Period?> AnalyzePeriodEndInput(CommandContext context);
}