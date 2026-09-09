using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using StartupIMS.Infrastructure.Persistence.Scaffolded.Identity;
using StartupIMS.Infrastructure.Services;
using StartupIMS.Shared.Settings;
using Xunit;

namespace StartupIMS.Tests;

public class JwtTokenServiceTests
{
    private static JwtTokenService CreateService() => new(Options.Create(new JwtSettings
    {
        Secret = "test-secret-at-least-32-characters-long!!",
        Issuer = "TestIssuer",
        Audience = "TestAudience",
        AccessTokenMinutes = 15,
        RefreshTokenDays = 7
    }));

    private static User TestUser() => new()
    {
        Id = 42,
        Name = "Test User",
        Email = "test@example.com",
        Role = "Founder",
        PasswordHash = "irrelevant-here"
    };

    [Fact]
    public void GenerateAccessToken_IncludesExpectedClaims()
    {
        var service = CreateService();
        var (token, _) = service.GenerateAccessToken(TestUser());

        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal("42", parsed.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
        Assert.Equal("test@example.com", parsed.Claims.First(c => c.Type == ClaimTypes.Email).Value);
        Assert.Equal("Founder", parsed.Claims.First(c => c.Type == ClaimTypes.Role).Value);
    }

    [Fact]
    public void GenerateAccessToken_ExpiresApproximatelyAtConfiguredMinutes()
    {
        var service = CreateService();
        var (_, expiresAt) = service.GenerateAccessToken(TestUser());

        var expectedExpiry = DateTime.UtcNow.AddMinutes(15);
        Assert.True(Math.Abs((expiresAt - expectedExpiry).TotalSeconds) < 5, "Expiry should land within a few seconds of the configured 15-minute window.");
    }

    [Fact]
    public void GenerateRefreshToken_ProducesDifferentValuesEachCall()
    {
        var service = CreateService();
        var first = service.GenerateRefreshToken();
        var second = service.GenerateRefreshToken();

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void HashToken_IsDeterministic_SameInputSameOutput()
    {
        var service = CreateService();
        var hash1 = service.HashToken("some-refresh-token-value");
        var hash2 = service.HashToken("some-refresh-token-value");

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void HashToken_DifferentInputsProduceDifferentHashes()
    {
        var service = CreateService();
        var hashA = service.HashToken("token-a");
        var hashB = service.HashToken("token-b");

        Assert.NotEqual(hashA, hashB);
    }

    [Fact]
    public void HashToken_NeverReturnsTheRawInput()
    {
        // Guards the actual security property we care about: the stored
        // value must never equal the raw token, even by coincidence.
        var service = CreateService();
        var raw = "my-raw-refresh-token";
        var hashed = service.HashToken(raw);

        Assert.NotEqual(raw, hashed);
    }
}
