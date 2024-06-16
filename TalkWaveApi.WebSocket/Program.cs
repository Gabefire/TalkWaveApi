using TalkWaveApi.WebSocket.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var Configuration = builder.Configuration;

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddSingleton<IUserIdProvider, EmailBasedUserIdProvider>();

builder.Services.AddCors(p => p.AddPolicy("dev", builder =>
{
    builder.WithOrigins("http://localhost:5173").AllowAnyMethod().AllowAnyHeader().AllowCredentials();
}));

builder.Services.AddCors(p => p.AddPolicy("prod", builder =>
{
    builder.AllowAnyMethod().AllowAnyHeader().AllowCredentials().SetIsOriginAllowed((host) => true);
}));

builder.Services.AddHealthChecks();

builder.Services.AddControllers();

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
        hubOptions.KeepAliveInterval = TimeSpan.FromSeconds(10);
        hubOptions.HandshakeTimeout = TimeSpan.FromSeconds(5);
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

//Todo see if allowedOrigins will be needed
var webSocketOptions = new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(30)
};
app.UseWebSockets(webSocketOptions);


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


public class EmailBasedUserIdProvider : IUserIdProvider
{
    public virtual string GetUserId(HubConnectionContext connection)
    {
        return connection.User?.FindFirst(ClaimTypes.Email)?.Value!;
    }
}