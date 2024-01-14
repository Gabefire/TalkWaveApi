namespace TalkWaveApi.Models;
public class ChannelDto
{
    public int ChannelId { get; set; } = -1;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "group";
    public bool IsOwner { get; set; } = false;
    public string ChannelPicLink { get; set; } = string.Empty;
}