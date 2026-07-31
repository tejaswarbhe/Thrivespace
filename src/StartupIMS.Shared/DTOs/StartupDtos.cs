namespace StartupIMS.Shared.DTOs;

public record CreateStartupRequest(string Name, string Domain, string? Description, DateOnly FoundingDate);
