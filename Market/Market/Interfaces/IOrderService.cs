using Market.DTOs;

namespace Market.Interfaces
{
    public interface IOrderService
    {
        Task<OrderResultDto> CreateOrderAsync(int buyerId, CreateOrderDto dto);
        Task<IEnumerable<OrderHistoryDto>> GetUserOrdersAsync(int userId);
        Task<decimal> GetBalanceAsync(int userId);
        Task<decimal> TopUpAsync(int userId, decimal amount);
    }
}