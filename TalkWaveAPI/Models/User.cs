using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace TalkWaveApi.Models;

[Index(nameof(Email), IsUnique = true)]
public class User
{
    [Key]
    public int UserId { get; set; }
    [MaxLength(100), Required]
    public string UserName { get; set; } = string.Empty;
    public string HashedPassword { get; set; } = string.Empty;
    [EmailAddress, Required]
    public string Email { get; set; } = string.Empty;
    public string ProfilePicLink { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}