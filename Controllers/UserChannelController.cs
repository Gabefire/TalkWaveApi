using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TalkWaveApi.Models;
using TalkWaveApi.Services;
using TalkWaveApi.Interface;

namespace TalkWaveApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserChannelController(ILogger<UserChannelController> logger, DatabaseContext context, IValidator validate) : ControllerBase
{
    private readonly DatabaseContext _context = context;

    private readonly IValidator _validate = validate;

    private readonly ILogger _logger = logger;

    // POST new user channel
    [HttpPost]
    public async Task<ActionResult> CreateChannel(string UserId)
    {
        if (!int.TryParse(UserId, out int Id))
        {
            return BadRequest();
        }

        if (UserId.IsNullOrEmpty())
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
            return BadRequest();
        }

        try
        {
            string queryString = $"SELECT * FROM Channel LEFT JOIN ChannelUserStatus ON Channel.ChannelId = ChannelUserStatus.ChannelId WHERE (UserId={user.UserId} OR UserId={Id}) and Type=user";
            var existingChannelCheck = await _context.ChannelUsersStatuses.FromSql($"SELECT COUNT(ChannelId) FROM ({queryString}) as table1 GROUP BY ChannelId HAVING COUNT(ChannelId) > 1").ToListAsync();
            return Ok();
        }
        catch
        {
            Channel channel = new()
            {
                Type = "user",
                UserId = user.UserId,
            };

            await _context.Channels.AddAsync(channel);

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

            await _context.ChannelUsersStatuses.AddAsync(userCSU);
            await _context.ChannelUsersStatuses.AddAsync(requestedUserCSU);

            await _context.SaveChangesAsync();

            _logger.LogInformation("info: New user channel created: {UserGroup}", nameof(ChannelController.GetChannel));

            string actionName = nameof(ChannelController.GetChannel);

            return CreatedAtAction(actionName, "Channel", new { id = channel.ChannelId }, channel);
        }
    }

}