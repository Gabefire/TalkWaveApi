using Microsoft.AspNetCore.Mvc;
using TalkWaveApi.Models;
using TalkWaveApi.Services;

namespace TalkWaveApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChannelController(ILogger<ChannelController> logger, DatabaseContext context) : ControllerBase
{
    private readonly DatabaseContext _context = context;


    private readonly ILogger<ChannelController> _logger = logger;


    // GET all message board user is joined need JWT
    [HttpGet]
    public void GetChannel()
    {
        return;
    }

    // POST new channel
    // Todo add owner based on JWT
    [HttpPost]
    public ActionResult<Channel> CreateChannel(Channel channel)
    {
        if (channel == null)
        {
            return BadRequest();
        }

        _context.Channels.Add(channel);
        _context.SaveChanges();
        return CreatedAtAction(nameof(GetChannel), new { id = channel.ChannelId }, channel);
    }
}
