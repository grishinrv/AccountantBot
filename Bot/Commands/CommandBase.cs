using Bot.Models;
using Bot.Services;

namespace Bot.Commands;

public abstract class CommandBase
{
    public abstract string Name { get; }
    private IUserWorkflowManager _userWorkflowManager = null!;
    protected Dictionary<string, Func<CommandContext, Task>> Transitions { get; }

    public void Initialize(IUserWorkflowManager userWorkflowManager)
    {
        _userWorkflowManager = userWorkflowManager;
    }

    protected virtual Task OnInitializedAsync(CommandContext context)
    {
        return Task.CompletedTask;
    }

    protected CommandBase()
    {
        Transitions = new Dictionary<string, Func<CommandContext, Task>>
        {
            { RootCommand.COMMAND_NAME, SwitchCommand<RootCommand> },
            { NewCategoryCommand.COMMAND_NAME, SwitchCommand<NewCategoryCommand> },
            { NewRecordCommand.COMMAND_NAME, SwitchCommand<NewRecordCommand> }
        };
    }

    protected async Task SwitchCommand<T>(CommandContext context) 
        where T : CommandBase
    {
        var commandInstance = _userWorkflowManager.SwitchCommand<T>();
        await commandInstance.OnInitializedAsync(context);
    }

    protected virtual Task DefaultAction(CommandContext context)
    {
        return SwitchCommand<RootCommand>(context);
    }

    public async Task Handle(CommandContext context)
    {
        if (!Transitions.TryGetValue(context.LatestInputFromUser, out var action))
        {
            action = DefaultAction;
        }
        await action(context);
    }
}