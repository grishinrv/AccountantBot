using Bot.Services;

namespace Bot.Commands;

public sealed class RootCommand : CommandBase
{
    public const string COMMAND_NAME =  "/start";
    public override string Name => COMMAND_NAME;

    public RootCommand()
    {
        
    }
    
    
}