using Microsoft.AspNetCore.SignalR;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BHG.WebService
{
    public class DyingMessageGameManager
    {
        private static DyingMessageGameManager _instance = null;

        protected static readonly Dictionary<string, Room> _roomSession = [];

        private DyingMessageGameManager()
        {

        }

        public static DyingMessageGameManager GetInstance()
        {
            _instance ??= new DyingMessageGameManager();
            return _instance;
        }

        public Room CreateSession(string roomCode, string hostUserName)
        {
            var room = new Room(roomCode);
            room.Players.Add(new Player(hostUserName) { IsHost = true });

            lock (_roomSession)
            {
                _roomSession[roomCode] = room;
            }

            return room;
        }

        public Room GetRoomSession(string roomCode)
        {
            if (_roomSession.TryGetValue(roomCode, out Room value))
            {
                return value;
            }
            return null;
        }

        public Room JoinRoomSession(string roomCode, string userName)
        {
            if (!_roomSession.TryGetValue(roomCode, out Room value)) return null;

            var player = new Player(userName);
            lock (value)
            {
                value.Players.Add(player);
            }
            return value;
        }

        public async Task<Room> StartGame(string roomCode, IHubContext<GameHub> hubContext)
        {
            var room = GetRoomSession(roomCode);
            if (room != null)
            {
                var cardDecks = CreateCardDecks();
                lock (room)
                {
                    // Random player
                    var killerPlayerIndex = new Random().Next(0, room.Players.Count - 1);
                    for (int i = 0; i < room.Players.Count; i++)
                    {
                        if (i == killerPlayerIndex)
                            room.Players[i].RoleId = PlayerRole.Killer;
                        else
                            room.Players[i].RoleId = PlayerRole.Civilian;
                    }
                    room.GameStateId = GameState.Start;
                    room.GameRound = 1;
                    room.CardDecks.AddRange(cardDecks);
                }
                await hubContext.Clients.Group(room.RoomCode).SendAsync(GameHub.RoomSendMsg, JsonSerializer.Serialize(room));

                _ = Task.Run(async () =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(3));
                    lock (room)
                    {
                        room.GameStateId = GameState.KillerTurn;
                    }
                    await hubContext.Clients.Group(room.RoomCode).SendAsync(GameHub.RoomSendMsg, JsonSerializer.Serialize(room));

                }).ConfigureAwait(false);
            }
            return room;
        }

        protected List<Card> CreateCardDecks()
        {
            var list = new List<Card>();

            DirectoryInfo di = new("./wwwroot/cards");
            if (di.Exists)
            {
                var files = di.GetFiles();
                int index = 0;
                foreach (var file in files)
                {
                    list.Add(new Card() { CardId = index++, FileName = string.Format("~/cards/{0}", file.Name), StatusId = CardStatus.Unknown });
                }
            }

            return list;
        }

    }
}
