using Moq;
using TalkWaveApi.Controllers;
using TalkWaveApi.Models;
using TalkWaveApi.Services;
using Microsoft.AspNetCore.Http;
using TalkWaveApi.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace TalkWaveAPI.Tests
{
    public class GroupChannelControllerTests
    {
        private readonly DbContextOptions<DatabaseContext> _contextOptions;

        private readonly IValidator _validator;

        private readonly HttpContext _context;

        public GroupChannelControllerTests()
        {
            _contextOptions = new DbContextOptionsBuilder<DatabaseContext>().UseInMemoryDatabase(databaseName: "TalkWave")
            .Options;

            var dbContext = new DatabaseContext(_contextOptions);

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


        [Theory]
        [InlineData("1")]
        [InlineData("4")]
        private async void JoinChannel(string Id)
        {
            //arrange
            using var loggerFactory = LoggerFactory.Create(loggingBuilder => loggingBuilder
            .SetMinimumLevel(LogLevel.Trace)
            .AddConsole());
            var logger = loggerFactory.
            CreateLogger<GroupChannelController>();
            using DatabaseContext dbContext = await SetUp();
            var controller = new GroupChannelController(logger, dbContext, _validator);
            controller.ControllerContext.HttpContext = _context;

            //act
            var actionResult = await controller.JoinChannel(Id);
            var okResult = actionResult as OkObjectResult;
            var channel = await dbContext.ChannelUsersStatuses.Where(x => x.UserId == 1).Where(x => x.ChannelId == int.Parse(Id)).FirstOrDefaultAsync();

            //assert
            Assert.NotNull(channel);
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
                    Type = "group",
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

        private async Task<DatabaseContext> SetUp()
        {
            var csus = GetChannelUserStatusesList();
            var channels = GetChannelsList();
            var context = CreateContext();
            await context.ChannelUsersStatuses.AddRangeAsync(csus);
            await context.Channels.AddRangeAsync(channels);
            await context.SaveChangesAsync();
            return context;
        }
    }
}