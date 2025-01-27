using Bot.Models;
using Bot.Services;
using Telegram.Bot;

namespace Bot.Commands;

public abstract class CommandBase
{
    protected const string BUTTON_CANCEL = "Abvoka";
    public abstract string Name { get; }
    private IUserWorkflowManager _userWorkflowManager = null!;
    protected TelegramBotClient Bot { get; }
    protected Dictionary<string, Func<CommandContext, Task>> Transitions { get; }

    public void Initialize(IUserWorkflowManager userWorkflowManager)
    {
        _userWorkflowManager = userWorkflowManager;
    }

    protected virtual Task OnInitializedAsync(CommandContext context)
    {
        return Task.CompletedTask;
    }

    protected CommandBase(TelegramBotClient bot)
    {
        Bot = bot;
        Transitions = new Dictionary<string, Func<CommandContext, Task>>
        {
            { RootCommand.COMMAND_NAME, SwitchCommand<RootCommand> },
            { NewCategoryCommand.COMMAND_NAME, SwitchCommand<NewCategoryCommand> },
            { NewRecordCommand.COMMAND_NAME, SwitchCommand<NewRecordCommand> },
            { GetStatisticsCommand.COMMAND_NAME, SwitchCommand<GetStatisticsCommand> },
            { ListRecordsCommand.COMMAND_NAME, SwitchCommand<ListRecordsCommand> },
            { BUTTON_CANCEL, SwitchCommand<RootCommand> }
        };
    }

    protected async Task SwitchCommand<T>(CommandContext context) 
        where T : CommandBase
    {
        var commandInstance = _userWorkflowManager.SwitchCommand<T>();
        await commandInstance.OnInitializedAsync(context);
    }

    protected virtual Task DefaultAction(CommandContext context)
    {
        return SwitchCommand<RootCommand>(context);
    }

    public async Task Handle(CommandContext context)
    {
        if (!Transitions.TryGetValue(context.LatestInputFromUser, out var action))
        {
            action = DefaultAction;
        }
        await action(context);
    }
}