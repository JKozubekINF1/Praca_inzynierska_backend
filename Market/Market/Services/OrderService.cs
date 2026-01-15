using Market.Data;
using Market.DTOs;
using Market.Interfaces;
using Market.Models;
using Microsoft.EntityFrameworkCore;

namespace Market.Services
{
    public class OrderService : IOrderService
    {
        private readonly AppDbContext _context;
        private readonly ISearchService _searchService;
        private readonly ILogService _logService;

        public OrderService(AppDbContext context, ISearchService searchService, ILogService logService)
        {
            _context = context;
            _searchService = searchService;
            _logService = logService;
        }
        public async Task<OrderResultDto> CreateOrderAsync(int buyerId, CreateOrderDto dto)
        {
            var announcement = await _context.Announcements
                .Include(a => a.User)
                .Include(a => a.VehicleDetails)
                .Include(a => a.PartDetails)
                .FirstOrDefaultAsync(a => a.Id == dto.AnnouncementId);

            if (announcement == null) throw new KeyNotFoundException("Ogłoszenie nie istnieje.");
            if (!announcement.IsActive) throw new InvalidOperationException("Ogłoszenie sprzedane.");
            if (announcement.UserId == buyerId) throw new InvalidOperationException("Nie możesz kupić własnego przedmiotu.");

            var buyer = await _context.Users.FindAsync(buyerId);

            if (buyer.Balance < announcement.Price)
                throw new InvalidOperationException("Niewystarczające środki.");
            buyer.Balance -= announcement.Price;
            announcement.User.Balance += announcement.Price;

            var order = new Order
            {
                BuyerId = buyerId,
                AnnouncementId = announcement.Id,
                TotalAmount = announcement.Price,
                Status = OrderStatus.Paid,
                OrderDate = DateTime.UtcNow,
                DeliveryMethod = dto.DeliveryMethod,
                DeliveryPointName = dto.DeliveryPointName,
                DeliveryAddress = dto.DeliveryAddress
            };

            _context.Orders.Add(order);
            announcement.IsActive = false; 

            await _context.SaveChangesAsync();
            try
            {
                await _searchService.IndexAnnouncementAsync(announcement);
            }
            catch (Exception ex)
            {
                await _logService.LogAsync("ALGOLIA_ERROR", $"Błąd aktualizacji po zakupie: {ex.Message}", "System");
            }

            return new OrderResultDto
            {
                OrderId = order.Id,
                AmountPaid = announcement.Price,
                RemainingBalance = buyer.Balance
            };
        }

        public async Task<IEnumerable<OrderHistoryDto>> GetUserOrdersAsync(int userId)
        {
            return await _context.Orders
                .Include(o => o.Announcement)
                .Where(o => o.BuyerId == userId)
                .OrderByDescending(o => o.OrderDate)
                .Select(o => new OrderHistoryDto
                {
                    OrderId = o.Id,
                    OrderDate = o.OrderDate,
                    TotalAmount = o.TotalAmount,
                    Status = o.Status.ToString(),
                    DeliveryMethod = o.DeliveryMethod,
                    DeliveryPointName = o.DeliveryPointName,
                    DeliveryAddress = o.DeliveryAddress,
                    AnnouncementTitle = o.Announcement.Title,
                    AnnouncementPhotoUrl = o.Announcement.PhotoUrl
                })
                .ToListAsync();
        }

        public async Task<decimal> GetBalanceAsync(int userId)
        {
            var balance = await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => u.Balance)
                .FirstOrDefaultAsync();
            return balance;
        }

        public async Task<decimal> TopUpAsync(int userId, decimal amount)
        {
            if (amount <= 0) throw new ArgumentException("Kwota musi być dodatnia.");

            var user = await _context.Users.FindAsync(userId);
            if (user == null) throw new KeyNotFoundException("Użytkownik nie znaleziony.");

            user.Balance += amount;
            await _context.SaveChangesAsync();

            return user.Balance;
        }
    }
}