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
    private static System.Timers.Timer? timer;

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
