using System.ComponentModel.DataAnnotations;
using StartupIMS.Domain.Enums;

namespace StartupIMS.Shared.DTOs;

public record RegisterRequest(
    [Required, StringLength(150, MinimumLength = 2)]
    string Name,

    [Required, EmailAddress, StringLength(200)]
    string Email,

    [Required, StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")]
    string Password,

    UserRole Role,

    // Only used when Role == Founder - creates the Startup row in the same call
    [StringLength(200)] string? StartupName = null,
    [StringLength(100)] string? StartupDomain = null,
    [StringLength(2000)] string? StartupDescription = null,
    DateOnly? StartupFoundingDate = null,

    // Only used when Role == Mentor - creates the Mentor profile row in the same call
    [StringLength(200)] string? MentorExpertise = null,
    [Range(0, 80)] int? MentorExperienceYears = null,
    [StringLength(200)] string? MentorOrganization = null
);

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password
);

public record RefreshRequest(string RefreshToken);

public record AuthResponse(string AccessToken, string RefreshToken, DateTime AccessTokenExpiresAt, UserRole Role, int UserId);
