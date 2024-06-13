using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using TalkWaveApi.WebSocket.Models;
using TalkWaveApi.WebSocket.Services;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;

namespace TalkWaveApi.WebSocket.Controllers;
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class MessageController(DatabaseContext context, ILogger<MessageController> logger) : ControllerBase
{
    private readonly DatabaseContext _context = context;
    private readonly ILogger _logger = logger;
    private static System.Timers.Timer? timer;


    // websocket connections route is not used anymore switch to signalR microservices left it here to show.
    public static readonly ConcurrentDictionary<string, List<System.Net.WebSockets.WebSocket>> connections = new();

    // Websocket messages
    [HttpGet("{ChannelType}/{Id}")]
    public async Task<IActionResult> GetWs([FromQuery(Name = "access_token")] string token, string Id)
    {
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

            await Echo(websocket, user, author, channel);
        }
        else
        {
            return BadRequest("Not a websocket request");
        }

        return new EmptyResult();
    }


    private async Task Echo(System.Net.WebSockets.WebSocket webSocket, User user, string userName, Channel channel)
    {
        connections.AddOrUpdate(channel.ChannelId.ToString(), [webSocket], (key, list) => { list.Add(webSocket); return list; });

        var buffer = new byte[1024 * 4];
        var clientBuffer = new ArraySegment<byte>(buffer);

        _logger.LogInformation("{User} connected to ChannelId: {ChannelId}", userName, channel.ChannelId);
        _logger.LogInformation("Connections in Channel: {Count}", connections[channel.ChannelId.ToString()].Count);


        SetTimer(webSocket);

        while (true)
        {
            try
            {
                var receiveResult = await webSocket.ReceiveAsync(clientBuffer, CancellationToken.None);
                if (receiveResult.CloseStatus.HasValue || webSocket.State != WebSocketState.Open)
                {
                    if (webSocket.State != WebSocketState.Closed)
                    {
                        await webSocket.CloseAsync(receiveResult.CloseStatus!.Value, receiveResult.CloseStatusDescription, CancellationToken.None);
                    }
                    break;
                }
                string result = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);
                timer?.Stop();
                timer?.Start();

                if (string.IsNullOrEmpty(result))
                {
                    _logger.LogDebug("Message Received: {result}", "ping");
                    continue;
                }

                _logger.LogDebug("Message Received: {result}", result);

                DateTime currentDate = DateTime.UtcNow;

                Message message = new()
                {
                    UserId = user.UserId,
                    ChannelId = channel.ChannelId,
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

                foreach (var item in connections[channel.ChannelId.ToString()])
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
            }
            catch
            {
                break;
            }

        }



        connections.AddOrUpdate(channel.ChannelId.ToString(), [webSocket], (key, list) => { list.Remove(webSocket); return list; });

        if (connections[channel.ChannelId.ToString()].Count == 0)
        {
            connections.TryRemove(channel.ChannelId.ToString(), out _);
        }

        _logger.LogInformation("{User} left ChannelId: {ChannelId}", userName, channel.ChannelId);

    }


    private static void SetTimer(System.Net.WebSockets.WebSocket webSocket)
    {
        timer = new System.Timers.Timer(120000);
        timer.Elapsed += (sender, e) =>
        {
            if (webSocket.State == WebSocketState.Open)
            {
                webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
            }
        };
        timer.AutoReset = false;
        timer.Enabled = true;
    }
}
