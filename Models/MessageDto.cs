namespace TalkWaveApi.Models;
public class MessageDto
{
    public bool Owner { get; set; } = false;
    public string Author { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

}