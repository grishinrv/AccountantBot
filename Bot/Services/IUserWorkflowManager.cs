using Bot.Commands;
using Bot.Models;

namespace Bot.Services;

public interface IUserWorkflowManager
{
     string UserName { get; }
     CommandBase CurrentCommand { get; set; }
     Task HandleInput(CommandContext context);
}