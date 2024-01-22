using Moq;
using TalkWaveApi.Controllers;
using TalkWaveApi.Models;
using TalkWaveApi.Services;
using Microsoft.AspNetCore.Http;
using TalkWaveApi.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR;


namespace TalkWaveAPI.Tests
{
    public class ChannelControllerTests
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


            _context = new DefaultHttpContext();

            var validateMock = new Mock<IValidator>();

            validateMock.Setup(x => x.ValidateJwt(_context)).Returns(Task.FromResult<User?>(new User
            {
                UserId = 1,
                UserName = "test",
            }));

            _validator = validateMock.Object;
        }

        [Fact]
        public async void GetChannels()
        {
            //arrange
            var csus = GetChannelUserStatusesList();
            var channels = GetChannelsList();
            using var context = CreateContext();
            await context.ChannelUsersStatuses.AddRangeAsync(csus);
            await context.Channels.AddRangeAsync(channels);
            await context.SaveChangesAsync();
            var controller = new ChannelController(context, _validator);
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
            var csus = GetChannelUserStatusesList();
            var channels = GetChannelsList();
            using var context = CreateContext();
            await context.ChannelUsersStatuses.AddRangeAsync(csus);
            await context.Channels.AddRangeAsync(channels);
            await context.SaveChangesAsync();
            var controller = new ChannelController(context, _validator);
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
            var csus = GetChannelUserStatusesList();
            var channels = GetChannelsList();
            using var context = CreateContext();
            await context.ChannelUsersStatuses.AddRangeAsync(csus);
            await context.Channels.AddRangeAsync(channels);
            await context.SaveChangesAsync();
            var controller = new ChannelController(context, _validator);
            controller.ControllerContext.HttpContext = _context;

            var channel = await context.Channels.FindAsync(int.Parse(Id));
            Assert.NotNull(channel);

            var actionResult = await controller.DeleteChannel(Id);
            var okResult = actionResult as OkObjectResult;

            var deletedChannel = await context.Channels.FindAsync(int.Parse(Id));
            Assert.Null(deletedChannel);
        }
        [Theory]
        [InlineData("3")]
        [InlineData("4")]
        public async void DeleteChannelUnauthorized(string Id)
        {
            var csus = GetChannelUserStatusesList();
            var channels = GetChannelsList();
            using var context = CreateContext();
            await context.ChannelUsersStatuses.AddRangeAsync(csus);
            await context.Channels.AddRangeAsync(channels);
            await context.SaveChangesAsync();
            var controller = new ChannelController(context, _validator);
            controller.ControllerContext.HttpContext = _context;

            var channel = await context.Channels.FindAsync(int.Parse(Id));
            Assert.NotNull(channel);

            var actionResult = await controller.DeleteChannel(Id);
            var okResult = actionResult as OkObjectResult;

            var deletedChannel = await context.Channels.FindAsync(int.Parse(Id));
            Assert.NotNull(deletedChannel);
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
        DatabaseContext CreateContext() => new DatabaseContext(_contextOptions);
    };
}