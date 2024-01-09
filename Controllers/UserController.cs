using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using TalkWaveApi.Models;
using TalkWaveApi.Services;
using Microsoft.EntityFrameworkCore;

namespace TalkWaveApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController(IConfiguration configuration, DatabaseContext context, ILogger logger) : ControllerBase
{
    private static readonly User user = new();
    private readonly DatabaseContext _context = context;
    private readonly IConfiguration _configuration = configuration;

    private readonly ILogger _logger = logger;

    // POST Sign-up
    [HttpPost("register")]
    public async Task<IActionResult> Register(UserDto request)
    {
        if (request.UserName == null || request.Password == null || request.Email == null)
        {
            return BadRequest("Please include a username, email and password");
        }

        try
        {
            // Check if email is taken
            var emailTest = await _context.Users.FirstOrDefaultAsync(x => x.Email == request.Email) ?? throw new Exception("Email already taken");

        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
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

    //POST login receive token
    [HttpPost("login")]
    public async Task<IActionResult> Login(UserDto request)
    {
        // logins in with email. User name is a placeholder
        try
        {
            var user = await _context.Users.Where(x => x.Email == request.Email).FirstOrDefaultAsync() ?? throw new Exception("Username is incorrect");

            if (BCrypt.Net.BCrypt.Verify(request.Password, user.HashedPassword))
            {
                throw new Exception("Password is incorrect.");
            }
        }
        catch (Exception e)
        {
            _logger.LogInformation("{Message}", e.Message);
            return BadRequest("Username or Password is incorrect");
        }


        string token = CreateToken(user);

        return Ok(token);
    }

    // Method to make JWT
    private string CreateToken(User user)
    {

        // Might add more claims here later
        List<Claim> claims =
        [
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.UserName.ToString()),
            new(ClaimTypes.Email, user.Email.ToString())
        ];

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            _configuration.GetSection("JwtSettings:Key").Value!
        ));

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature);

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.Now.AddHours(2),
            signingCredentials: creds
        );

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);

        return jwt;
    }
}