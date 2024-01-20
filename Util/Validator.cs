using System.IdentityModel.Tokens.Jwt;
using TalkWaveApi.Models;
using TalkWaveApi.Services;
using TalkWaveApi.Interface;
using System.Security.Claims;

namespace TalkWaveApi.Util
{
    public class Validator(DatabaseContext context) : IValidator
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
            Console.WriteLine(jwtToken);
            if (jwtToken == null)
            {
                return null;
            }


            string Id = jwtToken.Claims.First(claim => claim.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier").Value;

            if (!int.TryParse(Id, out int userId))
            {
                return null;
            }

            //Validate and get user
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return null;
            }

            return user;
        }
    }
}
