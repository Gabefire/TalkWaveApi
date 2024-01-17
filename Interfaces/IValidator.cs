using TalkWaveApi.Models;

namespace TalkWaveApi.Interface
{
    public interface IValidator
    {
        Task<User?> ValidateJwt(HttpContext context);
    }
}