using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TalkWaveApi.Models;
using TalkWaveApi.Services;
using TalkWaveApi.Interfaces;

namespace TalkWaveApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GroupChannelController(ILogger<GroupChannelController> logger, DatabaseContext context, IValidator validate) : ControllerBase
{
    private readonly DatabaseContext _context = context;

    private readonly IValidator _validate = validate;

    private readonly ILogger _logger = logger;

    // PUT join group channel
    [HttpPut("join/{Id}")]
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
        if (channel == null || channel.Type == "user")
        {
            return BadRequest();
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
    [HttpPost]
    public async Task<ActionResult> CreateChannel(ChannelDto channelDto)
    {
        if (channelDto.GetType() != typeof(ChannelDto))
        {
            return Unauthorized();
        }

        if (channelDto.Type != "group" && channelDto.Type != "user")
        {
            return BadRequest("Not a valid channel type");
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

        _logger.LogInformation("info: New group channel created: {UserGroup}", channel.Name);


        string actionName = nameof(ChannelController.GetChannel);

        return CreatedAtAction(actionName, "Channel", new { id = channel.ChannelId }, channel);
    }
    // GET search groups for likeness
    [HttpGet("{name}")]
    public async Task<ActionResult> SearchChannel(string name)
    {
        var user = await _validate.ValidateJwt(HttpContext);
        if (user == null)
        {
            return Unauthorized();
        }
        var channelGroupList = await _context.Channels.Where(x => x.Type == "group").Where(x => x.Name.ToLower().Contains(name.ToLower())).Select(x => new ChannelDto
        {
            Name = x.Name,
            ChannelId = x.ChannelId,
            IsOwner = x.UserId == user.UserId,
            Type = x.Type,
            ChannelPicLink = x.ChannelPicLink
        }).Take(5).ToListAsync();

        return Ok(channelGroupList);
    }

    //PUT leave channel
    [HttpPut("leave/{Id}")]
    public async Task<ActionResult> LeaveChannel(string Id)
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

        var cus = await _context.ChannelUsersStatuses.Where(x => x.UserId == user.UserId).Where(x => x.ChannelId == ChannelId).FirstOrDefaultAsync();
        if (cus == null)
        {
            return Ok();
        }

        _context.ChannelUsersStatuses.Remove(cus);
        await _context.SaveChangesAsync();

        return Ok();
    }
}