namespace Bot.Models;

public sealed record Period
{
    public DateOnly Start { get; init; }
    public DateOnly End { get; init; }
}