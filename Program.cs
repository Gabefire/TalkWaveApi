using System.Net;
using Microsoft.EntityFrameworkCore;
using TalkWaveApi.Services;

var builder = WebApplication.CreateBuilder(args);


// Add services to the container.
var Configuration = builder.Configuration;
builder.Services.AddDbContext<DatabaseContext>(options =>
options.UseNpgsql(Configuration.GetConnectionString("DefaultConnection"), x => x.MigrationsHistoryTable("_EfMigrations", Configuration.GetSection("Schema").GetSection("TalkwaveDataSchema").Value)));

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

//Todo see if allowedOrigins will be needed
app.UseWebSockets();

app.UseAuthorization();

app.MapControllers();

app.Run();
