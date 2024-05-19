using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace TalkWaveApi.WebSocket.Services;
public class UserIdProvider : IUserIdProvider
{
    public virtual string GetUserId(HubConnectionContext connection)
    {
        return connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
    }
}