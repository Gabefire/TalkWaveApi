namespace TalkWaveApi.Models;
public class ChannelDto
{
    public string Name { get; set; } = string.Empty;
    public int ChannelId { get; set; }
    public string Type { get; set; } = "group";
    public bool IsOwner { get; set; } = false;
    public string ChannelPicLink { get; set; } = string.Empty;
}