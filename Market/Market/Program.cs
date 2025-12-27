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

// --- 1. KONFIGURACJA CORS ---
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

// --- 2. BAZA DANYCH ---
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// --- 3. REJESTRACJA SERWISÓW (Dependency Injection) ---
builder.Services.AddScoped<IFileService, LocalFileService>();
builder.Services.AddScoped<ISearchService, AlgoliaSearchService>();
builder.Services.AddScoped<IAnnouncementService, AnnouncementService>();

// --- 4. ALGOLIA ---
var algoliaSettings = builder.Configuration.GetSection("Algolia");
string algoliaAppId = algoliaSettings["AppId"];
string algoliaApiKey = algoliaSettings["ApiKey"];

if (!string.IsNullOrEmpty(algoliaAppId) && !string.IsNullOrEmpty(algoliaApiKey))
{
    // Rejestrujemy klienta jako Singleton (zalecane przez Algolia)
    builder.Services.AddSingleton<ISearchClient>(new SearchClient(algoliaAppId, algoliaApiKey));
}
else
{
    Console.WriteLine("OSTRZE¯ENIE: Brak konfiguracji Algolia w appsettings.json.");
}

// --- 5. AUTH (JWT) ---
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

    // Obs³uga tokena z ciasteczek (dla bezpieczeñstwa)
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

// --- 6. KONTROLERY I JSON ---
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Zapobiega pêtlom referencji przy serializacji obiektów z relacjami
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

// --- 7. SWAGGER ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Market API", Version = "v1" });

    // Konfiguracja autoryzacji w Swaggerze (k³ódka)
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

// --- 8. BACKGROUND SERVICES ---
// Serwis dzia³aj¹cy w tle do sprz¹tania/dezaktywacji og³oszeñ
builder.Services.AddHostedService<ExpiredAnnouncementsCleanupService>();

var app = builder.Build();

// --- 9. MIDDLEWARE PIPELINE ---

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

// ---------------------------------------------------------
// POPRAWIONY SERWIS SPRZ¥TAJ¥CY (BACKGROUND SERVICE)
// ---------------------------------------------------------
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
        _logger.LogInformation("Serwis czyszcz¹cy wygas³e og³oszenia zosta³ uruchomiony.");

        // Sprawdzaj co godzinê (lub rzadziej), nie co 24h, ¿eby u¿ytkownicy szybciej widzieli zmiany
        var checkInterval = TimeSpan.FromHours(1);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _services.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var searchService = scope.ServiceProvider.GetRequiredService<ISearchService>(); // <--- POTRZEBNE DO ALGOLII

                    // ZnajdŸ og³oszenia, które wygas³y, ale wci¹¿ s¹ oznaczone jako aktywne
                    var expiredAnnouncements = await dbContext.Announcements
                        .Where(a => a.ExpiresAt < DateTime.UtcNow && a.IsActive)
                        .ToListAsync(stoppingToken);

                    if (expiredAnnouncements.Any())
                    {
                        foreach (var announcement in expiredAnnouncements)
                        {
                            // 1. Zmieniamy status na nieaktywny (zamiast usuwaæ!)
                            // Dziêki temu u¿ytkownik mo¿e je "przed³u¿yæ" póŸniej.
                            announcement.IsActive = false;

                            // 2. Aktualizujemy Algoliê (¿eby zniknê³o z wyników, bo IsActive:false)
                            // Mo¿emy u¿yæ IndexAnnouncementAsync (które ustawi IsActive=false w indeksie)
                            // lub RemoveAsync, jeœli wolisz usuwaæ z indeksu wygas³e.
                            // Lepiej zaktualizowaæ status:
                            await searchService.IndexAnnouncementAsync(announcement);
                        }

                        await dbContext.SaveChangesAsync(stoppingToken);
                        _logger.LogInformation($"Zdezaktywowano {expiredAnnouncements.Count} wygas³ych og³oszeñ i zaktualizowano Algoliê.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas czyszczenia wygas³ych og³oszeñ.");
            }

            await Task.Delay(checkInterval, stoppingToken);
        }
    }
}