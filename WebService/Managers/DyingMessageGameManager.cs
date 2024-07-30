using Microsoft.AspNetCore.SignalR;
using System.Numerics;
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
                lock (value.Players)
                {
                    value.Players.Add(player);
                }
            }
            return value;
        }

        public void ConfigGame(string roomCode, List<PlayerRole> extraRoles)
        {
            if (extraRoles != null && extraRoles.Count > 0)
            {
                var room = GetRoomSession(roomCode);
                lock (room)
                {
                    lock (room.ExtraRoles)
                    {
                        room.ExtraRoles.Clear();
                        room.ExtraRoles.AddRange(extraRoles);
                    }
                }
            }
        }

        public async Task<Room> StartGame(string roomCode, IHubContext<GameHub> hubContext)
        {
            var room = GetRoomSession(roomCode);
            if (room != null)
            {
                var cardDecks = CreateCardDecks();
                var excludeIndexes = new HashSet<int>();
                lock (room)
                {
                    // Random player
                    int playerCount = room.Players.Count;
                    int killerPlayerIndex = GetRandomIndex(playerCount, excludeIndexes);
                    int? dogJarvisPlayerIndex = null;
                    if (room.HasDogJarvisRole)
                    {
                        excludeIndexes.Add(killerPlayerIndex);
                        dogJarvisPlayerIndex = GetRandomIndex(playerCount, excludeIndexes);
                    }
                    for (int i = 0; i < room.Players.Count; i++)
                    {
                        lock (room.Players[i])
                        {
                            if (i == killerPlayerIndex)
                                room.Players[i].RoleId = PlayerRole.Killer;
                            else if (dogJarvisPlayerIndex.HasValue && i == dogJarvisPlayerIndex.Value)
                                room.Players[i].RoleId = PlayerRole.DogJarvis;
                            else
                                room.Players[i].RoleId = PlayerRole.Civilian;
                            room.Players[i].StatusId = PlayerStatus.Alive;
                        }
                    }
                    room.GameStateId = GameState.Start;
                    room.GameRound = 1;
                    room.CardDecks.AddRange(cardDecks);
                }
                await hubContext.Clients.Group(room.RoomCode).SendAsync(GameHub.RoomSendMsg, $"System: Game has beed start.");
                await hubContext.Clients.Group(room.RoomCode).SendAsync(GameHub.RoomSendData, room.ToJsonString());

                _ = Task.Run(async () =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(3));
                    lock (room)
                    {
                        room.GameStateId = GameState.KillerTurn;
                    }
                    await hubContext.Clients.Group(room.RoomCode).SendAsync(GameHub.RoomSendMsg, $"System: Killer turn.");
                    await hubContext.Clients.Group(room.RoomCode).SendAsync(GameHub.RoomSendData, room.ToJsonString());

                }).ConfigureAwait(false);
            }
            return room;
        }

        public async Task<Room> KillerChooseTarget(string roomCode, string targetUserName, IHubContext<GameHub> hubContext)
        {
            var room = GetRoomSession(roomCode) ?? throw new ArgumentOutOfRangeException(roomCode);
            var player = room.GetPlayer(targetUserName) ?? throw new ArgumentOutOfRangeException(targetUserName);

            lock (room)
            {
                if (room.HasDogJarvisRole && player.IsProtected)
                {
                    var dogJarvisPlayer = room.GetPlayer(PlayerRole.DogJarvis) ?? throw new ArgumentOutOfRangeException("dogJarvisPlayer");
                    lock (dogJarvisPlayer)
                    {
                        dogJarvisPlayer.StatusId = PlayerStatus.Dying;
                    }
                }
                else
                {
                    lock (player)
                    {
                        player.StatusId = PlayerStatus.Dying;
                    }
                }
            }
            await hubContext.Clients.Group(room.RoomCode).SendAsync(GameHub.RoomSendMsg, $"System: {targetUserName} is dying he/she will choose evidence.");

            if (!await CheckGameOver(roomCode, hubContext))
            {
                PrepareEvidences(room);

                lock (room)
                {
                    room.GameStateId = GameState.LeaveDyingMessageTime;
                }

                await hubContext.Clients.Group(room.RoomCode).SendAsync(GameHub.RoomSendData, room.ToJsonString());
            }

            return room;
        }

        protected async Task<bool> CheckGameOver(string roomCode, IHubContext<GameHub> hubContext)
        {
            var room = GetRoomSession(roomCode) ?? throw new ArgumentOutOfRangeException(roomCode);

            int killerTeamQty = room.Players.Count(x => x.RoleId == PlayerRole.Killer && x.StatusId == PlayerStatus.Alive);
            int civilianTeamQty = room.Players.Count(x => x.RoleId != PlayerRole.Killer && x.StatusId == PlayerStatus.Alive);
            bool killerTeamWin = killerTeamQty >= civilianTeamQty;
            bool civilianTeamWin = killerTeamQty == 0;

            if (killerTeamWin || civilianTeamWin)
            {
                lock (room)
                {
                    room.GameStateId = GameState.GameOver;
                }

                await hubContext.Clients.Group(room.RoomCode).SendAsync(GameHub.RoomSendMsg, $"System: {(killerTeamWin ? "Killer" : "Civilian")} team is win.");
                await hubContext.Clients.Group(room.RoomCode).SendAsync(GameHub.RoomSendData, room.ToJsonString());
            }

            return killerTeamWin || civilianTeamWin;
        }

        public async Task<Room> DogJarvisChooseTarget(string roomCode, string targetUserName, IHubContext<GameHub> hubContext)
        {
            var room = GetRoomSession(roomCode) ?? throw new ArgumentOutOfRangeException(roomCode);

            lock (room)
            {
                var player = room.GetPlayer(targetUserName) ?? throw new ArgumentOutOfRangeException(targetUserName);
                if (!room.HasDogJarvisRole) throw new Exception($"{roomCode} is not has player role DogJarvis.");
                if (room.GetPlayer(PlayerRole.DogJarvis) == null) throw new Exception($"{roomCode} is not set a player role DogJarvis.");

                lock (player)
                {
                    player.IsProtected = true;
                }
            }
            await hubContext.Clients.Group(room.RoomCode).SendAsync(GameHub.RoomSendData, room.ToJsonString());

            return room;
        }

        public async Task<Room> DeadChooseEvidence(string roomCode, int cardId, IHubContext<GameHub> hubContext)
        {
            var room = GetRoomSession(roomCode) ?? throw new ArgumentOutOfRangeException(roomCode);

            lock (room)
            {
                var card = room.HandCards.Find(x => x.CardId == cardId) ?? throw new Exception($"Card id {cardId} not found in HandCards of room code {roomCode}.");

                lock (card)
                {
                    card.StatusId = CardStatus.RealEvidence;
                }
            }
            await hubContext.Clients.Group(room.RoomCode).SendAsync(GameHub.RoomSendData, room.ToJsonString());

            return room;
        }

        public async Task<Room> KillerChooseEvidences(string roomCode, List<int> cardIds, IHubContext<GameHub> hubContext)
        {
            var room = GetRoomSession(roomCode) ?? throw new ArgumentOutOfRangeException(roomCode);

            lock (room)
            {
                foreach (var cardId in cardIds)
                {
                    var card = room.HandCards.Find(x => x.CardId == cardId) ?? throw new Exception($"Card id {cardId} not found in HandCards of room code {roomCode}.");

                    lock (card)
                    {
                        card.StatusId = CardStatus.FakeEvidence;
                    }
                }

                var discardedCardIndexes = new HashSet<int>();
                lock (room.HandCards)
                {
                    for (int i = 0; i < room.HandCards.Count; i++)
                    {
                        var card = room.HandCards[i];
                        if (card.StatusId == CardStatus.Unknown)
                        {
                            lock (card)
                            {
                                card.StatusId = CardStatus.Discarded;
                            }

                            lock (room.DiscardCards)
                            {
                                room.DiscardCards.Add(card);
                            }
                        }
                    }

                    room.HandCards.Clear();
                }
            }
            await hubContext.Clients.Group(room.RoomCode).SendAsync(GameHub.RoomSendData, room.ToJsonString());

            return room;
        }

        protected Room PrepareEvidences(Room room)
        {
            ArgumentNullException.ThrowIfNull(room);

            int maxPrepareCard = 9;

            int cardDeckQty = room.CardDecks.Count;
            var cardIndexes = new HashSet<int>();
            var cardIds = new HashSet<int>();
            for (int i = 0; i < maxPrepareCard; i++)
            {
                cardIndexes.Add(GetRandomIndex(cardDeckQty, cardIndexes));
            }

            lock (room)
            {
                lock (room.HandCards)
                {
                    room.HandCards.Clear();

                    foreach (var cardIndex in cardIndexes)
                    {
                        var card = room.CardDecks[cardIndex];
                        room.HandCards.Add(card);
                        cardIds.Add(card.CardId);
                    }
                }

                lock (room.CardDecks)
                {
                    foreach (var cardId in cardIds)
                    {
                        var index = room.CardDecks.FindIndex(x => x.CardId == cardId);
                        room.CardDecks.RemoveAt(index);
                    }
                }
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

        protected int GetRandomIndex(int count, HashSet<int> excludeIndexes)
        {
            var range = Enumerable.Range(0, count);
            if (excludeIndexes.Count > 0)
                range = range.Where(i => !excludeIndexes.Contains(i));

            int index = new Random().Next(0, count - excludeIndexes.Count);
            return range.ElementAt(index);
        }

    }
}
