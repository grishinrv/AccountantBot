using Bot.Services;

namespace Bot.Commands;

public abstract class CommandBase
{
    public abstract string Name { get; }
    
    protected abstract IReadOnlyDictionary<string, Action<IUserWorkflowManager, string>> Transitions { get; }

    protected virtual void DefaultAction(IUserWorkflowManager manager, string userInput)
    {
        
    }

    public void Handle(IUserWorkflowManager manager, string userInput)
    {
        if (!Transitions.TryGetValue(userInput, out var action))
        {
            action = DefaultAction;
        }
        action(manager, userInput);
    }
}