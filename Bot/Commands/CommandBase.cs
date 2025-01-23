using Bot.Services;

namespace Bot.Commands;

public abstract class CommandBase
{
    public abstract string Name { get; }
    protected IServiceProvider Container { get; private set; } = null!;
    protected Dictionary<string, Func<IUserWorkflowManager, string, Task>> Transitions { get; }

    public void Initialize(IServiceProvider serviceProvider)
    {
        Container = serviceProvider;
    }

    protected CommandBase()
    {
        Transitions = new Dictionary<string, Func<IUserWorkflowManager, string, Task>>
        {
            { RootCommand.COMMAND_NAME, SwitchCommand<RootCommand> },
            { NewCategoryCommand.COMMAND_NAME, SwitchCommand<NewCategoryCommand> },
            { NewRecordCommand.COMMAND_NAME, SwitchCommand<NewRecordCommand> }
        };
    }

    protected Task SwitchCommand<T>(IUserWorkflowManager manager, string commandName) 
        where T : CommandBase, new()
    {
        var commandInstance = new T();
        commandInstance.Initialize(Container);
        manager.CurrentCommand = commandInstance;
        return Task.CompletedTask;
    }

    protected virtual Task DefaultAction(IUserWorkflowManager manager, string userInput)
    {
        SwitchCommand<RootCommand>(manager, userInput);
        return Task.CompletedTask;
    }

    public async Task Handle(IUserWorkflowManager manager, string userInput)
    {
        if (!Transitions.TryGetValue(userInput, out var action))
        {
            action = DefaultAction;
        }
        await action(manager, userInput);
    }
}