namespace Bot.Models;

public sealed class CheckboxItemModel
{
    public bool IsChecked { get; set; }
    public required string DisplayName { get; init; }
    public required string Callback { get; init; }
    public required int CorrespondingValue { get; init; }
}