namespace Bot.Models;

public sealed class SelectedOptionsEventArgs<TOPtions> : EventArgs
    where TOPtions : struct, Enum
{
    public required TOPtions Options { get; init; }
    public required CommandContext Context { get; init; }
}