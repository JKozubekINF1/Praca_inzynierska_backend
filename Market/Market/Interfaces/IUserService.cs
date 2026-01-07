using Market.DTOs;

namespace Market.Interfaces
{
    public interface IUserService
    {
        Task<UserDto> GetProfileAsync(int userId);
        Task UpdateProfileAsync(int userId, UserDto dto);
        Task ChangePasswordAsync(int userId, ChangePasswordDto dto);
    }
}