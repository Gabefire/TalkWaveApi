using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TalkWaveApi.WebSocket.Models;


namespace TalkWaveApi.WebSocket.Hubs
{
    [Authorize]
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
        public async Task SendMessage(string groupId, string message)
        {
            string? userEmail = Context.UserIdentifier;

            if (userEmail == null)
            {
                Context.Abort();
            }
            MessageDto messageDto = new()
            {
                Author = userEmail,
                Content = message,
                CreatedAt = DateTime.UtcNow,
            };


            await Clients.Group(groupId).SendAsync("ReceiveMessage", userEmail, messageDto);
        }
    };
}

