using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Net.WebSockets;
using System.Reflection.Metadata.Ecma335;
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
    [HttpGet("{ChannelType}/{Id}"), Authorize]
    public async Task<ActionResult<List<Message>>> Get(string ChannelType, string Id)
    {
        try
        {
            //JWT for user ID

            string token = HttpContext.Request.Headers.Authorization.ToString();
            var handler = new JwtSecurityTokenHandler();

            //Check if JWT can be read
            if (!handler.CanReadToken(token.Split(" ")[1]))
            {
                return BadRequest("Invalid Jwt");
            };

            var jwtToken = handler.ReadToken(token.Split(" ")[1]) as JwtSecurityToken;

            string userEmail = jwtToken!.Claims.First(claim => claim.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress").Value;



            // Find user see if in channel
            var user = await _context.Users.Where(x => x.Email == userEmail).FirstOrDefaultAsync() ?? throw new Exception("Not a valid email");

            var channelStatus = await _context.ChannelUsersStatuses.FromSql($"SELECT * FROM ChannelUserStatus WHERE UserId = {user.UserId} and ChannelId = {Id}").FirstOrDefaultAsync() ?? throw new Exception("User not in channel");

            var messages = await _context.Messages.Where(x => x.ChannelId.ToString() == Id).ToListAsync();

            List<MessageDto> messageDtos = [];

            foreach (Message message in messages)
            {
                MessageDto messageDto = new()
                {
                    Author = message.Author,
                    Owner = message.UserId == user.UserId,
                    Content = message.Content,
                    CreatedAt = message.CreatedAt
                };
            }

            return Ok(messageDtos);

        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }



    // Websockets
    [Route("/ws/{ChannelType}/{Id}"), Authorize]
    public async Task GetWs(string ChannelType, string Id)
    {
        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            // Not using compression for right now
            using var websocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            await Echo(websocket);
        }
        else
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
    }


    private static async Task Echo(WebSocket webSocket)
    {
        var buffer = new byte[1024 * 4];
        var receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        while (!receiveResult.CloseStatus.HasValue)
        {
            await webSocket.SendAsync()
        }
    }
}
