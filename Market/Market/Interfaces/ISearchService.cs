using Market.Models;
using Market.DTOs;

namespace Market.Interfaces
{
    public interface ISearchService
    {
        Task IndexAnnouncementAsync(Announcement announcement);
        Task<SearchResultDto> SearchAsync(SearchQueryDto query);
        Task IndexManyAnnouncementsAsync(IEnumerable<Announcement> announcements);
    }
}