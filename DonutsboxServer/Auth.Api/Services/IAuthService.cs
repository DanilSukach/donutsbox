using Auth.Api.Dto;

namespace Auth.Api.Services;

public interface IAuthService
{
    public Task RegisterAsync(RegisterRequestDto dto);
    public Task<AuthResponseDto> LoginAsync(LoginRequestDto dto);
    public Task<AuthResponseDto> RefreshTokenAsync(RefreshRequestDto dto);
    public Task CreateAdminAsync(RegisterRequestDto dto);
}
