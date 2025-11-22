namespace Bot.Commands;

public enum CorrectRecordCommandState
{
    WaitingForPeriod = 0,
    WaitingForFieldsToInclude = 1,
    WaitingForFilter = 2,
    WaitingForId = 3,
    WaitingForCategory = 4,
    WaitingForAmount = 5,
    WaitingForComment = 6
}