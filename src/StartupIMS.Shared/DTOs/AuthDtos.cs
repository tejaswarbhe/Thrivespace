using StartupIMS.Domain.Enums;

namespace StartupIMS.Shared.DTOs;

public record RegisterRequest(
    string Name,
    string Email,
    string Password,
    UserRole Role,
    // Only used when Role == Founder - creates the Startup row in the same call
    string? StartupName = null,
    string? StartupDomain = null,
    string? StartupDescription = null,
    DateOnly? StartupFoundingDate = null,
    // Only used when Role == Mentor - creates the Mentor profile row in the same call
    string? MentorExpertise = null,
    int? MentorExperienceYears = null,
    string? MentorOrganization = null
);

public record LoginRequest(string Email, string Password);

public record RefreshRequest(string RefreshToken);

public record AuthResponse(string AccessToken, string RefreshToken, DateTime AccessTokenExpiresAt, UserRole Role, int UserId);
