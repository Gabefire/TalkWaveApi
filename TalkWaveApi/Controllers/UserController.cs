using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using TalkWaveApi.Models;
using TalkWaveApi.Services;
using Microsoft.EntityFrameworkCore;
using TalkWaveApi.Interfaces;

namespace TalkWaveApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController(IConfiguration configuration, DatabaseContext context, ILogger<UserController> logger, IValidator validate) : ControllerBase
{
    private static readonly User user = new();
    private readonly DatabaseContext _context = context;
    private readonly IConfiguration _configuration = configuration;
    private readonly IValidator _validate = validate;
    private readonly ILogger _logger = logger;

    // POST Sign-up
    [HttpPost("register")]
    public async Task<IActionResult> Register(UserDto request)
    {
        if (request.UserName == null || request.Password == null || request.Email == null)
        {
            return BadRequest("Please include a username, email and password");
        }

        // Validate if email is taken
        var emailTest = await _context.Users.FirstOrDefaultAsync(x => x.Email == request.Email);
        if (emailTest != null)
        {
            return Conflict("Email already taken");
        }

        string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        //Todo Add profile pic link
        user.UserName = request.UserName;
        user.HashedPassword = passwordHash;
        user.Email = request.Email;



        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation("info: New user joined: {User}", user.UserName);

        return Ok();
    }

    //POST login receive token
    [HttpPost("login")]
    public async Task<IActionResult> Login(UserLoginDto request)
    {
        // logins in with email. User name is a placeholder

        var user = await _context.Users.Where(x => x.Email == request.Email).FirstOrDefaultAsync();
        if (user == null)
        {
            return BadRequest("Password or username is incorrect");
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.HashedPassword))
        {
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            return BadRequest("Password or username is incorrect");
        };
        string token = CreateToken(user);

        return Ok(new { token, userName = user.UserName, id = user.UserId });
    }

    // PUT edit user
    [HttpPut("edit")]
    public async Task<ActionResult> EditUser(UserDto userDto)
    {
        var user = await _validate.ValidateJwt(HttpContext);
        if (user == null)
        {
            return Unauthorized();
        }

        if (!userDto.UserName.IsNullOrEmpty())
        {
            user.UserName = userDto.UserName;
        }

        if (!userDto.Email.IsNullOrEmpty())
        {
            user.Email = userDto.Email;
        }

        if (!userDto.Password.IsNullOrEmpty())
        {
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(userDto.Password);
            user.HashedPassword = passwordHash;
        }

        _context.Update(user);

        await _context.SaveChangesAsync();

        return Ok();
    }

    // GET search for user might make this a query string
    [HttpGet("{name}")]
    public async Task<ActionResult> SearchUser(string name)
    {
        // Validate JWT and get user
        var user = await _validate.ValidateJwt(HttpContext);
        if (user == null)
        {
            return Unauthorized();
        }


        var userList = await _context.Users.Where(x => x.UserId != user.UserId).Where(x => x.UserName.ToLower().Contains(name.ToLower())).Select(x => new UserSearchDto { UserId = x.UserId, UserName = x.UserName, ProfilePicLink = x.ProfilePicLink }).Take(5).ToListAsync();
        return Ok(userList);
    }

    // Method to make JWT
    private string CreateToken(User user)
    {
        // Might add more claims here later
        List<Claim> claims =
        [
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Email, user.Email),
        ];

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            _configuration.GetSection("JwtSettings:Key").Value!
        ));

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature);

        var token = new JwtSecurityToken(
            claims: claims,
            signingCredentials: creds,
            // Added a large expire date for now this needs change later once mechanism is added to app to auto refresh token
            expires: DateTime.UtcNow.AddYears(1000)
        );

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);

        return jwt;
    }
}