using System.ComponentModel.DataAnnotations;

namespace StartupIMS.Shared.DTOs;

public record CreateStartupRequest(
    [Required, StringLength(200, MinimumLength = 2)] string Name,
    [Required, StringLength(100)] string Domain,
    [StringLength(2000)] string? Description,
    DateOnly FoundingDate
);

public record UpdateStartupRequest(
    [Required, StringLength(200, MinimumLength = 2)] string Name,
    [Required, StringLength(100)] string Domain,
    [StringLength(2000)] string? Description
);

public record PitchDeckInfo(string OriginalFileName, DateTime UploadedAt);
