using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using TalkWaveApi.WebSocket.Models;
using TalkWaveApi.WebSocket.Services;


namespace TalkWaveApi.WebSocket.Hubs
{

    [Authorize]
    public class ChatHub(DatabaseContext dbContext) : Hub
    {
        private readonly DatabaseContext _context = dbContext;
        public async Task JoinGroup(string groupId)
        {
            string? token = Context.GetHttpContext()?.Request.Query["access_token"];
            var user = ValidateJwt(token);
            await Groups.AddToGroupAsync(Context.ConnectionId, groupId);
        }

        public async Task LeaveGroup(string groupId)
        {
            string? token = Context.GetHttpContext()?.Request.Query["access_token"];
            var user = ValidateJwt(token);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupId);
        }

        public async Task SendMessage(string groupId, string message)
        {
            string? token = Context.GetHttpContext()?.Request.Query["access_token"];
            var user = await ValidateJwt(token);
            await Clients.Group(groupId).SendAsync("ReceiveMessage", user?.UserName, message);
        }
        public async Task<User?> ValidateJwt(string? token)
        {
            var handler = new JwtSecurityTokenHandler();


            if (!handler.CanReadToken(token))
            {
                throw new Exception("Bad request");
            };

            var jwtToken = handler.ReadToken(token) as JwtSecurityToken;
            if (jwtToken == null)
            {
                throw new Exception("Bad request");
            }

            string userId = jwtToken.Claims.First(claim => claim.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier").Value;

            if (!int.TryParse(userId, out int userParsedId))
            {
                throw new Exception("Bad request");
            }

            //Validate and get user
            var user = await _context.Users.FindAsync(userParsedId) ?? throw new Exception("Bad request");
            return user;
        }
        public async Task ValidateChannel(string Id)
        {
            //Validate Id can be casted as int
            if (!int.TryParse(Id, out int ChannelId))
            {
                return BadRequest("Not a valid id");
            }

            //Validate channel exists
            var channel = await _context.Channels.FindAsync(ChannelId);
            if (channel == null)
            {
                return BadRequest("Channel does not exist");
            }

            //Validate user is in channel
            var userTest = await _context.ChannelUsersStatuses.Where(x => x.ChannelId == ChannelId).Where(x => x.UserId == user.UserId).FirstOrDefaultAsync();
            if (userTest == null)
            {
                return BadRequest("User not in channel");
            }
        }
    };
}

