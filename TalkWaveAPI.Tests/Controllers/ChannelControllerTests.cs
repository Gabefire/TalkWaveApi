using Moq;
using TalkWaveApi.Controllers;
using TalkWaveApi.Models;
using TalkWaveApi.Services;
using Microsoft.AspNetCore.Http;
using TalkWaveApi.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;

namespace TalkWaveApi.Tests
{
    [Collection("TalkWaveApiTestCollection")]
    public class ChannelControllerTests : IDisposable
    {
        private readonly DbContextOptions<DatabaseContext> _contextOptions;

        private readonly IValidator _validator;

        private readonly HttpContext _context;

        private readonly SqliteConnection _connection;


        public ChannelControllerTests()
        {
            // Create and open a connection. This creates the SQLite in-memory database, which will persist until the connection is closed
            // at the end of the test (see Dispose below).
            _connection = new SqliteConnection("Filename=:memory:");
            _connection.Open();

            // These options will be used by the context instances in this test suite, including the connection opened above.
            _contextOptions = new DbContextOptionsBuilder<DatabaseContext>()
            .UseSqlite(_connection)
            .Options;

            using var dbContext = new DatabaseContext(_contextOptions);

            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            SetUp(dbContext);

            _context = new DefaultHttpContext();

            var validateMock = new Mock<IValidator>();

            validateMock.Setup(x => x.ValidateJwt(_context)).Returns(Task.FromResult<User?>(new User
            {
                UserId = 2,
                UserName = "test",
            }));

            _validator = validateMock.Object;
        }

        public void Dispose()
        {
            var dbContext = new DatabaseContext(_contextOptions);
            dbContext.ChangeTracker
            .Entries()
            .ToList()
            .ForEach(e => e.State = EntityState.Detached);
        }

        [Fact]
        public async void GetChannels()
        {
            //arrange
            using DatabaseContext dbContext = CreateContext();
            var controller = new ChannelController(dbContext, _validator);
            controller.ControllerContext.HttpContext = _context;
            //act
            var actionResult = await controller.GetChannels();
            var okResult = actionResult as OkObjectResult;
            var channelList = okResult?.Value as List<ChannelDto>;
            //asset
            Assert.IsType<List<ChannelDto>>(channelList);
            Assert.Equal(3, channelList.Count());
            Assert.Contains(channelList, channel => channel.Name == "test1");
            Assert.Contains(channelList, channel => channel.Name == "test0");
        }

        public static IEnumerable<object[]> Channels()
        {
            {
                yield return new object[] { 2, new ChannelDto { Name = "test2", Type = "group", IsOwner = true } };
                yield return new object[] { 1, new ChannelDto { Name = "test1", Type = "group", IsOwner = true } };
            }
        }

        [Theory]
        [MemberData(nameof(Channels))]
        public async void GetChannel(string Id, ChannelDto expectedChannel)
        {
            //arrange
            using DatabaseContext dbContext = CreateContext();
            var controller = new ChannelController(dbContext, _validator);
            controller.ControllerContext.HttpContext = _context;
            //act
            var actionResult = await controller.GetChannel(Id);
            var okResult = actionResult as OkObjectResult;
            var channel = okResult?.Value as ChannelDto;
            //asset
            Assert.IsType<ChannelDto>(channel);
            Assert.Equal(channel.Name, expectedChannel.Name);
        }
        [Theory]
        [InlineData("1")]
        [InlineData("2")]
        public async void DeleteChannel(string Id)
        {
            using DatabaseContext dbContext = CreateContext();
            var controller = new ChannelController(dbContext, _validator);
            controller.ControllerContext.HttpContext = _context;

            var channel = await dbContext.Channels.FindAsync(int.Parse(Id));
            Assert.NotNull(channel);

            var actionResult = await controller.DeleteChannel(Id);
            var okResult = actionResult as OkObjectResult;

            var deletedChannel = await dbContext.Channels.FindAsync(int.Parse(Id));
            Assert.Null(deletedChannel);
        }
        private List<User> GetUserList()
        {
            List<User> users = new List<User>
            {
                new User
                {
                    UserId = 1,
                    UserName = "test0",
                    Email = "test0@test.com",
                    ProfilePicLink = "/"
                },
                new User
                {
                    UserId = 2,
                    UserName = "test1",
                    Email = "test1@test.com",
                    ProfilePicLink = "/"
                }

            };
            return users;
        }
        private List<Channel> GetChannelsList()
        {
            List<Channel> channels = new List<Channel>
            {
                new Channel
                {
                    ChannelId = 1,
                    Type = "group",
                    UserId = 2,
                    Name = "test1"
                },
                new Channel
                {
                    ChannelId = 2,
                    Type = "group",
                    UserId = 2,
                    Name = "test2"
                },
                new Channel
                {
                    ChannelId = 3,
                    Type = "user",
                    UserId = 2,
                    Name = "test3"
                },
                new Channel
                {
                    ChannelId = 4,
                    Type = "user",
                    UserId = 1,
                    Name = "test4"
                },
            };
            return channels;
        }
        private List<ChannelUserStatus> GetChannelUserStatusesList()
        {
            List<ChannelUserStatus> csu = new List<ChannelUserStatus>
            {
                new ChannelUserStatus
                {
                 UserId = 2,
                 ChannelId = 1,
                },
                new ChannelUserStatus
                {
                    UserId = 2,
                    ChannelId = 3,
                },
                new ChannelUserStatus
                {
                    UserId = 1,
                    ChannelId = 3
                },
                new ChannelUserStatus
                {
                    UserId = 2,
                    ChannelId = 2
                }
            };
            return csu;
        }
        DatabaseContext CreateContext() => new DatabaseContext(_contextOptions);

        private void SetUp(DatabaseContext context)
        {
            var csus = GetChannelUserStatusesList();
            var channels = GetChannelsList();
            var users = GetUserList();

            context.ChannelUsersStatuses.AddRange(csus);
            context.Channels.AddRange(channels);
            context.Users.AddRange(users);
            context.SaveChanges();
        }
    };
}