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
            return (string)Context.GetHttpContext().GetRouteValue(RoomRouteKey);
        }

        protected string GetUserName()
        {
            return UserSession.TryGetValue(Context.ConnectionId, out string val) ? val : null;
        }

        public override async Task OnConnectedAsync()
        {
            string roomCode = GetRoomCode();

            await Groups.AddToGroupAsync(Context.ConnectionId, roomCode);

            await base.OnConnectedAsync();

            await Clients.Group(roomCode).SendAsync(RoomSendMsg, $"System: {Context.ConnectionId} has joined the room '{roomCode}'.");
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
