using Algolia.Search.Clients;
using Market.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json.Serialization;
using Market.Services;
using Market.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("DevelopmentCors", policyBuilder =>
    {
        policyBuilder.SetIsOriginAllowed(origin => true) 
                     .AllowAnyMethod()
                     .AllowAnyHeader()
                     .AllowCredentials(); 
    });
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IFileService, LocalFileService>();
builder.Services.AddScoped<ISearchService, AlgoliaSearchService>();
builder.Services.AddScoped<IAnnouncementService, AnnouncementService>();

var algoliaSettings = builder.Configuration.GetSection("Algolia");
string algoliaAppId = algoliaSettings["AppId"];
string algoliaApiKey = algoliaSettings["ApiKey"];

if (!string.IsNullOrEmpty(algoliaAppId) && !string.IsNullOrEmpty(algoliaApiKey))
{
    builder.Services.AddSingleton<ISearchClient>(new SearchClient(algoliaAppId, algoliaApiKey));
}
else
{
    Console.WriteLine("OSTRZE¯ENIE: Brak konfiguracji Algolia w appsettings.json.");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Brak klucza JWT.")))
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            context.Request.Cookies.TryGetValue("AuthToken", out var token);
            if (!string.IsNullOrEmpty(token))
            {
                context.Token = token;
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Market API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter JWT with Bearer into field (e.g., 'Bearer your_token_here')",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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
            new string[] {}
        }
    });
});

builder.Services.AddHostedService<ExpiredAnnouncementsCleanupService>();

var app = builder.Build();

app.UseCookiePolicy(new CookiePolicyOptions
{
    HttpOnly = Microsoft.AspNetCore.CookiePolicy.HttpOnlyPolicy.Always,
    Secure = CookieSecurePolicy.Always
});

app.UseCors("DevelopmentCors");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Market API V1");
    });
}

app.UseHttpsRedirection();
app.UseStaticFiles(); 
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public class ExpiredAnnouncementsCleanupService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<ExpiredAnnouncementsCleanupService> _logger;

    public ExpiredAnnouncementsCleanupService(IServiceProvider services,
        ILogger<ExpiredAnnouncementsCleanupService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Expired Announcements Cleanup Service is running.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _services.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    var expiredAnnouncements = dbContext.Announcements
                        .Where(a => a.ExpiresAt < DateTime.UtcNow)
                        .ToList();

                    if (expiredAnnouncements.Any())
                    {
                        dbContext.Announcements.RemoveRange(expiredAnnouncements);
                        await dbContext.SaveChangesAsync();
                        _logger.LogInformation($"Removed {expiredAnnouncements.Count} expired announcements.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while cleaning up expired announcements.");
            }

            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}