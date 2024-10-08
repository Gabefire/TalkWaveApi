using TalkWaveApi.WebSocket.Hubs;
using TalkWaveApi.WebSocket.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var Configuration = builder.Configuration;

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddSingleton<IUserIdProvider, UserIdProvider>();

builder.Services.AddCors(p => p.AddPolicy("dev", builder =>
{
    builder.WithOrigins("http://localhost:5173").AllowAnyMethod().AllowAnyHeader().AllowCredentials();
}));

builder.Services.AddCors(p => p.AddPolicy("prod", builder =>
{
    builder.WithOrigins("https://talkwaveapp.com").AllowAnyMethod().AllowAnyHeader().AllowCredentials();
}));


builder.Services.AddDbContext<DatabaseContext>(options =>
options.UseNpgsql(Configuration.GetConnectionString("DefaultConnection"), x =>
{
    x.MigrationsHistoryTable("_EfMigrations", Configuration.GetSection("Schema").GetSection("TalkwaveDataSchema").Value);
}));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddHealthChecks();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(x =>
  {
      var key = Encoding.UTF8.GetBytes(Configuration["JwtSettings:Key"]!);
      x.RequireHttpsMetadata = false;
      x.Authority = Configuration["JwtSettings:Authority"]!;
      x.TokenValidationParameters = new TokenValidationParameters
      {
          ValidateIssuer = false,
          ValidateAudience = false,
          ValidateLifetime = true,
          ValidateIssuerSigningKey = true,
          ClockSkew = TimeSpan.Zero,
          IssuerSigningKey = new SymmetricSecurityKey(key)
      };
      x.Events = new JwtBearerEvents
      {
          OnMessageReceived = context =>
          {

              var accessToken = context.Request.Query["access_token"];
              // If the request is for our hub...
              var path = context.HttpContext.Request.Path;
              if (!string.IsNullOrEmpty(accessToken) &&
                path.StartsWithSegments("/api/Message"))
              {
                  // Read the token out of the query string
                  context.Token = accessToken;
              }
              return Task.CompletedTask;
          },
      };
  });



builder.Services.AddSignalR(hubOptions =>
    {
        hubOptions.EnableDetailedErrors = true;
        hubOptions.KeepAliveInterval = TimeSpan.FromSeconds(15);
        hubOptions.HandshakeTimeout = TimeSpan.FromSeconds(15);
        hubOptions.MaximumParallelInvocationsPerClient = 5;

    });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseCors("dev");
}
else if (app.Environment.IsProduction())
{
    app.UseCors("prod");
}

app.UseAuthentication();
app.UseRouting();
app.UseAuthorization();
app.MapHealthChecks("/health");
app.MapHub<ChatHub>("/api/Message");
app.MapGet("/", async context =>
    {
        await context.Response.WriteAsync("Welcome to running ASP.NET Core on ECS");
    });
app.Run();
