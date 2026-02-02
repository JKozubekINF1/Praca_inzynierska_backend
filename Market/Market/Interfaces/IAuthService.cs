using Market.DTOs;

namespace Market.Interfaces
{
    public interface IAuthService
    {
        Task<(bool Success, string Message)> RegisterAsync(RegisterDto dto);
        Task<string?> LoginAsync(LoginDto dto);
        VerifyResultDto Verify(string token);
        Task<(bool Success, string Message)> ForgotPasswordAsync(string email);
        Task<(bool Success, string Message)> ResetPasswordAsync(ResetPasswordDto dto);
        Task<(bool Success, string Message)> ActivateAccountAsync(string token);
    }
}