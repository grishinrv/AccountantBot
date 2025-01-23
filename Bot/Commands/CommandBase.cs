using Bot.Services;

namespace Bot.Commands;

public abstract class CommandBase
{
    public abstract string Name { get; }
    
    protected Dictionary<string, Action<IUserWorkflowManager, string>> Transitions { get; }

    public CommandBase()
    {
        Transitions = new Dictionary<string, Action<IUserWorkflowManager, string>>()
        {
            { RootCommand.COMMAND_NAME, SwitchCommand<RootCommand> },
            { NewCategoryCommand.COMMAND_NAME, SwitchCommand<NewCategoryCommand> },
            { NewRecordCommand.COMMAND_NAME, SwitchCommand<NewRecordCommand> }
        };
    }

    protected void SwitchCommand<T>(IUserWorkflowManager manager, string command) where T : CommandBase, new()
    {
        manager.CurrentCommand = new T();
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