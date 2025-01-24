using Bot.Commands;
using Bot.Models;

namespace Bot.Services;

public sealed class UserWorkflowManager : IUserWorkflowManager
{
    private readonly ILogger<UserWorkflowManager> _logger;
    public string UserName { get; }
    public CommandBase CurrentCommand { get; set; }

    public UserWorkflowManager(
        ILogger<UserWorkflowManager> logger,
        IServiceProvider serviceProvider,
        string userName)
    {
        _logger = logger;
        UserName = userName;
        var command = new RootCommand();
        command.Initialize(serviceProvider);
        CurrentCommand = command;
    }

    public async Task HandleInput(CommandContext context)
    {
        _logger.LogDebug("Pass handling to \"{CurrentCommand}\" command", CurrentCommand.GetType().Name);
        await CurrentCommand.Handle(this, context);
    }
}