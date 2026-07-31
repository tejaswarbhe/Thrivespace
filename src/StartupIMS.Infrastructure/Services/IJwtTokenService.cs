using StartupIMS.Infrastructure.Persistence.Scaffolded.Identity;

namespace StartupIMS.Infrastructure.Services;

public interface IJwtTokenService
{
    (string token, DateTime expiresAt) GenerateAccessToken(User user);
    string GenerateRefreshToken();
    string HashToken(string rawToken);
}
