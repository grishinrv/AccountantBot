using Bot.Commands;

namespace Bot.Services;

public sealed class UserWorkflowManager : IUserWorkflowManager
{
    public string UserName { get; }

    public CommandBase CurrentCommand { get; set; }

    public UserWorkflowManager(string userName)
    {
        UserName = userName;
    }

    public void HandleInput(string text)
    {
        CurrentCommand.Handle(this, text);
    }
}