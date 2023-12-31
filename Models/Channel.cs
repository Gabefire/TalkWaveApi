using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace TalkWaveApi.Models;
public class Channel
{
    [Key]
    public int ChannelId { get; set; }
    public int UserId { get; set; }
    public string Type { get; set; } = "Group";
    public string ProfilePicLink { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}