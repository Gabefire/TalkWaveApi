namespace TalkWaveApi.Models;
public class Message
{
    public int Id { get; set; }
    public int OwnerId { get; set; } = 0;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

}