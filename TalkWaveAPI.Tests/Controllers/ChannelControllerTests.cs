using Moq;
using TalkWaveApi.Controllers;
using TalkWaveApi.Models;
using TalkWaveApi.Services;
using Microsoft.AspNetCore.Http;
using TalkWaveApi.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace TalkWaveApi.Tests
{
    [Collection("TalkWaveApiTestCollection")]
    public class ChannelControllerTests : IDisposable
    {
        private readonly DbContextOptions<DatabaseContext> _contextOptions;

        private readonly IValidator _validator;

        private readonly HttpContext _context;

        public ChannelControllerTests()
        {
            _contextOptions = new DbContextOptionsBuilder<DatabaseContext>().UseInMemoryDatabase(databaseName: "TalkWave")
            .Options;

            using var dbContext = new DatabaseContext(_contextOptions);

            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            SetUp(dbContext);

            _context = new DefaultHttpContext();

            var validateMock = new Mock<IValidator>();

            validateMock.Setup(x => x.ValidateJwt(_context)).Returns(Task.FromResult<User?>(new User
            {
                UserId = 1,
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
            Assert.Equal(2, channelList.Count());
            Assert.Contains(channelList, channel => channel.Name == "test1");
            Assert.Contains(channelList, channel => channel.Name == "test3");
        }

        public static IEnumerable<object[]> Channels()
        {
            {
                yield return new object[] { "2", new ChannelDto { Name = "test2", Type = "group", IsOwner = true } };
                yield return new object[] { "1", new ChannelDto { Name = "test1", Type = "group", IsOwner = true } };
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
        private List<Channel> GetChannelsList()
        {
            List<Channel> channels = new List<Channel>
            {
                new Channel
                {
                    ChannelId = 1,
                    Type = "group",
                    UserId = 1,
                    Name = "test1"
                },
                new Channel
                {
                    ChannelId = 2,
                    Type = "group",
                    UserId = 1,
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
                    UserId = 2,
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
                 UserId = 1,
                 ChannelId = 1,
                },
                new ChannelUserStatus
                {
                    UserId = 2,
                    ChannelId = 2,
                },
                new ChannelUserStatus
                {
                    UserId = 1,
                    ChannelId = 3
                }
            };
            return csu;
        }
        DatabaseContext CreateContext() => new(_contextOptions);

        private void SetUp(DatabaseContext context)
        {
            var csus = GetChannelUserStatusesList();
            var channels = GetChannelsList();

            context.ChannelUsersStatuses.AddRange(csus);
            context.Channels.AddRange(channels);
            context.SaveChanges();
        }
    };
}