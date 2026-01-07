using Market.DTOs;       
using Market.DTOs.Admin;  
using Market.Helpers;     
using Market.Models;      

namespace Market.Interfaces
{
    public interface IAdminService
    {
        Task<PagedResult<AdminUserDto>> GetUsersAsync(PaginationParams pagination);
        Task DeleteUserAsync(int id, string adminName);
        Task<PagedResult<AdminAnnouncementDto>> GetAnnouncementsAsync(PaginationParams pagination);
        Task DeleteAnnouncementAsync(int id, string adminName);
        Task<List<SystemLog>> GetLogsAsync();
        Task<object> GetUserDetailAsync(int id);
        Task<bool> ToggleBanAsync(int id, string adminName);
        Task<AdminStatsDto> GetStatsAsync();
    }
}