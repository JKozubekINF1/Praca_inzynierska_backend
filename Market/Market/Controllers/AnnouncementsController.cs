using Algolia.Search.Clients;
using Market.Data;
using Market.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace Market.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AnnouncementsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ISearchClient _algolia;       // <--- DODANO POLA
        private readonly IConfiguration _configuration; // <--- DODANO POLA

        // ZAKTUALIZOWANY KONSTRUKTOR
        public AnnouncementsController(AppDbContext context, ISearchClient algolia, IConfiguration configuration)
        {
            _context = context;
            _algolia = algolia;             // <--- PRZYPISANIE
            _configuration = configuration; // <--- PRZYPISANIE
        }

        [HttpPost]
        public async Task<IActionResult> CreateAnnouncement([FromBody] AnnouncementDto dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Nie jesteś zalogowany.");
            }

            // --- WALIDACJA ---
            if (string.IsNullOrEmpty(dto.Title) || dto.Title.Length > 100)
                return BadRequest("Tytuł jest wymagany i nie może przekraczać 100 znaków.");
            if (string.IsNullOrEmpty(dto.Description) || dto.Description.Length > 2000)
                return BadRequest("Opis jest wymagany i nie może przekraczać 2000 znaków.");
            if (dto.Price <= 0)
                return BadRequest("Cena musi być większa od 0.");
            if (string.IsNullOrEmpty(dto.Category) || !new[] { "Pojazd", "Część" }.Contains(dto.Category))
                return BadRequest("Kategoria musi być 'Pojazd' lub 'Część'.");
            if (string.IsNullOrEmpty(dto.PhoneNumber) || dto.PhoneNumber.Length > 15)
                return BadRequest("Numer telefonu jest wymagany i nie może przekraczać 15 znaków.");
            if (string.IsNullOrEmpty(dto.ContactPreference) || !new[] { "Telefon", "Email" }.Contains(dto.ContactPreference))
                return BadRequest("Preferowana forma kontaktu musi być 'Telefon' lub 'Email'.");

            // --- TWORZENIE OBIEKTU ---
            var announcement = new Announcement
            {
                Title = dto.Title,
                Description = dto.Description,
                Price = dto.Price,
                IsNegotiable = dto.IsNegotiable,
                Category = dto.Category,
                UserId = int.Parse(userId),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(30),
                PhoneNumber = dto.PhoneNumber,
                ContactPreference = dto.ContactPreference,
                TypeSpecificData = dto.Category == "Pojazd"
                    ? JsonSerializer.Serialize(dto.VehicleData)
                    : JsonSerializer.Serialize(dto.PartData)
            };

            // --- ZAPIS DO SQL ---
            _context.Announcements.Add(announcement);
            await _context.SaveChangesAsync();

            // --- INTEGRACJA Z ALGOLIĄ (DODANO) ---
            // To sprawia, że nowe ogłoszenie od razu pojawia się w wyszukiwarce
            try
            {
                var indexName = _configuration["Algolia:IndexName"];
                var index = _algolia.InitIndex(indexName);

                var indexModel = new AnnouncementIndexModel
                {
                    ObjectID = announcement.Id.ToString(),
                    Id = announcement.Id,
                    Title = announcement.Title,
                    Price = announcement.Price,
                    Category = announcement.Category
                    // Możesz tu dodać opis, jeśli chcesz szukać po treści:
                    // Description = announcement.Description.Length > 200 ? announcement.Description.Substring(0, 200) : announcement.Description
                };

                // Dodajemy dane pojazdu do indeksu (Marka, Rok, Przebieg)
                if (announcement.Category == "Pojazd" && dto.VehicleData != null)
                {
                    indexModel.Brand = dto.VehicleData.Brand;
                    indexModel.Year = dto.VehicleData.Year;
                    indexModel.Mileage = dto.VehicleData.Mileage;
                }

                // Wysyłamy do chmury (asynchronicznie)
                await index.SaveObjectAsync(indexModel);
            }
            catch (Exception ex)
            {
                // Logujemy błąd, ale nie przerywamy requestu użytkownikowi.
                // Ogłoszenie jest w SQL, najwyżej nie ma go w wyszukiwarce.
                Console.WriteLine($"Błąd synchronizacji z Algolią: {ex.Message}");
            }

            return Ok(new { Message = "Ogłoszenie dodane pomyślnie.", AnnouncementId = announcement.Id });
        }

        [HttpPost("{id}/renew")]
        public async Task<IActionResult> RenewAnnouncement(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized("Nie jesteś zalogowany.");

            var announcement = await _context.Announcements.FindAsync(id);
            if (announcement == null) return NotFound("Ogłoszenie nie zostało znalezione.");

            if (announcement.UserId != int.Parse(userId)) return Forbid("Nie masz uprawnień do przedłużenia tego ogłoszenia.");

            announcement.ExpiresAt = DateTime.UtcNow.AddDays(30);
            await _context.SaveChangesAsync();

            // Opcjonalnie: Tutaj też można zaktualizować Algolię (jeśli np. trzymasz tam datę wygaśnięcia)

            return Ok(new { Message = "Ogłoszenie przedłużone pomyślnie.", NewExpirationDate = announcement.ExpiresAt });
        }

        [HttpGet("active")]
        [AllowAnonymous]
        public IActionResult GetActiveAnnouncements()
        {
            var activeAnnouncements = _context.Announcements
                .Where(a => a.ExpiresAt >= DateTime.UtcNow)
                .ToList();

            return Ok(activeAnnouncements);
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAnnouncement(int id)
        {
            var announcement = await _context.Announcements
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (announcement == null) return NotFound();

            var response = new
            {
                announcement.Id,
                announcement.Title,
                announcement.Description,
                announcement.Price,
                announcement.IsNegotiable,
                announcement.Category,
                announcement.CreatedAt,
                announcement.ExpiresAt,
                announcement.PhoneNumber,
                announcement.ContactPreference,
                announcement.TypeSpecificData,
                IsActive = announcement.ExpiresAt >= DateTime.UtcNow,
                User = announcement.User != null ? new
                {
                    announcement.User.Id,
                    announcement.User.Username,
                    announcement.User.Email,
                    announcement.User.Name,
                    announcement.User.Surname
                } : null
            };

            return Ok(response);
        }

        [HttpGet("user/me/announcements")]
        public async Task<IActionResult> GetUserAnnouncements()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized("Nie jesteś zalogowany.");

            var announcements = await _context.Announcements
                .Where(a => a.UserId == int.Parse(userId))
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => new
                {
                    a.Id,
                    a.Title,
                    a.Price,
                    a.Category,
                    a.CreatedAt,
                    a.ExpiresAt,
                    IsActive = a.ExpiresAt >= DateTime.UtcNow
                })
                .ToListAsync();

            return Ok(announcements);
        }

        [HttpPost("{id}/activate")]
        public async Task<IActionResult> ActivateAnnouncement(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized("Nie jesteś zalogowany.");

            var announcement = await _context.Announcements.FindAsync(id);
            if (announcement == null) return NotFound("Ogłoszenie nie zostało znalezione.");

            if (announcement.UserId != int.Parse(userId)) return Forbid("Nie masz uprawnień do aktywacji tego ogłoszenia.");

            if (announcement.ExpiresAt < DateTime.UtcNow)
            {
                announcement.ExpiresAt = DateTime.UtcNow.AddDays(30);
            }
            else
            {
                announcement.ExpiresAt = announcement.ExpiresAt.AddDays(30);
            }

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Ogłoszenie aktywowane pomyślnie.", NewExpirationDate = announcement.ExpiresAt });
        }

        [HttpPost("sync-algolia")]
        [AllowAnonymous]
        public async Task<IActionResult> SyncAllToAlgolia()
        {
            var indexName = _configuration["Algolia:IndexName"];
            var index = _algolia.InitIndex(indexName);

            var announcements = await _context.Announcements.ToListAsync();
            var algoliaObjects = new List<AnnouncementIndexModel>();

            foreach (var a in announcements)
            {
                var model = new AnnouncementIndexModel
                {
                    ObjectID = a.Id.ToString(),
                    Id = a.Id,
                    Title = a.Title,
                    Price = a.Price,
                    Category = a.Category
                };

                if (a.Category == "Pojazd" && !string.IsNullOrEmpty(a.TypeSpecificData))
                {
                    try
                    {
                        var vehicle = JsonSerializer.Deserialize<VehicleData>(a.TypeSpecificData);
                        if (vehicle != null)
                        {
                            model.Brand = vehicle.Brand;
                            model.Year = vehicle.Year;
                            model.Mileage = vehicle.Mileage;
                        }
                    }
                    catch {}
                }

                algoliaObjects.Add(model);
            }

            if (algoliaObjects.Any())
            {
                
                await index.SaveObjectsAsync(algoliaObjects);
            }

            return Ok(new { Message = $"Zsynchronizowano {algoliaObjects.Count} ogłoszeń z Algolią." });
        }


        [HttpGet("search")]
        [AllowAnonymous]
        public async Task<IActionResult> Search(
            [FromQuery] string? query,
            [FromQuery] decimal? minPrice,
            [FromQuery] decimal? maxPrice,
            [FromQuery] int? minYear,
            [FromQuery] int? maxYear,
            [FromQuery] string? category,
            [FromQuery] int page = 0,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var indexName = _configuration["Algolia:IndexName"];
                var index = _algolia.InitIndex(indexName);

                var filters = new List<string>();

                if (minPrice.HasValue) filters.Add($"price >= {minPrice}");
                if (maxPrice.HasValue) filters.Add($"price <= {maxPrice}");
                if (minYear.HasValue) filters.Add($"year >= {minYear}");
                if (maxYear.HasValue) filters.Add($"year <= {maxYear}");
                if (!string.IsNullOrEmpty(category)) filters.Add($"category:{category}");

                string filterString = string.Join(" AND ", filters);

                var searchQ = new Algolia.Search.Models.Search.Query(query ?? "")
                {
                    Filters = filterString,
                    Page = page,
                    HitsPerPage = pageSize
                };

                var result = await index.SearchAsync<AnnouncementIndexModel>(searchQ);

                return Ok(new
                {
                    TotalHits = result.NbHits,
                    TotalPages = result.NbPages,
                    CurrentPage = result.Page,
                    Items = result.Hits 
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Błąd wyszukiwania: {ex.Message}");
            }
        }
    }
}