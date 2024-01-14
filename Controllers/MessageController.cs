using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Net.WebSockets;
using System.Text;
using TalkWaveApi.Models;
using TalkWaveApi.Services;

namespace TalkWaveApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MessageController(DatabaseContext context, ILogger<ChannelController> logger) : ControllerBase
{
    private enum ChannelEnum
    {
        User = 1,
        Group = 2,
    }

    private readonly DatabaseContext _context = context;
    private readonly ILogger _logger = logger;

    private static readonly Message message = new();

    // websocket connections
    public static readonly ConcurrentDictionary<string, WebSocket> connections = new();

    // GET all messages for channel
    [HttpGet("{ChannelType}/{Id}"), Authorize]
    public async Task<ActionResult<List<Message>>> Get(string ChannelType, string Id)
    {
        try
        {
            //Validate Channel Id
            var channel = await _context.Channels.FindAsync(Id) ?? throw new Exception("Not a valid channel");

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
                var userName = await _context.Users.Where(u => u.UserId == message.UserId).Select(u => u.UserName).SingleOrDefaultAsync();


                MessageDto messageDto = new()
                {
                    Author = userName!.ToString()!,
                    IsOwner = message.UserId == user.UserId,
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



    // Websocket route
    [HttpGet("ws/{ChannelType}/{Id}")]
    public async Task GetWs(string ChannelType, string Id)
    {
        //Validate Id can be casted as int
        if (!int.TryParse(Id, out int ChannelId))
        {
            HttpContext.Response.StatusCode = StatusCodes.Status404NotFound;
        }

        //Todo JWT
        User user = new()
        {
            UserId = 123,
            UserName = "test",
            HashedPassword = "123",
            Email = "test",
        };

        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            using var websocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            await Echo(websocket, user, ChannelId);
        }
        else
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
    }


    private async Task Echo(WebSocket webSocket, User user, int ChannelId)
    {

        string wsID = Guid.NewGuid().ToString();
        connections.TryAdd(wsID, webSocket);

        var buffer = new byte[1024 * 4];
        var clientBuffer = new ArraySegment<byte>(buffer);
        var receiveResult = await webSocket.ReceiveAsync(clientBuffer, CancellationToken.None);

        _logger.LogInformation("{message}", "connection made");

        //Get username              
        var userName = await _context.Users.Where(u => u.UserId == user.UserId).Select(u => u.UserName).SingleOrDefaultAsync();

        while (!receiveResult.CloseStatus.HasValue)
        {
            //Get message
            string result = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);

            _logger.LogInformation("{Message}", result);

            Message message = new()
            {
                UserId = user.UserId,
                ChannelId = ChannelId,
                Content = result,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Messages.AddAsync(message);

            MessageDto messageDto = new()
            {
                Author = userName!
                Content = user
            };

            receiveResult = await webSocket.ReceiveAsync(clientBuffer, CancellationToken.None);



            foreach (var item in connections)
            {


                if (item.Value == webSocket)
                {
                    messageDto.IsOwner = true;
                }


                await item.Value.SendAsync(
                    clientBuffer,
                    receiveResult.MessageType,
                    receiveResult.EndOfMessage,
                    CancellationToken.None
                );

            }



        }

        await webSocket.CloseAsync(
            receiveResult.CloseStatus.Value,
            receiveResult.CloseStatusDescription,
            CancellationToken.None);

    }

}
