using Market.DTOs; 

namespace Market.Interfaces
{
    public interface IAuthService
    {
        Task<(bool Success, string Message)> RegisterAsync(RegisterDto dto);
        Task<string?> LoginAsync(LoginDto dto);
        VerifyResultDto Verify(string token);
    }
}