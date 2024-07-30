namespace BHG.WebService
{
    public enum DMGameAction : byte
    {
        /// <summary>
        /// Only host player can start game.
        /// </summary>
        StartGame = 1,

        /// <summary>
        /// Killer choose civilian to kill.
        /// </summary>
        KillerChooseTarget = 2,

        /// <summary>
        /// Dog Jarvis choose player to protect.
        /// </summary>
        DogJarvisChooseTarget = 3,

        /// <summary>
        /// Dead civilian choose real evidence.
        /// </summary>
        DeadChooseEvidence = 4,

        /// <summary>
        /// Killer choose 2 fake evidences.
        /// </summary>
        KillerChooseEvidences = 5,

        /// <summary>
        /// All players vote out for suspects.
        /// </summary>
        VoteKillerOut = 6,

        /// <summary>
        /// All players vote to confirm kill suspect.
        /// </summary>
        VoteConfirmKill = 7
    }
}
