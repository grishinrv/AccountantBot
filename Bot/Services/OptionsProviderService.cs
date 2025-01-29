using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Bot.Models;
using Telegram.Bot;

namespace Bot.Services;

public sealed class OptionsProviderService<TOptions> : IOptionsProviderService<TOptions>
    where TOptions : struct, Enum
{
    private readonly CheckboxItemModel[] _checkboxes;
    private readonly TelegramBotClient _bot;
    private const string PREFIX_CHECK = "check_";
    
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
}