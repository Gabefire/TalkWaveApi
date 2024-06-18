//Model to store if user in groups
using System.ComponentModel.DataAnnotations;

namespace TalkWaveApi.WebSocket.Models;
public class ChannelUserStatus
{
    [Key]
    public int ChannelUsersStatusId { get; set; }

    public int UserId { get; set; }

    public int ChannelId { get; set; }

}