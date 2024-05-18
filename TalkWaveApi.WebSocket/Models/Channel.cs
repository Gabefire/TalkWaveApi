using System.ComponentModel.DataAnnotations;

namespace TalkWaveApi.WebSocket.Models;
public class Channel
{
    [Key]
    public int ChannelId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string Type { get; set; } = "group";
    public string ChannelPicLink { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}