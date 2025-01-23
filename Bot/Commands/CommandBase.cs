using Bot.Models;
using Bot.Services;

namespace Bot.Commands;

public abstract class CommandBase
{
    public abstract string Name { get; }
    protected IServiceProvider Container { get; private set; } = null!;
    protected Dictionary<string, Func<IUserWorkflowManager, CommandContext, Task>> Transitions { get; }

    public void Initialize(IServiceProvider serviceProvider)
    {
        Container = serviceProvider;
    }

    protected CommandBase()
    {
        Transitions = new Dictionary<string, Func<IUserWorkflowManager, CommandContext, Task>>
        {
            { RootCommand.COMMAND_NAME, SwitchCommand<RootCommand> },
            { NewCategoryCommand.COMMAND_NAME, SwitchCommand<NewCategoryCommand> },
            { NewRecordCommand.COMMAND_NAME, SwitchCommand<NewRecordCommand> }
        };
    }

    protected Task SwitchCommand<T>(IUserWorkflowManager manager, CommandContext context) 
        where T : CommandBase, new()
    {
        var commandInstance = new T();
        commandInstance.Initialize(Container);
        manager.CurrentCommand = commandInstance;
        return Task.CompletedTask;
    }

    protected virtual Task DefaultAction(IUserWorkflowManager manager, CommandContext context)
    {
        SwitchCommand<RootCommand>(manager, context);
        return Task.CompletedTask;
    }

    public async Task Handle(IUserWorkflowManager manager, CommandContext context)
    {
        if (!Transitions.TryGetValue(context.LatestInputFromUser, out var action))
        {
            action = DefaultAction;
        }
        await action(manager, context);
    }
}