namespace BHG.WebService.Data
{
    public enum GameState : byte
    {
        Waiting = 1,
        Start = 2,
        CloseEye = 3,
        KillerTurn = 4,
        ProtectorTurn = 5,
        LeaveDyingMessageTime = 6,
        LeaveFakeEvidenceTime = 7,
        DiscussTime = 8,
        VoteOutTime = 9,
        VoteKillTime = 10,
        GameOver = 11,
    }
}
