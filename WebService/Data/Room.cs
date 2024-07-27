using System.Text.Json.Serialization;

namespace BHG.WebService
{
    public class Room
    {
        public Room(string roomCode)
        {
            RoomCode = roomCode;
            GameStateId = GameState.Waiting;
        }

        public string RoomCode { get; set; }

        public GameState GameStateId { get; set; }

        public int GameRound { get; set; }

        public List<Player> Players { get; protected set; } = [];

        public Dictionary<int, List<Card>> InGameCards { get; protected set; } = [];

        [JsonIgnore]
        public List<Card> CardDecks { get; protected set; } = [];

        public List<Card> DiscardCards { get; protected set; } = [];
    }
}
