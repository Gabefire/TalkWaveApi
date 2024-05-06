using Microsoft.AspNetCore.SignalR;


namespace TalkWaveApi.WebSocket.Hubs
{

    public class ChatHub : Hub
    {
        public async Task JoinGroup(string groupId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupId);
        }

        public async Task LeaveGroup(string groupId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupId);
        }

        public async Task SendMessage(string groupId, string user, string message)
        {
            await Clients.Group(groupId).SendAsync("RecieveMessage", user, message);
        }
    };
}

