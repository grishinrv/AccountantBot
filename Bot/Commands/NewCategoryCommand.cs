using Bot.Services;

namespace Bot.Commands;

/// <summary>
/// ny_kategori - Skapa en ny kategori
/// </summary>
public sealed class NewCategoryCommand : CommandBase
{
    public const string COMMAND_NAME =  "/ny_kategori";
    public override string Name => COMMAND_NAME;

    protected override void DefaultAction(IUserWorkflowManager manager, string userInput)
    {
    }
}