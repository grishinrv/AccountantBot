using Bot.Commands;
using Bot.Models;

namespace Bot.Services;

public sealed class UserWorkflowManager : IUserWorkflowManager
{
    private readonly ILogger<UserWorkflowManager> _logger;
    private readonly IServiceProvider _container;
    public string UserName { get; }
    
    private CommandBase _currentCommand = null!;

    private CommandBase CurrentCommand
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
        IServiceProvider container,
        string userName)
    {
        _logger = logger;
        _container = container;
        UserName = userName;
        var command = _container.GetRequiredService<RootCommand>();
        command.Initialize(this);
        CurrentCommand = command;
    }

    public CommandBase SwitchCommand<T>() 
        where T : CommandBase
    {
        var commandInstance = _container.GetRequiredService<T>();
        commandInstance.Initialize(this);
        CurrentCommand = commandInstance;
        return commandInstance;
    }
    
    public async Task HandleInput(CommandContext context)
    {
        _logger.LogDebug("Pass handling to \"{CurrentCommand}\" command, input: \"{InputText}\"", 
            CurrentCommand.GetType().Name,
            context.LatestInputFromUser);
        await CurrentCommand.Handle(context);
    }
}