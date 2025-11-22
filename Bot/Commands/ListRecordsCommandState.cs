namespace Bot.Commands;

public enum ListRecordsCommandState
{
    WaitingForPeriod = 0,
    WaitingForFieldsToInclude = 1,
    WaitingForFilter = 2
}
