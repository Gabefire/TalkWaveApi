using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TalkWaveApi.Models;
using TalkWaveApi.Services;
using TalkWaveApi.Interfaces;

namespace TalkWaveApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserChannelController(ILogger<UserChannelController> logger, DatabaseContext context, IValidator validate) : ControllerBase
{
    private readonly DatabaseContext _context = context;

    private readonly IValidator _validate = validate;

    private readonly ILogger _logger = logger;

    // POST new user channel
    [HttpPost("{UserId}")]
    public async Task<ActionResult> CreateChannel(string UserId)
    {
        if (UserId.IsNullOrEmpty())
        {
            return BadRequest("UserId not present");
        }

        if (!int.TryParse(UserId, out int Id))
        {
            return BadRequest();
        }

        var user = await _validate.ValidateJwt(HttpContext);
        if (user == null)
        {
            return Unauthorized();
        }

        var requestedUser = await _context.Users.FindAsync(Id);
        if (requestedUser == null)
        {
            return BadRequest("Not a valid userId");
        }

        var channelId = await _context.ChannelUsersStatuses
            .Join(
                _context.Channels,
                csu => csu.ChannelId,
                c => c.ChannelId,
                (csu, c) => new
                {
                    channelId = c.ChannelId,
                    type = c.Type,
                    userId = csu.UserId,
                }
            )
            .Where(c => c.type == "user")
            .Where(csu => csu.userId == requestedUser.UserId || csu.userId == user.UserId)
            .Distinct()
            .GroupBy(x => x.channelId)
            .Select(x => new { ChannelId = x.Key, total = x.Count() })
            .Where(x => x.total > 1)
            .FirstOrDefaultAsync();


        if (channelId != null)
        {
            return Conflict(new { message = $"An existing channel between this user already exists", channelId = channelId.ChannelId });
        }

        Channel channel = new()
        {
            Type = "user",
            UserId = user.UserId,
        };

        await _context.Channels.AddAsync(channel);
        await _context.SaveChangesAsync();


        ChannelUserStatus userCSU = new()
        {
            ChannelId = channel.ChannelId,
            UserId = user.UserId
        };

        ChannelUserStatus requestedUserCSU = new()
        {
            ChannelId = channel.ChannelId,
            UserId = requestedUser.UserId
        };

        ChannelDto channelDto = new()
        {
            ChannelId = channel.ChannelId,
            Name = channel.Name,
            IsOwner = channel.UserId == user.UserId,
        };

        await _context.ChannelUsersStatuses.AddAsync(userCSU);
        await _context.ChannelUsersStatuses.AddAsync(requestedUserCSU);

        await _context.SaveChangesAsync();

        _logger.LogInformation("info: New user channel created: {UserGroup}", $"{user.UserName} and {requestedUser.UserName}");

        string actionName = nameof(ChannelController.GetChannel);

        return CreatedAtAction(actionName, "Channel", new { id = channel.ChannelId }, channelDto);

    }

}