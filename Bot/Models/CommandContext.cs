namespace Bot.Models;

public sealed class CommandContext
{
    public required string UserName { get; init; }
    public required long ChatId { get; init; }
    public required string LatestInputFromUser { get; init; }
}