using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using TalkWaveApi.Models;
using TalkWaveApi.Services;
using TalkWaveApi.Interface;

namespace TalkWaveApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MessageController(DatabaseContext context, ILogger<MessageController> logger, IValidator validate) : ControllerBase
{
    private readonly DatabaseContext _context = context;
    private readonly ILogger _logger = logger;

    private readonly IValidator _validate = validate;

    // websocket connections
    // Maybe add a dictionary with user for websocket to validate session
    public static readonly ConcurrentDictionary<string, List<WebSocket>> connections = new();

    // Websocket messages
    [HttpGet("{ChannelType}/{Id}"), Authorize]
    public async Task<IActionResult> GetWs(string ChannelType, string Id)
    {
        // Validate group size
        connections.TryGetValue(Id, out List<WebSocket>? connectionList);
        if (ChannelType == "user" && connectionList != null && connectionList.Count >= 2)
        {
            return BadRequest();
        }

        var user = await _validate.ValidateJwt(HttpContext);
        if (user == null)
        {
            return Unauthorized();
        }

        //Validate Id can be casted as int
        if (!int.TryParse(Id, out int ChannelId))
        {
            return BadRequest();
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

        var jsonMessages = JsonSerializer.SerializeToUtf8Bytes(GetMessageDtos(ChannelId, user.UserId));
        await webSocket.SendAsync(
            jsonMessages,
            WebSocketMessageType.Binary,
            true,
            CancellationToken.None
            );

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


    private async Task<List<MessageDto>> GetMessageDtos(int ChannelId, int userId)
    {
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
                IsOwner = message.UserId == userId,
                Content = message.Content,
                CreatedAt = message.CreatedAt
            };
        }

        return messageDtos;
    }
}
