using BLL.Services;
using DAL;
using DAL.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

namespace ElectricityAPI
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

            string? connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionString));

            builder.Services.AddHostedService<WeatherUpdateService>();
            builder.Services.AddHttpClient();
            builder.Services.AddScoped<WeatherSyncService>();
            builder.Services.AddScoped<ConsumptionSyncService>();
            builder.Services.AddScoped<ForecastScriptService>();
            builder.Services.AddScoped<BuildingRepository>();
            builder.Services.AddScoped<WeatherRepository>();
            builder.Services.AddScoped<ConsumptionRepository>();
            builder.Services.AddScoped<ForecastRepository>();
            builder.Services.AddScoped<RefreshTokenRepository>();
            builder.Services.AddScoped<BuildingQueryService>();
            builder.Services.AddScoped<BuildingMapService>();
            builder.Services.AddScoped<UserRepository>();
            builder.Services.AddScoped<AuthService>();

            string jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured.");
            string? jwtIssuer = builder.Configuration["Jwt:Issuer"];
            string? jwtAudience = builder.Configuration["Jwt:Audience"];

            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateIssuerSigningKey = true,
                        ValidateLifetime = true,
                        ValidIssuer = jwtIssuer,
                        ValidAudience = jwtAudience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                        ClockSkew = TimeSpan.Zero
                    };
                });

            builder.Services.AddAuthorization();

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Enter JWT token"
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            WebApplication app = builder.Build();

            using (IServiceScope scope = app.Services.CreateScope())
            {
                IServiceProvider services = scope.ServiceProvider;
                AppDbContext context = services.GetRequiredService<AppDbContext>();

                await ElectricityAPI.Data.DatabaseSeeder.SeedBuildingsAsync(context);
            }

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
