using HireFlow.Identity.Domain.Entities;

namespace HireFlow.Identity.Application.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(User user, IEnumerable<string> roles);
    string GenerateRefreshToken();
    string HashToken(string token);
    string GenerateOtpCode();
    string HashOtp(string otp);
    int AccessTokenExpiryMinutes { get; }
}
