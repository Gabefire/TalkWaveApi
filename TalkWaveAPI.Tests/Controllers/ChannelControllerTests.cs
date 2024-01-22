using Moq;
using TalkWaveApi.Controllers;
using TalkWaveApi.Models;
using TalkWaveApi.Services;
using Microsoft.AspNetCore.Http;
using TalkWaveApi.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;


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


            var contextMock = new Mock<HttpContext>();
            _context = contextMock.Object;

            var validateMock = new Mock<IValidator>();

            validateMock.Setup(x => x.ValidateJwt(_context)).Returns(Task.FromResult<User?>(new User
            {
                UserId = 1,
                UserName = "test",
            }));

            _validator = validateMock.Object;
        }

        [Fact]
        public void GetChannels()
        {
            //arrange
            var csus = GetChannelUserStatusesList();
            var channels = GetChannelsList();
            using var context = CreateContext();
            context.ChannelUsersStatuses.AddRange(csus);
            context.Channels.AddRange(channels);
            context.SaveChanges();
            var controller = new ChannelController(context, _validator);
            //act
            var channelList = (controller.GetChannels().Result as OkObjectResult)?.Value as List<Channel>;
            //asset
            Assert.NotNull(channelList);
            Assert.Equal(2, channelList.Count());
            Assert.Equal(1, channelList.First().ChannelId);

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