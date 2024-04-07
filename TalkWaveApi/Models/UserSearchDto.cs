namespace TalkWaveApi.Models;
public class UserSearchDto
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string ProfilePicLink { get; set; } = string.Empty;
}