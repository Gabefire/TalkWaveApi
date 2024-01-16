using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TalkWaveApi.Models;
using TalkWaveApi.Services;
using TalkWaveApi.Util;

namespace TalkWaveApi.Controllers;

[ApiController]
[Route("api/")]
public class ChannelController(ILogger<ChannelController> logger, DatabaseContext context, Validator validate) : ControllerBase
{
    private readonly DatabaseContext _context = context;

    private readonly Validator _validate = validate;

    private readonly ILogger<ChannelController> _logger = logger;


    // GET all message board user is joined
    [HttpGet("channels")]
    public async Task<ActionResult> GetChannels()
    {
        // Validate JWT and get user
        var user = await _validate.ValidateJwt(HttpContext);
        if (user == null)
        {
            return Unauthorized();
        }

        //Get list of channel names/ids
        var channelList = await _context.ChannelUsersStatuses.FromSql(
            $"SELECT Name, ChannelId, Type FROM ChannelUserStatus FULL JOIN User ON ChannelUserStatus.UserId = User.UserId FULL JOIN Channel ON ChannelUserStatus.ChannelId = Channel.ChannelId WHERE UserId = {user.UserId}"
        ).ToListAsync();

        return Ok(channelList);

    }
    // GET single channel
    [HttpGet("channel/{Id}")]
    public async Task<ActionResult> GetChannel(string Id)
    {
        if (!int.TryParse(Id, out int ChannelId))
        {
            return BadRequest();
        }
        var user = await _validate.ValidateJwt(HttpContext);
        if (user == null)
        {
            return Unauthorized("Invalid Jwt");
        }

        var channel = await _context.Channels.FindAsync(ChannelId);
        if (channel == null)
        {
            return BadRequest("Channel not found");
        }

        var channelStatus = await _context.ChannelUsersStatuses.Where(x => x.UserId == user.UserId).Where(x => channel.ChannelId == ChannelId).FirstOrDefaultAsync();
        if (channelStatus == null)
        {
            return Unauthorized("User not in channel");
        }


        ChannelDto channelDto = new()
        {
            Name = channel.Name,
            ChannelId = channel.ChannelId,
            Type = channel.Type,
            IsOwner = user.UserId == channel.UserId,
        };

        return Ok(channelDto);
    }

    // DELETE channel
    [HttpDelete("channel/{Id}")]
    public async Task<ActionResult> DeleteChannel(string Id)
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

        var channel = await _context.Channels.FindAsync(ChannelId);
        if (channel == null)
        {
            return BadRequest();
        }

        if (user.UserId != channel.UserId)
        {
            return Unauthorized();
        }

        _context.Channels.Remove(channel);
        await _context.SaveChangesAsync();

        return Ok();
    }

    // Todo add method to edit channel
};
