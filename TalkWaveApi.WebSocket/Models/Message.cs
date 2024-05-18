using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace TalkWaveApi.WebSocket.Models;
[Index(nameof(ChannelId))]
public class Message
{
    [Key]
    public int MessageId { get; set; }
    public int UserId { get; set; }
    public int ChannelId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

}