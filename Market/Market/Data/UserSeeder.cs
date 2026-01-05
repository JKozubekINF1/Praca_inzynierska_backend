using Market.Models;
using BCrypt.Net;

namespace Market.Data
{
    public class UserSeeder
    {
        private readonly AppDbContext _context;

        public UserSeeder(AppDbContext context)
        {
            _context = context;
        }

        public void Seed()
        {
            if (_context.Database.CanConnect())
            {
                if (!_context.Users.Any(u => u.Role == "Admin"))
                {
                    var admin = new User()
                    {
                        Username = "admin",
                        Email = "admin@market.pl",
                        Role = "Admin",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!")
                    };

                    _context.Users.Add(admin);
                    _context.SaveChanges();
                }
            }
        }
    }
}