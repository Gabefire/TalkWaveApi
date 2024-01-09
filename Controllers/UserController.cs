using Microsoft.AspNetCore.Mvc;
using TalkWaveApi.Models;
using TalkWaveApi.Services;

namespace TalkWaveApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController(DatabaseContext context) : ControllerBase
{
    private static User user = new();
    private readonly DatabaseContext _context = context;

    [HttpPost("register")]
    public async Task<IActionResult> Register(UserDto request)
    {
        if (request.UserName == null || request.Password == null)
        {
            return BadRequest("Please include a username and password");
        }

        string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        //Todo Add profile pic link
        user.UserName = request.UserName;
        user.HashedPassword = passwordHash;
        user.Email = request.Email;


        try
        {
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
            return Ok();
        }
        catch
        {
            return BadRequest("Something went wrong");
        }

    }
}