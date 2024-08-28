using System.Text.Json;
using System.Text.Json.Serialization;

namespace BHG.WebService
{
    public class Room
    {
        public Room(string roomCode)
        {
            RoomCode = roomCode;
            GameStateId = GameState.Waiting;
            CreateDate = DateTime.Now;
        }

        public string RoomCode { get; set; }

        public GameState GameStateId { get; set; }

        public int GameRound { get; set; }

        public List<Player> Players { get; protected set; } = [];

        public Dictionary<int, List<Card>> Cards { get; protected set; } = [];

        [JsonIgnore]
        public List<Card> CardDecks { get; protected set; } = [];

        public List<Card> DiscardCards { get; protected set; } = [];

        public List<PlayerRole> ExtraRoles { get; protected set; } = [];

        [JsonIgnore]
        public DateTime CreateDate { get; protected set; }

        [JsonIgnore]
        public DateTime? ModifyDate { get; set; }

        [JsonIgnore]
        public bool HasDogJarvisRole => ExtraRoles.Any(x => x == PlayerRole.DogJarvis);

        public List<string> PlayerVoteLogs { get; set; } = [];

        public List<string> VoteHangingLogs { get; set; } = [];

        public int DiscussTimeRemain { get; set; }

        public bool IsPlayerInRoom(string userName)
        {
            return Players.Any(x => x.UserName == userName);
        }

        public Player GetPlayer(string userName)
        {
            return Players.Find(x => x.UserName == userName);
        }

        public Player GetPlayer(PlayerRole roleId)
        {
            return Players.Find(x => x.RoleId == roleId);
        }

        public Player GetPlayer(PlayerStatus statusId)
        {
            return Players.Find(x => x.StatusId == statusId);
        }

        public IEnumerable<Player> GetAlivePlayers()
        {
            return Players.Where(x => x.StatusId == PlayerStatus.Alive);
        }

        public bool IsHostPlayer(string userName)
        {
            if (!IsPlayerInRoom(userName)) return false;

            var player = GetPlayer(userName);
            return player != null && player.IsHost;
        }

        public bool IsPlayerInRole(string userName, PlayerRole roleId)
        {
            if (!IsPlayerInRoom(userName)) return false;

            var player = GetPlayer(userName);
            return player != null && player.RoleId == roleId;
        }

        public bool IsPlayerStatus(string userName, PlayerStatus statusId)
        {
            if (!IsPlayerInRoom(userName)) return false;

            var player = GetPlayer(userName);
            return player != null && player.StatusId == statusId;
        }

        public void ClearRoomLog()
        {
            DiscussTimeRemain = 0;
            PlayerVoteLogs.Clear();
        }

        public GameState GetStartRoundGameState()
        {
            return HasDogJarvisRole? GameState.ProtectorTurn: GameState.KillerTurn;
        }
    }
}
