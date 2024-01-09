//Model to store if user in groups
using System.ComponentModel.DataAnnotations;

namespace TalkWaveApi.Models;
public class ChannelUsersStatus
{
    [Key]
    public int ChannelUsersStatusId { get; set; }

    public int UserId { get; set; }

    public int ChannelId { get; set; }

}