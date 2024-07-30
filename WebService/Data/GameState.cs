namespace BHG.WebService
{
    public enum GameState : byte
    {
        Waiting = 1,
        Start = 2,
        KillerTurn = 3,
        ProtectorTurn = 4,
        LeaveDyingMessageTime = 5,
        LeaveFakeEvidenceTime = 6,
        DiscussTime = 7,
        VoteOutTime = 8,
        VoteKillTime = 9,
        GameOver = 10,
    }
}
