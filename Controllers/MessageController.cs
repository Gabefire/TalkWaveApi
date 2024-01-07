using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TalkWaveApi.Models;
using TalkWaveApi.Services;

namespace TalkWaveApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MessageController(DatabaseContext context) : ControllerBase
{
    private enum ChannelEnum
    {
        User = 1,
        Group = 2,
    }

    private readonly DatabaseContext _context = context;

    // GET all messages for channel
    [HttpGet("{ChannelType}/{Id}")]
    public Task<List<Message>> Get(string ChannelType, string Id)
    {

    }
}
