using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TalkWaveApi.WebSocket.Models;
using TalkWaveApi.WebSocket.Services;


namespace TalkWaveApi.WebSocket.Hubs
{
    public class ChatHub(DatabaseContext dbContext, ILogger logger) : Hub
    {
        private readonly DatabaseContext _context = dbContext;
        private readonly ILogger _logger = logger;
        public async Task JoinGroup(string groupId)
        {
            string? userId = Context.UserIdentifier;

            _logger.LogDebug(userId);

            if (!int.TryParse(userId, out int userParsedId))
            {
                Context.Abort();
                return;
            }
            User? user = await _context.Users.FindAsync(userParsedId);
            if (user == null)
            {
                Context.Abort();
                return;
            }
            await ValidateChannel(groupId, user, Context);

            await Groups.AddToGroupAsync(Context.ConnectionId, groupId);
        }

        public async Task LeaveGroup(string groupId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupId);
        }

        public async Task SendMessage(string groupId, string message)
        {
            string? userId = Context.UserIdentifier;
            if (!int.TryParse(userId, out int userParsedId))
            {
                Context.Abort();
                return;
            }
            User? user = await _context.Users.FindAsync(userParsedId);
            await Clients.Group(groupId).SendAsync("ReceiveMessage", user?.UserName, message);
        }
        public async Task ValidateChannel(string Id, User user, HubCallerContext context)
        {
            //Validate Id can be casted as int
            if (!int.TryParse(Id, out int ChannelId))
            {
                context.Abort();
            }

            //Validate channel exists
            var channel = await _context.Channels.FindAsync(ChannelId);
            if (channel == null)
            {
                context.Abort();
            }

            //Validate user is in channel
            var userTest = await _context.ChannelUsersStatuses.Where(x => x.ChannelId == ChannelId).Where(x => x.UserId == user.UserId).FirstOrDefaultAsync();
            if (userTest == null)
            {
                context.Abort();
            }
        }
    };
}

