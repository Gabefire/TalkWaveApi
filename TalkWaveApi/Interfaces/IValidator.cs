using TalkWaveApi.Models;

namespace TalkWaveApi.Interfaces
{
    public interface IValidator
    {
        Task<User?> ValidateJwt(HttpContext context);
    }
}