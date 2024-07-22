using Microsoft.AspNetCore.SignalR;

namespace BHG.WebService
{
    public class RoomHub : Hub
    {
        public async Task SendMessage(string user, string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }
    }
}
