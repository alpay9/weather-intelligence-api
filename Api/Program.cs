using System.Threading.RateLimiting;
using FluentValidation;
using FluentValidation.AspNetCore;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Application.Abstractions.Auth;
using Infrastructure.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Controllers + FluentValidation
builder.Services.AddControllers()
    .AddJsonOptions(o => { o.JsonSerializerOptions.PropertyNamingPolicy = null; });
builder.Services.AddFluentValidationAutoValidation();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// EF Core (Postgres)
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

// Rate limiting policies (values from config)
var weatherPerMin = builder.Configuration.GetValue<int>("RateLimits:WeatherPerMinute", 10);
var insightsPerMin = builder.Configuration.GetValue<int>("RateLimits:InsightsPerMinute", 2);
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("weather", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.Identity?.Name ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "anon",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = weatherPerMin,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
    options.AddPolicy("insights", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.Identity?.Name ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "anon",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = insightsPerMin,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

// CORS (allow local dev frontends)
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyHeader().AllowAnyMethod().WithOrigins("http://localhost:3000")));

// Register auth services
builder.Services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
builder.Services.AddScoped<ITokenService, TokenService>();

// JWT Authentication
var key = Encoding.UTF8.GetBytes(builder.Configuration["JWT_SECRET"]!);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    });

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<Infrastructure.Persistence.AppDbContext>();
    db.Database.Migrate(); // applies any pending migrations at startup
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "WeatherIntel v1"));
}

app.UseAuthentication();
app.UseCors();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers(); // We'll add /api/v1 routing on controllers later

app.Run();
