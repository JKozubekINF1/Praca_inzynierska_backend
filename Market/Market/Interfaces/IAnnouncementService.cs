using Market.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Market.Interfaces
{
    public interface IAnnouncementService
    {
        Task<int> CreateAsync(CreateAnnouncementDto dto, int userId);
        Task<AnnouncementDto?> GetByIdAsync(int id);
        Task<List<AnnouncementListDto>> GetUserAnnouncementsAsync(int userId);
        Task ActivateAsync(int id, int userId);
        Task RenewAsync(int id, int userId);
        Task DeleteAsync(int id, int userId);
        Task UpdateAsync(int id, CreateAnnouncementDto dto, int userId);
        Task<SearchResultDto> SearchAsync(SearchQueryDto dto);
        Task<int> SyncAllToSearchAsync();
    }
}