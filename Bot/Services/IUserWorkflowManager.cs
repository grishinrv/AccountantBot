using Bot.Commands;

namespace Bot.Services;

public interface IUserWorkflowManager
{
     string UserName { get; }
     CommandBase CurrentCommand { get; set; }
}