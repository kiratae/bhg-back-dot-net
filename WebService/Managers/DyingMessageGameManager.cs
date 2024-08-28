using Microsoft.AspNetCore.SignalR;
using System.Drawing;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BHG.WebService
{
    public class DyingMessageGameManager
    {
        private static DyingMessageGameManager _instance = null;

        public const int DefaultDiscussTime = 300;

        protected static readonly Dictionary<string, Room> _roomSession = [];

        private DyingMessageGameManager()
        {

        }

        public static DyingMessageGameManager GetInstance()
        {
            _instance ??= new DyingMessageGameManager();
            return _instance;
        }

        public void ClearInactiveSessions()
        {
            DateTime now = DateTime.Now;
            foreach (var key in _roomSession.Keys)
            {
                var room = _roomSession[key];
                DateTime lastModifyDate = room.ModifyDate ?? room.CreateDate;
                TimeSpan inactiveTime = now - lastModifyDate;
                if (inactiveTime.Minutes >= 30)
                {
                    lock (_roomSession)
                    {
                        _roomSession.Remove(key);
                    }
                }
            }
        }

        public List<Room> GetActiveSession()
        {
            return [.. _roomSession.Values];
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
                    value.ModifyDate = DateTime.Now;
                }
            }
            return value;
        }

        public Room LeaveRoomSession(string roomCode, string userName)
        {
            if (!_roomSession.TryGetValue(roomCode, out Room value)) return null;

            lock (value)
            {
                lock (value.Players)
                {
                    int index = value.Players.FindIndex(x => x.UserName == userName);
                    if (index != -1)
                    {
                        value.Players.RemoveAt(index);
                    }

                    value.ModifyDate = DateTime.Now;
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
                        room.ModifyDate = DateTime.Now;
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
                    room.ModifyDate = DateTime.Now;
                }
                await hubContext.Clients.Group(room.RoomCode).SendAsync(GameHub.RoomSendMsg, $"System: Game has beed start.");
                await hubContext.Clients.Group(room.RoomCode).SendAsync(GameHub.RoomSendData, room);

                _ = Task.Run(async () =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(3));
                    lock (room)
                    {
                        room.GameStateId = room.GetStartRoundGameState();
                        room.ModifyDate = DateTime.Now;
                    }
                    await hubContext.Clients.Group(room.RoomCode).SendAsync(GameHub.RoomSendMsg, $"System: Killer turn.");
                    await hubContext.Clients.Group(room.RoomCode).SendAsync(GameHub.RoomSendData, room);

                }).ConfigureAwait(false);
            }
            return room;
        }

        public async Task<Room> BackToLobby(string roomCode, IHubContext<GameHub> hubContext)
        {
            var room = GetRoomSession(roomCode);
            if (room != null)
            {
                lock (room)
                {
                    room.CardDecks.Clear();
                    room.Cards.Clear();

                    room.ClearRoomLog();

                    for (int i = 0; i < room.Players.Count; i++)
                    {
                        lock (room.Players[i])
                        {
                            room.Players[i].StatusId = PlayerStatus.Unknown;
                            room.Players[i].RoleId = PlayerRole.Unknown;
                            room.Players[i].IsProtected = false;
                        }
                    }
                    room.GameStateId = GameState.Waiting;
                    room.ModifyDate = DateTime.Now;
                }
                await hubContext.Clients.Group(room.RoomCode).SendAsync(GameHub.RoomSendMsg, $"System: Game has beed start.");
                await hubContext.Clients.Group(room.RoomCode).SendAsync(GameHub.RoomSendData, room);
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
                room.ModifyDate = DateTime.Now;
            }
            await hubContext.Clients.Group(room.RoomCode).SendAsync(GameHub.RoomSendMsg, $"System: {targetUserName} is dying he/she will choose evidence.");

            if (!await CheckGameOver(roomCode, hubContext))
            {
                PrepareEvidences(room);

                lock (room)
                {
                    room.GameStateId = GameState.LeaveDyingMessageTime;
                    room.ModifyDate = DateTime.Now;
                }

                await hubContext.Clients.Group(room.RoomCode).SendAsync(GameHub.RoomSendData, room);
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
                    room.GameStateId = killerTeamWin ? GameState.GameOverKillerWin : GameState.GameOverCivilianWin;
                    room.ModifyDate = DateTime.Now;
                }

                await hubContext.Clients.Group(room.RoomCode).SendAsync(GameHub.RoomSendMsg, $"System: {(killerTeamWin ? "Killer" : "Civilian")} team is win.");
                await hubContext.Clients.Group(room.RoomCode).SendAsync(GameHub.RoomSendData, room);
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
                room.ModifyDate = DateTime.Now;
            }
            await hubContext.Clients.Group(room.RoomCode).SendAsync(GameHub.RoomSendData, room);

            return room;
        }

        public async Task<Room> DyingChooseEvidence(string roomCode, int cardId, IHubContext<GameHub> hubContext)
        {
            var room = GetRoomSession(roomCode) ?? throw new ArgumentOutOfRangeException(roomCode);
            var dyingPlayer = room.GetPlayer(PlayerStatus.Dying);

            lock (room)
            {
                var card = room.Cards[room.GameRound].Find(x => x.CardId == cardId) ?? throw new Exception($"Card id {cardId} not found in HandCards of room code {roomCode}.");

                lock (card)
                {
                    card.StatusId = CardStatus.RealEvidence;
                }

                lock (dyingPlayer)
                {
                    dyingPlayer.StatusId = PlayerStatus.Dead;
                }

                room.GameStateId = GameState.LeaveFakeEvidenceTime;
                room.ModifyDate = DateTime.Now;
            }
            await hubContext.Clients.Group(room.RoomCode).SendAsync(GameHub.RoomSendData, room);

            return room;
        }

        public async Task<Room> KillerChooseFakeEvidences(string roomCode, List<int> cardIds, IHubContext<GameHub> hubContext)
        {
            var room = GetRoomSession(roomCode) ?? throw new ArgumentOutOfRangeException(roomCode);

            lock (room)
            {
                foreach (var cardId in cardIds)
                {
                    var card = room.Cards[room.GameRound].Find(x => x.CardId == cardId) ?? throw new Exception($"Card id {cardId} not found in HandCards of room code {roomCode}.");

                    lock (card)
                    {
                        card.StatusId = CardStatus.FakeEvidence;
                    }
                }

                var discardedCardIndexes = new HashSet<int>();
                lock (room.Cards[room.GameRound])
                {
                    for (int i = 0; i < room.Cards[room.GameRound].Count; i++)
                    {
                        var card = room.Cards[room.GameRound][i];
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

                    room.Cards[room.GameRound] = ShuffleCards(room.Cards[room.GameRound]);
                }

                room.GameStateId = GameState.DiscussTime;
                room.DiscussTimeRemain = DefaultDiscussTime;
                room.ModifyDate = DateTime.Now;
            }
            await hubContext.Clients.Group(room.RoomCode).SendAsync(GameHub.RoomSendData, room);


            _ = Task.Run(async () =>
            {
                for (int i = room.DiscussTimeRemain; i > 0; i--)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));
                    lock (room)
                    {
                        room.DiscussTimeRemain = i;
                        room.ModifyDate = DateTime.Now;
                    }
                    await hubContext.Clients.Group(room.RoomCode).SendAsync(GameHub.RoomSendDiscussTime, i);
                }

                await Task.Delay(TimeSpan.FromSeconds(1));

                lock (room)
                {
                    room.GameStateId = GameState.VoteHanging;
                    room.ClearRoomLog();
                    room.ModifyDate = DateTime.Now;
                }
                await hubContext.Clients.Group(room.RoomCode).SendAsync(GameHub.RoomSendMsg, $"System: Time to vote killer.");
                await hubContext.Clients.Group(room.RoomCode).SendAsync(GameHub.RoomSendData, room);
            }).ConfigureAwait(false);


            return room;
        }

        public async Task<Room> VoteHanging(string roomCode, string userName, string targetUserName, IHubContext<GameHub> hubContext)
        {
            var room = GetRoomSession(roomCode) ?? throw new ArgumentOutOfRangeException(roomCode);
            var player = room.GetPlayer(userName) ?? throw new ArgumentOutOfRangeException(userName);
            var targetPlayer = room.GetPlayer(targetUserName) ?? throw new ArgumentOutOfRangeException(targetUserName);

            lock (room)
            {
                if (!room.PlayerVoteLogs.Contains(player.UserName))
                {
                    room.VoteHangingLogs.Add(targetPlayer.UserName);
                    room.PlayerVoteLogs.Add(player.UserName);
                }
                room.ModifyDate = DateTime.Now;
            }

            // Everyone is voted.
            if (room.PlayerVoteLogs.Count == room.GetAlivePlayers().Count())
            {
                int voteSize = room.VoteHangingLogs.Count;
                string candidate = FindCandidate(room.VoteHangingLogs, voteSize);
                if (IsMajority(room.VoteHangingLogs, voteSize, candidate))
                {
                    var candidatePlayer = room.GetPlayer(candidate);

                    lock (room)
                    {
                        room.GameRound++;
                        room.GameStateId = room.GetStartRoundGameState();
                        room.ClearRoomLog();

                        lock (candidatePlayer)
                        {
                            candidatePlayer.StatusId = PlayerStatus.Dead;
                        }

                        room.ModifyDate = DateTime.Now;
                    }

                    await CheckGameOver(roomCode, hubContext);
                }
                else
                {
                    lock (room)
                    {
                        room.GameRound++;
                        room.GameStateId = room.GetStartRoundGameState();
                        room.ClearRoomLog();
                        room.ModifyDate = DateTime.Now;
                    }
                }
            }
            await hubContext.Clients.Group(room.RoomCode).SendAsync(GameHub.RoomSendMsg, $"System: {userName} vote {targetUserName}.");
            await hubContext.Clients.Group(room.RoomCode).SendAsync(GameHub.RoomSendData, room);

            return room;
        }

        protected Room PrepareEvidences(Room room)
        {
            ArgumentNullException.ThrowIfNull(room);

            int maxPrepareCard = 9;

            int cardDeckQty = room.CardDecks.Count;
            var cardIndexes = new HashSet<int>();
            var cardIds = new HashSet<int>();
            var cards = new List<Card>();
            for (int i = 0; i < maxPrepareCard; i++)
            {
                int cardIndex = GetRandomIndex(cardDeckQty, cardIndexes);
                cardIndexes.Add(cardIndex);
                var card = room.CardDecks[cardIndex];
                cards.Add(card);
                cardIds.Add(card.CardId);
            }

            lock (room)
            {
                lock (room.Cards)
                {
                    room.Cards[room.GameRound] = cards;
                }

                lock (room.CardDecks)
                {
                    foreach (var cardId in cardIds)
                    {
                        var index = room.CardDecks.FindIndex(x => x.CardId == cardId);
                        room.CardDecks.RemoveAt(index);
                    }
                }

                room.ModifyDate = DateTime.Now;
            }

            return room;
        }

        protected List<Card> ShuffleCards(List<Card> cards)
        {
            int cardDeckQty = cards.Count;
            var cardIndexes = new HashSet<int>();
            var list = new List<Card>();
            for (int i = 0; i < cardDeckQty; i++)
            {
                int cardIndex = GetRandomIndex(cardDeckQty, cardIndexes);
                cardIndexes.Add(cardIndex);
                var card = cards[cardIndex];
                list.Add(card);
            }
            return cards;
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

        private static string FindCandidate(IEnumerable<string> list, int size)
        {
            int maj_index = 0, count = 1;
            int i;
            for (i = 1; i < list.Count(); i++)
            {
                if (list.ElementAt(maj_index) == list.ElementAt(i))
                    count++;
                else
                    count--;

                if (count == 0)
                {
                    maj_index = i;
                    count = 1;
                }
            }
            return list.ElementAt(maj_index);
        }

        private static bool IsMajority(IEnumerable<string> list, int size, string cand)
        {
            int i, count = 0;
            for (i = 0; i < size; i++)
            {
                if (list.ElementAt(i) == cand)
                    count++;
            }
            if (count > size / 2)
                return true;
            else
                return false;
        }
    }
}
