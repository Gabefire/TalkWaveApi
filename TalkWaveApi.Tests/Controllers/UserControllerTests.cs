using Moq;
using TalkWaveApi.Controllers;
using TalkWaveApi.Models;
using TalkWaveApi.Services;
using Microsoft.AspNetCore.Http;
using TalkWaveApi.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace TalkWaveApi.Tests
{
    [Collection("TalkWaveApiTestCollection")]
    public class UserControllerTests : IDisposable
    {

        private readonly DbContextOptions<DatabaseContext> _contextOptions;

        private readonly IValidator _validator;

        private readonly HttpContext _context;
        private readonly IConfigurationRoot _configuration;

        public UserControllerTests()
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

            var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection()
            .Build();
            _configuration = configuration;
        }

        public void Dispose()
        {
            var dbContext = new DatabaseContext(_contextOptions);
            dbContext.ChangeTracker
            .Entries()
            .ToList()
            .ForEach(e => e.State = EntityState.Detached);
        }

        public static IEnumerable<object[]> Users()
        {
            {
                yield return new object[] { new UserDto { UserName = "test1", Password = "123", Email = "test@test.com" } };
                yield return new object[] { new UserDto { UserName = "test2", Password = "123", Email = "test2@test.com" } };
            }
        }

        [Theory]
        [MemberData(nameof(Users))]
        private async void Register(UserDto request)
        {
            //arrange
            using var loggerFactory = LoggerFactory.Create(loggingBuilder => loggingBuilder
            .SetMinimumLevel(LogLevel.Trace)
            .AddConsole());
            var logger = loggerFactory.
            CreateLogger<UserController>();

            using DatabaseContext dbContext = CreateContext();

            var controller = new UserController(_configuration, dbContext, logger, _validator);
            controller.ControllerContext.HttpContext = _context;

            //act
            var actionResult = await controller.Register(request);

            var user = await dbContext.Users.Where(x => x.Email == request.Email).SingleOrDefaultAsync();

            //asset
            Assert.NotNull(user);
        }

        [Theory]
        [MemberData(nameof(Users))]
        private async void Login(UserDto request)
        {
            //arrange
            using var loggerFactory = LoggerFactory.Create(loggingBuilder => loggingBuilder
            .SetMinimumLevel(LogLevel.Trace)
            .AddConsole());
            var logger = loggerFactory.
            CreateLogger<UserController>();

            using DatabaseContext dbContext = CreateContext();

            var inMemorySetting = new Dictionary<string, string>
            {
                {"JwtSettings:Key" , "this is my custom Secret key for authentication"},
            };

            var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySetting!)
            .Build();

            var controller = new UserController(configuration, dbContext, logger, _validator);
            controller.ControllerContext.HttpContext = _context;

            //act
            var actionResult = await controller.Register(request);

            var user = await dbContext.Users.Where(x => x.Email == request.Email).SingleOrDefaultAsync();

            var okResult = await controller.Login(request);

            //asset
            Assert.IsType<OkObjectResult>(okResult);
        }
        public static IEnumerable<object[]> EditUserEnum()
        {
            {
                yield return new object[] { new UserDto { UserName = "changed", Password = "123", Email = "test3@test.com" } };
            }
        }

        [Theory]
        [MemberData(nameof(EditUserEnum))]
        private async void EditUser(UserDto request)
        {
            //arrange
            using var loggerFactory = LoggerFactory.Create(loggingBuilder => loggingBuilder
            .SetMinimumLevel(LogLevel.Trace)
            .AddConsole());
            var logger = loggerFactory.
            CreateLogger<UserController>();

            using DatabaseContext dbContext = CreateContext();

            var controller = new UserController(_configuration, dbContext, logger, _validator);
            controller.ControllerContext.HttpContext = _context;

            //act
            var actionResult = await controller.EditUser(request);

            var user = await dbContext.Users.FindAsync(1);

            //asset
            Assert.NotNull(user);
            Assert.Equal(user.Email, request.Email);
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