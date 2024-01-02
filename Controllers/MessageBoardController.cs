using Microsoft.AspNetCore.Mvc;
using TalkWaveApi.Models;

namespace TalkWaveApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MessageBoardController(ILogger<MessageBoardController> logger) : ControllerBase
{

    private readonly ILogger<MessageBoardController> _logger = logger;

    // GET all message board user is joined
    [HttpGet]
    public void Get()
    {
        return;
    }
}
