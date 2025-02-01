using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Bot.Models;
using Bot.Utils;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace Bot.Services;

public sealed class OptionsProviderService<TOptions> : IOptionsProviderService<TOptions>
    where TOptions : struct, Enum
{
    private readonly CheckboxItemModel[] _checkboxes;
    private readonly TelegramBotClient _bot;
    private const string PREFIX_CHECK = "check_";
    public event EventHandler<SelectedOptionsEventArgs<TOptions>>? OptionsSelected;
    
    public OptionsProviderService(TelegramBotClient bot)
    {
        _bot = bot;
        _checkboxes = ((TOptions[])Enum.GetValues(typeof(TOptions)))
            .Where(x => TryGetDisplayName(x, out _))
            .Select(x =>
            {
                TryGetDisplayName(x, out var name);
                var correspondingValue = x.GetHashCode();
                return new CheckboxItemModel
                {
                    DisplayName = name!,
                    Callback = PREFIX_CHECK + correspondingValue,
                    CorrespondingValue = correspondingValue
                };
            })
            .ToArray();
    }
    
    private static bool TryGetDisplayName<T>(T value, [NotNullWhen(true)] out string? name) where T : Enum
    {
        var field = value.GetType().GetField(value.ToString())!;
        DisplayAttribute? attribute = field.GetCustomAttribute<DisplayAttribute>();
        name = attribute?.Name;
        return name != null; 
    }


    public void RegisterTransitions(Dictionary<string, Func<CommandContext, Task>> commandTransitions)
    {
        foreach (var checkboxItem in _checkboxes)
        {
            commandTransitions.Add(checkboxItem.Callback, OnCheckBoxChecked);
        }
        commandTransitions.Add(KeyboardFactory.CALLBACK_APPLY, OnApplied);
    }

    public async Task PromptOptions(CommandContext context)
    {
        await _bot.SendMessage(
            chatId: context.ChatId,
            text: "Kontrollera alternativen för att aktivera:",
            parseMode: ParseMode.Markdown,
            replyMarkup: KeyboardFactory.GetCheckBoxList(_checkboxes));
    }
    
    private async Task OnCheckBoxChecked(CommandContext context)
    {
        var item = _checkboxes.First(x => x.Callback == context.LatestInputFromUser);
        item.IsChecked = !item.IsChecked;

        var keyboard = KeyboardFactory.GetCheckBoxList(_checkboxes);
        await _bot.EditMessageReplyMarkup(
            chatId: context.ChatId,
            messageId: context.CallBackMessageId!.Value,
            replyMarkup: keyboard);
    }
    
    private Task OnApplied(CommandContext context)
    {
        var totalSelectedOptions = _checkboxes
            .Where(x => x.IsChecked)
            .Select(x => x.CorrespondingValue)
            .Sum();

        var eventArgs = new SelectedOptionsEventArgs<TOptions>
        {
            Options = (TOptions)Enum.ToObject(typeof(TOptions), totalSelectedOptions),
            Context = context
        };

        OptionsSelected?.Invoke(this, eventArgs);
        return Task.CompletedTask;
    }
}