using Market.DTOs;

namespace Market.Interfaces
{
    public interface IFavoriteService
    {
        Task<FavoriteResponseDto> ToggleFavoriteAsync(int userId, int announcementId);
        Task<List<AnnouncementListDto>> GetUserFavoritesAsync(int userId);
        Task<List<int>> GetUserFavoriteIdsAsync(int userId);
    }
}