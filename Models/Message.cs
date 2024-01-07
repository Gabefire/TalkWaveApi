using System.ComponentModel.DataAnnotations;

namespace TalkWaveApi.Models;
public class Message
{
    [Key]
    public int MessageId { get; set; }
    public int UserId { get; set; }
    [Required]
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

}