using Moq;
using TalkWaveApi.Controllers;
using TalkWaveApi.Models;
using TalkWaveApi.Services;
using Microsoft.AspNetCore.Http;
using TalkWaveApi.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace TalkWaveApi.Tests
{
    [Collection("TalkWaveApiTestCollection")]
    public class UserChannelControllerTests : IDisposable
    {

        private readonly DbContextOptions<DatabaseContext> _contextOptions;

        private readonly IValidator _validator;

        private readonly HttpContext _context;

        public UserChannelControllerTests()
        {
            _contextOptions = new DbContextOptionsBuilder<DatabaseContext>().UseInMemoryDatabase(databaseName: "TalkWave")
            .Options;

            var dbContext = new DatabaseContext(_contextOptions);

            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            SetUp(dbContext);

            _context = new DefaultHttpContext();

            var validateMock = new Mock<IValidator>();

            validateMock.Setup(x => x.ValidateJwt(_context)).Returns(Task.FromResult<User?>(new User
            {
                UserId = 1,
                UserName = "test1",
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


        [Theory]
        [InlineData("2")]
        private async void CreateChannel(string UserId)
        {
            //arrange
            using var loggerFactory = LoggerFactory.Create(loggingBuilder => loggingBuilder
            .SetMinimumLevel(LogLevel.Trace)
            .AddConsole());
            var logger = loggerFactory.
            CreateLogger<UserChannelController>();

            using DatabaseContext dbContext = CreateContext();

            var controller = new UserChannelController(logger, dbContext, _validator);
            controller.ControllerContext.HttpContext = _context;

            //act
            var actionResult = await controller.CreateChannel(UserId);

            var userCSU = await dbContext.ChannelUsersStatuses.Where(x => x.UserId == 1).SingleOrDefaultAsync();

            var requestedCSU = await dbContext.ChannelUsersStatuses.Where(x => x.UserId == 2).SingleOrDefaultAsync();

            var channel = await dbContext.Channels.Where(x => x.ChannelId == 5).SingleOrDefaultAsync();

            //asset
            Assert.NotNull(userCSU);
            Assert.NotNull(requestedCSU);
            Assert.NotNull(channel);
        }

        [Theory]
        [InlineData("2")]
        private async void CreateChannelConflict(string UserId)
        {
            //arrange
            using var loggerFactory = LoggerFactory.Create(loggingBuilder => loggingBuilder
            .SetMinimumLevel(LogLevel.Trace)
            .AddConsole());
            var logger = loggerFactory.
            CreateLogger<UserChannelController>();

            using DatabaseContext dbContext = CreateContext();

            var controller = new UserChannelController(logger, dbContext, _validator);
            controller.ControllerContext.HttpContext = _context;

            //act
            var okResult = await controller.CreateChannel(UserId);
            var conflictResult = await controller.CreateChannel(UserId);

            //asset
            Assert.IsType<CreatedAtActionResult>(okResult);
            Assert.IsType<ConflictObjectResult>(conflictResult);
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

        private List<User> GetUserList()
        {
            List<User> users = new List<User>
            {
                new User
                {
                    UserId = 1,
                    UserName = "test1"
                },
                new User
                {
                    UserId = 2,
                    UserName = "test2"
                }
            };
            return users;
        }
        DatabaseContext CreateContext() => new(_contextOptions);

        private void SetUp(DatabaseContext context)
        {
            var channels = GetChannelsList();
            var users = GetUserList();

            context.Channels.AddRange(channels);
            context.Users.AddRange(users);
            context.SaveChanges();
        }
    }
}