using Bot.Commands;
using Bot.Models;

namespace Bot.Services;

public interface IUserWorkflowManager
{
     string UserName { get; }
     CommandBase SwitchCommand<T>() where T : CommandBase;
     Task HandleInput(CommandContext context);
}