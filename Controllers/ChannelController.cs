using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TalkWaveApi.Models;
using TalkWaveApi.Services;
using TalkWaveApi.Util;

namespace TalkWaveApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChannelController(ILogger<ChannelController> logger, DatabaseContext context, Validator validate) : ControllerBase
{
    private readonly DatabaseContext _context = context;

    private readonly Validator _validate = validate;

    private readonly ILogger<ChannelController> _logger = logger;


    // GET all message board user is joined
    [HttpGet, Authorize]
    public async Task<ActionResult> GetChannel()
    {
        // Validate JWT and get user
        var user = _validate.ValidateJwt(HttpContext).Result;
        if (user == null)
        {
            return BadRequest("Invalid JWT or User not found");
        }

        //Get list of channel names/ids
        var channelList = await _context.ChannelUsersStatuses.FromSql(
            $"SELECT Name, ChannelId, Type FROM ChannelUserStatus FULL JOIN User ON ChannelUserStatus.UserId = User.UserId FULL JOIN Channel ON ChannelUserStatus.ChannelId = Channel.ChannelId WHERE UserId = {user.UserId}"
        ).ToListAsync();

        return Ok(channelList);

    }

    // POST new channel
    // Todo add owner based on JWT
    [HttpPost]
    public async Task<ActionResult> CreateChannel(Channel channel)
    {
        if (channel.GetType() != typeof(Channel))
        {
            return BadRequest("Invalid channel type");
        }
        // Validate JWT and get user
        var user = _validate.ValidateJwt(HttpContext).Result;
        if (user == null)
        {
            return BadRequest("Invalid JWT or User not found");
        }



        await _context.Channels.AddAsync(channel);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetChannel), new { id = channel.ChannelId }, channel);
    }

    // GET search groups for likeness
    [HttpGet("search/{name}")]
    public async Task<ActionResult> SearchChannel(string name)
    {
        // Validate JWT and get user
        var user = _validate.ValidateJwt(HttpContext).Result;
        if (user == null)
        {
            return BadRequest("Invalid JWT or User not found");
        }


        //Fuzzy search similar names limit 10
        var channelList = await _context.ChannelUsersStatuses.FromSql(
        $"SELECT Name, ChannelId, Type FROM ChannelUserStatus FULL JOIN User ON ChannelUserStatus.UserId = User.UserId FULL JOIN Channel ON ChannelUserStatus.ChannelId = Channel.ChannelId WHERE UserId = {user.UserId} AND Name ILIKE {name}% LIMIT 10"
        ).ToListAsync();

        return Ok(channelList);
    }
}
