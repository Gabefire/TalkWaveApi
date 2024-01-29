using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using TalkWaveApi.Models;
using TalkWaveApi.Services;
using TalkWaveApi.Interfaces;
using System.IdentityModel.Tokens.Jwt;

namespace TalkWaveApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MessageController(DatabaseContext context, ILogger<MessageController> logger, IValidator validator) : ControllerBase
{
    private readonly DatabaseContext _context = context;
    private readonly ILogger _logger = logger;
    private readonly IValidator _validate = validator;


    // websocket connections
    // Maybe add a dictionary with user for websocket to validate session already exists or not
    public static readonly ConcurrentDictionary<string, List<WebSocket>> connections = new();

    // Websocket messages
    [HttpGet("{ChannelType}/{Id}")]
    public async Task<IActionResult> GetWs([FromQuery(Name = "authorization")] string token, string ChannelType, string Id)
    {
        connections.TryGetValue(Id, out List<WebSocket>? connectionList);
        if (ChannelType == "user" && connectionList != null && connectionList.Count >= 2)
        {
            return BadRequest("User channel");
        }

        var handler = new JwtSecurityTokenHandler();

        if (!handler.CanReadToken(token))
        {
            return BadRequest();
        };
        var jwtToken = handler.ReadToken(token) as JwtSecurityToken;
        if (jwtToken == null)
        {
            return BadRequest();
        }

        string userId = jwtToken.Claims.First(claim => claim.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier").Value;

        if (!int.TryParse(userId, out int userParsedId))
        {
            return BadRequest();
        }

        //Validate and get user
        var user = await _context.Users.FindAsync(userParsedId);

        if (user == null)
        {
            return Unauthorized();
        }

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

        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            using var websocket = await HttpContext.WebSockets.AcceptWebSocketAsync();

            var userName = _context.Users.Where(u => u.UserId == user.UserId).Select(u => u.UserName).SingleOrDefault();

            string author = "Anonymous";

            if (userName != null)
            {
                author = userName;
            }

            await Echo(websocket, user, author, ChannelId, channel.Name);
        }
        else
        {
            return BadRequest("Not a websocket request");
        }

        return new EmptyResult();
    }


    private async Task Echo(WebSocket webSocket, User user, string userName, int ChannelId, string channelName)
    {
        connections.AddOrUpdate(ChannelId.ToString(), [webSocket], (key, list) => { list.Add(webSocket); return list; });

        var buffer = new byte[1024 * 4];
        var clientBuffer = new ArraySegment<byte>(buffer);
        var receiveResult = await webSocket.ReceiveAsync(clientBuffer, CancellationToken.None);

        _logger.LogInformation("info: {User} connected to {Channel}", userName, channelName);
        _logger.LogInformation("info: Connections in Channel: {Count}", connections[ChannelId.ToString()].Count);


        while (!receiveResult.CloseStatus.HasValue)
        {
            string result = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);

            if (string.IsNullOrEmpty(result))
            {
                continue;
            }



            DateTime currentDate = DateTime.UtcNow;

            Message message = new()
            {
                UserId = user.UserId,
                ChannelId = ChannelId,
                Content = result,
                CreatedAt = currentDate
            };

            await _context.Messages.AddAsync(message);
            await _context.SaveChangesAsync();

            MessageDto messageDto = new()
            {
                Author = userName,
                Content = result,
                CreatedAt = currentDate,
            };
            foreach (var item in connections[ChannelId.ToString()])
            {
                if (item == webSocket)
                {
                    messageDto.IsOwner = true;
                }

                var jsonData = JsonSerializer.SerializeToUtf8Bytes(messageDto);

                await item.SendAsync(
                    jsonData,
                    WebSocketMessageType.Binary,
                    true,
                    CancellationToken.None
                );

            }

            receiveResult = await webSocket.ReceiveAsync(clientBuffer, CancellationToken.None);
        }

        //Clean up
        await webSocket.CloseAsync(
            receiveResult.CloseStatus.Value,
            receiveResult.CloseStatusDescription,
            CancellationToken.None);

        connections.AddOrUpdate(ChannelId.ToString(), [webSocket], (key, list) => { list.Remove(webSocket); return list; });

        if (connections[ChannelId.ToString()].Count == 0)
        {
            connections.TryRemove(ChannelId.ToString(), out _);
        }

    }

    [HttpGet("{Id}")]
    public async Task<ActionResult> GetMessages(string Id)
    {
        if (!int.TryParse(Id, out int ChannelId))
        {
            return BadRequest();
        }
        var user = await _validate.ValidateJwt(HttpContext);
        if (user == null)
        {
            return Unauthorized();
        }

        var messages = await _context.Messages.Where(x => x.ChannelId == ChannelId).ToListAsync();

        List<MessageDto> messageDtos = [];


        foreach (Message message in messages)
        {
            var userName = await _context.Users.Where(u => u.UserId == message.UserId).Select(u => u.UserName).SingleOrDefaultAsync();

            string author = "Anonymous";

            if (userName != null)
            {
                author = userName;
            }

            MessageDto messageDto = new()
            {
                Author = author,
                IsOwner = message.UserId == user.UserId,
                Content = message.Content,
                CreatedAt = message.CreatedAt
            };

            messageDtos.Add(messageDto);
        }

        return Ok(messageDtos);
    }
}
