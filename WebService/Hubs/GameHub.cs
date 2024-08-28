using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace BHG.WebService
{
    public class GameHub : Hub
    {
        protected const string RoomRouteKey = "roomCode";
        public const string RoomSendMsg = "RoomSend";
        public const string RoomSendLog = "RoomSendLog";
        public const string RoomJoinedMsg = "RoomJoined";
        public const string RoomSendData = "RoomDataSend";
        public const string RoomSendDiscussTime = "RoomDiscussTime";
        public const string RoomSendPlayerDead = "RoomSendPlayerDead";
        public const string RoomSendPlayerVote = "RoomSendPlayerVote";

        private static readonly Dictionary<string, string> UserSession = [];

        protected string GetRoomCode()
        {
            var httpContext = Context.GetHttpContext();
            if (!httpContext.Request.Path.HasValue) return null;

            var splited = httpContext.Request.Path.Value.Split('/');
            if (splited.Length != 3) return null;

            return splited[2];
        }

        protected string GetUserName()
        {
            return UserSession.TryGetValue(Context.ConnectionId, out string val) ? val : null;
        }

        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();

            string roomCode = GetRoomCode();
            if (!string.IsNullOrEmpty(roomCode))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomCode);
                await Groups.AddToGroupAsync(Context.ConnectionId, roomCode);

                await Clients.Group(roomCode).SendAsync(RoomSendLog, $"System: {Context.ConnectionId} has joined the room '{roomCode}'.");
            }
        }

        public async Task SetUserName(string userName)
        {
            if (!UserSession.ContainsKey(Context.ConnectionId))
            {
                lock (UserSession)
                {
                    UserSession[Context.ConnectionId] = userName;
                }
            }

            string roomCode = GetRoomCode();

            await Clients.Group(roomCode).SendAsync(RoomSendLog, $"System: {Context.ConnectionId} is user '{userName}'.");
        }

        public async Task RemoveFromRoom(string roomName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomName);

            string userName = GetUserName();
            await Clients.Group(roomName).SendAsync(RoomSendLog, $"System: {userName ?? Context.ConnectionId} has left the room '{roomName}'.");
        }

        public async Task SendMessageRoom(string message)
        {
            string userName = GetUserName();
            await Clients.Group(GetRoomCode()).SendAsync(RoomSendLog, $"{userName ?? Context.ConnectionId}: {message}");
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            string roomName = (string)Context.GetHttpContext().GetRouteValue("roomName");

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomName);

            string userName = GetUserName();
            await Clients.Group(roomName).SendAsync(RoomSendLog, $"System: {userName ?? Context.ConnectionId} has left the room '{roomName}'.");

            if (!string.IsNullOrEmpty(userName))
            {
                var room = DyingMessageGameManager.GetInstance().LeaveRoomSession(roomName, userName);

                await Clients.Group(roomName).SendAsync(RoomSendData, room);
            }

            lock (UserSession)
            {
                UserSession.Remove(Context.ConnectionId);
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
