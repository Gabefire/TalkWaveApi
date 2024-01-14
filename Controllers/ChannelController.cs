using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
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

    // PUT join group channel
    [HttpPut("channel/group/{Id}")]
    public async Task<ActionResult> JoinChannel(string Id)
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

        var inChannel = await _context.ChannelUsersStatuses.Where(x => x.UserId == user.UserId).Where(x => x.ChannelId == ChannelId).FirstOrDefaultAsync();
        if (inChannel != null)
        {
            return Ok();
        }

        ChannelUserStatus cus = new()
        {
            UserId = user.UserId,
            ChannelId = channel.ChannelId
        };

        await _context.ChannelUsersStatuses.AddAsync(cus);
        await _context.SaveChangesAsync();

        return Ok();
    }

    // POST new group channel
    [HttpPost("channel/group")]
    public async Task<ActionResult> CreateChannel(ChannelDto channelDto)
    {
        if (channelDto.GetType() != typeof(ChannelDto))
        {
            return Unauthorized();
        }
        // Validate JWT and get user
        var user = await _validate.ValidateJwt(HttpContext);
        if (user == null)
        {
            return BadRequest("Invalid JWT or User not found");
        }

        Channel channel = new()
        {
            Name = channelDto.Name,
            UserId = user.UserId,
            Type = channelDto.Type,
        };

        await _context.Channels.AddAsync(channel);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetChannel), new { id = channel.ChannelId }, channel);
    }

    // GET search groups for likeness
    [HttpGet("channels/group/{name}")]
    public async Task<ActionResult> SearchChannel(string name)
    {
        // Validate JWT and get user
        var user = await _validate.ValidateJwt(HttpContext);
        if (user == null)
        {
            return Unauthorized();
        }


        //Fuzzy search similar names limit 10
        var channelGroupList = await _context.ChannelUsersStatuses.FromSql(
        $"SELECT Name, ChannelId, Type FROM Channel WHERE Type = 'group' Name ILIKE {name}% LIMIT 10"
        ).ToListAsync();

        var channelUserList = await _context.ChannelUsersStatuses.FromSql(
        $"SELECT Name, UserId, Type FROM Channel WHERE Type = 'user' Name ILIKE {name}% LIMIT 10"
        ).ToListAsync();

        return Ok(channelGroupList.Concat(channelUserList));
    }
}
