using Market.Data;
using Market.Models;

namespace Market.Services
{
    public interface ILogService
    {
        Task LogAsync(string action, string message, string? username = null);
    }

    public class LogService : ILogService
    {
        private readonly AppDbContext _context;

        public LogService(AppDbContext context)
        {
            _context = context;
        }

        public async Task LogAsync(string action, string message, string? username = null)
        {
            var log = new SystemLog
            {
                Action = action,
                Message = message,
                Username = username ?? "System",
                CreatedAt = DateTime.UtcNow
            };

            _context.SystemLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}