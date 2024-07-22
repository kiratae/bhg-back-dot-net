using Microsoft.AspNetCore.SignalR;

namespace BHG.WebService
{
    public class GameHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            string roomName = (string)Context.GetHttpContext().GetRouteValue("roomName");

            await Groups.AddToGroupAsync(Context.ConnectionId, roomName);

            await base.OnConnectedAsync();
        }

        public async Task SendMessage(string user, string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }

        public async Task AddToRoom(string roomName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, roomName);

            await Clients.Group(roomName).SendAsync("Send", $"{Context.ConnectionId} has joined the room '{roomName}'.");
        }

        public async Task RemoveFromRoom(string roomName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomName);

            await Clients.Group(roomName).SendAsync("Send", $"{Context.ConnectionId} has left the room '{roomName}'.");
        }

        public async Task SendMessageToRoom(string roomName, string message)
        {
            await Clients.Group(roomName).SendAsync("Send", message);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
        }
    }
}
