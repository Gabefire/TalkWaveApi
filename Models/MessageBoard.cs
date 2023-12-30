namespace TalkWaveApi.Models;
public class MessageBoard
{
    public int Id { get; set; }
    public int OwnerId { get; set; } = 0;
    public string Type { get; set; } = string.Empty;
    public string ProfilePicLink { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}