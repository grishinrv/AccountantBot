using Bot.Models;

namespace Bot.Services;

public interface IOptionsProviderService<TOptions> where TOptions : struct, Enum
{
    void RegisterTransitions(Dictionary<string, Func<CommandContext, Task>> commandTransitions);
    Task PromptOptions(CommandContext context);
    event EventHandler<SelectedOptionsEventArgs<TOptions>>? OptionsSelected;
}