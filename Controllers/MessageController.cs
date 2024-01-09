using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using TalkWaveApi.Models;
using TalkWaveApi.Services;

namespace TalkWaveApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MessageController(DatabaseContext context) : ControllerBase
{
    private enum ChannelEnum
    {
        User = 1,
        Group = 2,
    }

    private readonly DatabaseContext _context = context;

    // GET all messages for channel
    // Todo need auth
    // Todo websockets
    [HttpGet("{ChannelType}/{Id}"), Authorize]
    public async Task<List<Message>> Get(string ChannelType, string Id)
    {
        try
        {
            //JWT for user ID

            string token = HttpContext.Request.Headers.Authorization.ToString();
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadToken(token.Split(" ")[1]) as JwtSecurityToken ?? throw new Exception("No token");

            string userEmail = jwtToken.Claims.First(claim => claim.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress").Value ?? throw new Exception("Not a valid ID");

            // Find userID see if in channel
            var userId = _context.Users.Where(x => x.Email == userEmail).FirstAsync();

            var channelStatus = await _context.ChannelUsersStatuses.Where(x => x.UserId.ToString() == userId.ToString()).FirstOrDefaultAsync() ?? throw new Exception("User not in channel");

            var messages = await _context.Messages.FindAsync(Id)

        }
        catch
        {
            return BadRequest("Invalid token");
        }
    }

}
