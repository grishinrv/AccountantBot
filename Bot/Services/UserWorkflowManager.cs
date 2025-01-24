using Bot.Commands;
using Bot.Models;

namespace Bot.Services;

public sealed class UserWorkflowManager : IUserWorkflowManager
{
    private readonly ILogger<UserWorkflowManager> _logger;
    public string UserName { get; }
    
    private CommandBase _currentCommand;

    public CommandBase CurrentCommand
    {
        get => _currentCommand;
        set
        {
            if (_currentCommand != value)
            {
                _logger.LogDebug("Changing current command to \"{Command}\"", value.GetType().Name);
                _currentCommand = value;
            }
        }
    }

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
        _logger.LogDebug("Pass handling to \"{CurrentCommand}\" command, input: \"{InputText}\"", 
            CurrentCommand.GetType().Name,
            context.LatestInputFromUser);
        await CurrentCommand.Handle(this, context);
    }
}