using Market.DTOs;
using Market.Models;
using System.Threading.Tasks;

namespace Market.Interfaces
{
    public interface IAnnouncementService
    {
        Task<int> CreateAsync(CreateAnnouncementDto dto, int userId);
        Task<Announcement?> GetByIdAsync(int id);
        Task<IEnumerable<AnnouncementListDto>> GetUserAnnouncementsAsync(int userId);
        Task RenewAsync(int id, int userId);
        Task ActivateAsync(int id, int userId);
        Task<SearchResultDto> SearchAsync(SearchQueryDto query);
        Task<int> SyncAllToSearchAsync();
    }
}