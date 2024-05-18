using Microsoft.EntityFrameworkCore;
using TalkWaveApi.WebSocket.Models;

namespace TalkWaveApi.WebSocket.Services;
public class DatabaseContext(DbContextOptions<DatabaseContext> options) : DbContext(options)
{
    public DbSet<Channel> Channels { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<ChannelUserStatus> ChannelUsersStatuses { get; set; }
}