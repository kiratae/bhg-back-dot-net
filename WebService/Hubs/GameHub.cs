using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace BHG.WebService
{
    public class GameHub : Hub
    {
        protected const string RoomRouteKey = "roomCode";
        public const string RoomSendMsg = "RoomSend";
        public const string RoomSendData = "RoomDataSend";

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

                await Clients.Group(roomCode).SendAsync(RoomSendMsg, $"System: {Context.ConnectionId} has joined the room '{roomCode}'.");
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

            await Clients.Group(roomCode).SendAsync(RoomSendMsg, $"System: {Context.ConnectionId} is user '{userName}'.");
        }

        public async Task RemoveFromRoom(string roomName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomName);

            string userName = GetUserName();
            await Clients.Group(roomName).SendAsync(RoomSendMsg, $"System: {userName ?? Context.ConnectionId} has left the room '{roomName}'.");
        }

        public async Task SendMessageRoom(string message)
        {
            string userName = GetUserName();
            await Clients.Group(GetRoomCode()).SendAsync(RoomSendMsg, $"{userName ?? Context.ConnectionId}: {message}");
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            string roomName = (string)Context.GetHttpContext().GetRouteValue("roomName");

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomName);

            string userName = GetUserName();
            await Clients.Group(roomName).SendAsync(RoomSendMsg, $"System: {userName ?? Context.ConnectionId} has left the room '{roomName}'.");

            lock (UserSession)
            {
                UserSession.Remove(Context.ConnectionId);
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
