using System.ComponentModel.DataAnnotations;

namespace TalkWaveApi.Models;
public class User
{
    [Key]
    public int UserId { get; set; }
    [MaxLength(100), Required]
    public string UserName { get; set; } = string.Empty;
    public string HashedPassword { get; set; } = string.Empty;
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    public string ProfilePicLink { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}