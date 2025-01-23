using Bot.Commands;
using Bot.Models;

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

    public async Task HandleInput(CommandContext context)
    {
        await CurrentCommand.Handle(this, context);
    }
}