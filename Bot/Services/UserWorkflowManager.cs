using Bot.Commands;

namespace Bot.Services;

public sealed class UserWorkflowManager : IUserWorkflowManager
{
    public string UserName { get; }

    public CommandBase CurrentCommand { get; set; }

    public UserWorkflowManager(string userName, IServiceProvider serviceProvider)
    {
        UserName = userName;
        var command = new RootCommand();
        command.Initialize(serviceProvider);
        CurrentCommand = command;
    }

    public async Task HandleInput(string text)
    {
        await CurrentCommand.Handle(this, text);
    }
}