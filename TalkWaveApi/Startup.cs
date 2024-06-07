using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using TalkWaveApi.Services;
using TalkWaveApi.Util;
using TalkWaveApi.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace TalkWaveApi;

public class Startup
{
    public Startup(IWebHostEnvironment env, IConfiguration configuration)
    {
        Configuration = configuration;
        CurrentEnvironment = env;
    }

    public IConfiguration Configuration { get; }

    public IWebHostEnvironment CurrentEnvironment { get; }
    // This method gets called by the runtime. Use this method to add services to the container
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddCors(p => p.AddPolicy("corsapp", builder =>
        {
            builder.WithOrigins("*").AllowAnyMethod().AllowAnyHeader().AllowCredentials();
        }));


        services.AddControllers();
        services.AddHealthChecks();
        services.AddDbContext<DatabaseContext>(options =>
        options.UseNpgsql(Configuration.GetConnectionString("DefaultConnection"), x => x.MigrationsHistoryTable("_EfMigrations", Configuration.GetSection("Schema").GetSection("TalkwaveDataSchema").Value)));

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(x =>
        {
            x.RequireHttpsMetadata = false;
            var key = Encoding.UTF8.GetBytes(Configuration["JwtSettings:Key"]!);
            x.SaveToken = true;
            // remove/change the below two in deployment
            x.Authority = Configuration["JwtSettings:Authority"]!;
            if (CurrentEnvironment.IsDevelopment())
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
        services.AddScoped<IValidator, Validator>();
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        services.AddEndpointsApiExplorer();
        if (CurrentEnvironment.IsDevelopment())
        {
            services.AddSwaggerGen(option =>
                {
                    option.SwaggerDoc("v1", new OpenApiInfo { Title = "TalkWave API", Version = "v1" });
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
        }

    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        // Configure the HTTP request pipeline.
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        app.UseCors("corsapp");
        app.UseHttpsRedirection();

        //Todo see if allowedOrigins will be needed
        var webSocketOptions = new WebSocketOptions
        {
            KeepAliveInterval = TimeSpan.FromSeconds(30)
        };
        app.UseHealthChecks("/health");
        app.UseWebSockets(webSocketOptions);

        app.UseRouting();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapGet("/", async context =>
            {
                await context.Response.WriteAsync("Welcome to running ASP.NET Core on AWS Lambda");
            });
        });
    }
}