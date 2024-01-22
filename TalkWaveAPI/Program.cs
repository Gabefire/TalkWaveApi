using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using TalkWaveApi.Services;
using TalkWaveApi.Util;
using TalkWaveApi.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Text;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);


// Add services to the container.
var Configuration = builder.Configuration;
builder.Services.AddDbContext<DatabaseContext>(options =>
options.UseNpgsql(Configuration.GetConnectionString("DefaultConnection"), x => x.MigrationsHistoryTable("_EfMigrations", Configuration.GetSection("Schema").GetSection("TalkwaveDataSchema").Value)));

builder.Services.AddControllers();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = false;
    var key = Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:Key"]!);
    x.SaveToken = true;
    // remove/change the below two in deployment
    x.Authority = builder.Configuration["JwtSettings:Authority"]!;
    if (builder.Environment.IsDevelopment())
    {
        x.RequireHttpsMetadata = false;
    }
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ClockSkew = TimeSpan.Zero,
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});
builder.Services.AddScoped<IValidator, Validator>();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(option =>
{
    option.SwaggerDoc("v1", new OpenApiInfo { Title = "Demo API", Version = "v1" });
    option.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter a valid token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });
    option.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type=ReferenceType.SecurityScheme,
                    Id="Bearer"
                }
            },
            new string[]{}
        }
    });
});

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
