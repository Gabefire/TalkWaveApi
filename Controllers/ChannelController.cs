using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TalkWaveApi.Models;
using TalkWaveApi.Services;
using TalkWaveApi.Interface;
using System.Runtime.InteropServices;

namespace TalkWaveApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChannelController(ILogger<ChannelController> logger, DatabaseContext context, IValidator validate) : ControllerBase
{
    private readonly DatabaseContext _context = context;

    private readonly IValidator _validate = validate;

    private readonly ILogger _logger = logger;


    // GET all message board user is joined
    [HttpGet]
    public async Task<ActionResult> GetChannels()
    {
        // Validate JWT and get user
        var user = await _validate.ValidateJwt(HttpContext);
        if (user == null)
        {
            return Unauthorized();
        }
        // \"Channels\".\"Name\", \"Channels\".\"ChannelId\", \"Channels\".\"Type\", \"Users\".\"UserId\", cus.\"ChannelUsersStatusId\"
        //Get list of channel names/ids

        string sqlQuery = "SELECT c.\"Name\", c.\"ChannelId\", c2.\"UserId\", c2.\"ChannelUsersStatusId\"" +
                          "FROM \"Channels\" AS c " +
                          "LEFT JOIN \"ChannelUsersStatuses\" AS c2 ON c.\"ChannelId\" = c2.\"ChannelId\"";

        var channelList = await _context.ChannelUsersStatuses
            .Join(
                _context.Channels,
                csu => csu.ChannelId,
                c => c.ChannelId,
                (csu, c) => new
                {
                    id = c.ChannelId,
                    name = c.Name,
                    userId = csu.UserId
                }
            )
            .Where(csu => csu.userId == user.UserId)
            .ToListAsync();

        foreach (var channel in channelList)
        {
            Console.WriteLine(channel);
        }

        return Ok(channelList);

    }
    // GET single channel
    [HttpGet("{Id}")]
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
            Type = channel.Type,
            IsOwner = user.UserId == channel.UserId,
        };

        return Ok(channelDto);
    }

    // DELETE channel
    [HttpDelete("{Id}")]
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
