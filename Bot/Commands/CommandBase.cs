using Bot.Services;

namespace Bot.Commands;

public abstract class CommandBase
{
    public abstract string Name { get; }
    protected IServiceProvider Container { get; private set; }
    protected Dictionary<string, Action<IUserWorkflowManager, string>> Transitions { get; }

    public void Initialize(IServiceProvider serviceProvider)
    {
        Container = serviceProvider;
    }

    protected CommandBase()
    {
        Transitions = new Dictionary<string, Action<IUserWorkflowManager, string>>
        {
            { RootCommand.COMMAND_NAME, SwitchCommand<RootCommand> },
            { NewCategoryCommand.COMMAND_NAME, SwitchCommand<NewCategoryCommand> },
            { NewRecordCommand.COMMAND_NAME, SwitchCommand<NewRecordCommand> }
        };
    }

    protected void SwitchCommand<T>(IUserWorkflowManager manager, string commandName) 
        where T : CommandBase, new()
    {
        var commandInstance = new T();
        commandInstance.Initialize(Container);
        manager.CurrentCommand = commandInstance;
    }

    protected virtual void DefaultAction(IUserWorkflowManager manager, string userInput) =>
        SwitchCommand<RootCommand>(manager, userInput);

    public void Handle(IUserWorkflowManager manager, string userInput)
    {
        if (!Transitions.TryGetValue(userInput, out var action))
        {
            action = DefaultAction;
        }
        action(manager, userInput);
    }
}