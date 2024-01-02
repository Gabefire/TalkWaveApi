using Microsoft.EntityFrameworkCore;
using TalkWaveApi.Models;

namespace TalkWaveApi.Services;
public class DatabaseContext(DbContextOptions<DatabaseContext> options) : DbContext(options)
{
    public DbSet<MessageBoard> MessageBoards { get; set; }
}