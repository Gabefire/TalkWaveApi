using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TalkWaveApi.Models;
using TalkWaveApi.Services;
using TalkWaveApi.Interfaces;
using System.Net.Security;

namespace TalkWaveApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChannelController(DatabaseContext context, IValidator validate) : ControllerBase
{
    private readonly DatabaseContext _context = context;

    private readonly IValidator _validate = validate;



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

        var channelList = await _context.ChannelUsersStatuses
        .Where(x => x.UserId == user.UserId)
        .Join(
            _context.ChannelUsersStatuses,
            csu => csu.ChannelId,
            c => c.ChannelId,
            (csu, c) => new
            {
                csu.ChannelId,
                c.UserId
            }
        )
        .Join(
            _context.Users,
            csu => csu.UserId,
            user => user.UserId,
            (csu, user) => new
            {
                csu.ChannelId,
                csu.UserId,
                userName = user.UserName
            }
        )
        .Join(
            _context.Channels,
            csu => csu.ChannelId,
            c => c.ChannelId,
            (csu, c) => new
            {
                Name = c.Type == "group" ? c.Name : csu.userName,
                c.Type,
                c.ChannelId,
                IsOwner = c.UserId == user.UserId,
                c.ChannelPicLink,
                csu.UserId
            }
        )
        .Where(x => x.Type != "group" || x.UserId == user.UserId)
        .Where(x => x.Type != "user" || x.UserId != user.UserId)
        .Select(x => new ChannelDto()
        {
            Name = x.Name,
            Type = x.Type,
            ChannelId = x.ChannelId,
            IsOwner = x.IsOwner,
            ChannelPicLink = x.ChannelPicLink
        })
        .ToListAsync();

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

        var channelStatus = await _context.ChannelUsersStatuses.Where(x => x.UserId == user.UserId).Where(x => x.ChannelId == ChannelId).FirstOrDefaultAsync();
        if (channelStatus == null)
        {
            return Unauthorized("User not in channel");
        }

        var channel = await _context.ChannelUsersStatuses
            .Where(x => x.ChannelId == ChannelId)
            .Join(
                _context.Channels,
                csu => csu.ChannelId,
                c => c.ChannelId,
                (csu, c) => new
                {
                    csu.UserId,
                    c.Type,
                    c.ChannelId
                }
            )
            .Join(
                _context.Users,
                csu => csu.UserId,
                user => user.UserId,
                (csu, user) => new
                {
                    csu.Type,
                    csu.ChannelId,
                    csu.UserId,
                    userName = user.UserName
                }
            )
            .Where(x => x.Type != "group" || x.UserId == user.UserId)
            .Where(x => x.Type != "user" || x.UserId != user.UserId)
            .Join(
                _context.Channels,
                csu => csu.ChannelId,
                c => c.ChannelId,
                (csu, c) => new ChannelDto()
                {
                    Name = c.Type == "group" ? c.Name : csu.userName,
                    Type = c.Type,
                    ChannelId = c.ChannelId,
                    IsOwner = c.UserId == user.UserId,
                    ChannelPicLink = c.ChannelPicLink
                }
            )
            .SingleOrDefaultAsync();

        if (channel == null)
        {
            return BadRequest("Channel not found");
        }

        return Ok(channel);
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
        var csus = await _context.ChannelUsersStatuses.Where(x => x.ChannelId == channel.ChannelId).ToListAsync();
        var messages = await _context.Messages.Where(x => x.ChannelId == channel.ChannelId).ToListAsync();

        if (messages != null)
        {
            _context.Messages.RemoveRange(messages);
        }
        if (csus != null)
        {
            _context.ChannelUsersStatuses.RemoveRange(csus);
        }

        _context.Channels.Remove(channel);
        await _context.SaveChangesAsync();

        return Ok();
    }

    // Todo add method to edit channel
};
