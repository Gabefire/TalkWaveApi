using System.IdentityModel.Tokens.Jwt;
using Microsoft.EntityFrameworkCore;
using TalkWaveApi.Models;
using TalkWaveApi.Services;

namespace TalkWaveApi.Util;
public class Validator(DatabaseContext context)
{
    private readonly DatabaseContext _context = context;
    public async Task<User?> ValidateJwt(HttpContext context)
    {

        //JWT for user ID
        string token = context.Request.Headers.Authorization.ToString();
        var handler = new JwtSecurityTokenHandler();
        //Check if JWT can be read
        if (!handler.CanReadToken(token.Split(" ")[1]))
        {
            return null;
        };
        var jwtToken = handler.ReadToken(token.Split(" ")[1]) as JwtSecurityToken;
        string userEmail = jwtToken!.Claims.First(claim => claim.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress").Value;

        //Validate and get user
        var user = await _context.Users.Where(x => x.Email == userEmail).FirstOrDefaultAsync();
        if (user == null)
        {
            return null;
        }

        return user;
    }
}