using System.ComponentModel.DataAnnotations;

namespace TalkWaveApi.Models;
public class UserDto
{
    [MaxLength(100), Required]
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    public string ProfilePicLink { get; set; } = string.Empty;
}