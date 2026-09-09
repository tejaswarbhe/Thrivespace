using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StartupIMS.Infrastructure.Persistence;
using StartupIMS.Infrastructure.Persistence.Scaffolded.Core;
using StartupIMS.Infrastructure.Services;
using StartupIMS.Shared.DTOs;
using StartupIMS.Shared.Settings;
using Microsoft.Extensions.Options;

namespace StartupIMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StartupsController : ControllerBase
{
    private static readonly string[] AllowedPitchDeckExtensions = { ".pdf", ".pptx" };

    private readonly CoreDbContext _db;
    private readonly IFileStorageService _files;
    private readonly FileStorageSettings _fileSettings;
    private readonly IStartupVisibilityService _visibility;

    public StartupsController(CoreDbContext db, IFileStorageService files, IOptions<FileStorageSettings> fileSettings, IStartupVisibilityService visibility)
    {
        _db = db;
        _files = files;
        _fileSettings = fileSettings.Value;
        _visibility = visibility;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private bool IsAdmin => User.IsInRole("Admin");
    private bool IsMentor => User.IsInRole("Mentor");

    private static StartupResponse ToResponse(Startup s) => new(
        s.Id, s.UserId, s.Name, s.Domain, s.Description, s.FoundingDate, s.Status,
        s.PitchDeckOriginalFileName, s.PitchDeckUploadedAt);

    [HttpGet]
    public async Task<ActionResult<IEnumerable<StartupResponse>>> GetAll()
    {
        List<Startup> startups;

        if (IsAdmin)
        {
            startups = await _db.Startups.ToListAsync();
        }
        else
        {
            var visibleIds = await _visibility.GetVisibleStartupIdsAsync(CurrentUserId, IsMentor);
            startups = await _db.Startups.Where(s => visibleIds.Contains(s.Id)).ToListAsync();
        }

        return startups.Select(ToResponse).ToList();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<StartupResponse>> GetById(int id)
    {
        var startup = await _db.Startups.FindAsync(id);
        if (startup is null) return NotFound();

        if (!IsAdmin && !IsMentor && startup.UserId != CurrentUserId)
            return Forbid();

        return ToResponse(startup);
    }

    [HttpPost]
    [Authorize(Policy = "FounderOnly")]
    public async Task<ActionResult<StartupResponse>> Create(CreateStartupRequest request)
    {
        var startup = new Startup
        {
            UserId = CurrentUserId,
            Name = request.Name,
            Domain = request.Domain,
            Description = request.Description,
            FoundingDate = request.FoundingDate,
            Status = "Applied"
        };

        _db.Startups.Add(startup);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = startup.Id }, ToResponse(startup));
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "FounderOnly")]
    public async Task<ActionResult<StartupResponse>> Update(int id, UpdateStartupRequest request)
    {
        var startup = await _db.Startups.FindAsync(id);
        if (startup is null) return NotFound();

        if (startup.UserId != CurrentUserId)
            return Forbid();

        startup.Name = request.Name;
        startup.Domain = request.Domain;
        startup.Description = request.Description;

        await _db.SaveChangesAsync();
        return ToResponse(startup);
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(int id)
    {
        var startup = await _db.Startups.FindAsync(id);
        if (startup is null) return NotFound();

        if (!string.IsNullOrEmpty(startup.PitchDeckPath))
            _files.Delete(startup.PitchDeckPath);

        _db.Startups.Remove(startup);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private async Task<bool> CanAccessStartupAsync(Startup startup)
    {
        if (IsAdmin || IsMentor) return true;
        return startup.UserId == CurrentUserId;
    }

    [HttpPost("{id}/pitch-deck")]
    [Authorize(Policy = "FounderOnly")]
    [RequestSizeLimit(20_971_520)]
    public async Task<ActionResult<PitchDeckInfo>> UploadPitchDeck(int id, IFormFile file)
    {
        var startup = await _db.Startups.FindAsync(id);
        if (startup is null) return NotFound();

        if (startup.UserId != CurrentUserId)
            return Forbid();

        if (file is null || file.Length == 0)
            return BadRequest("No file was uploaded.");

        if (file.Length > _fileSettings.MaxFileSizeBytes)
            return BadRequest($"File exceeds the maximum size of {_fileSettings.MaxFileSizeBytes / 1024 / 1024} MB.");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedPitchDeckExtensions.Contains(extension))
            return BadRequest($"Only {string.Join(", ", AllowedPitchDeckExtensions)} files are allowed.");

        if (!string.IsNullOrEmpty(startup.PitchDeckPath))
            _files.Delete(startup.PitchDeckPath);

        var savedPath = await _files.SaveAsync(file, "pitch-decks");

        startup.PitchDeckPath = savedPath;
        startup.PitchDeckOriginalFileName = file.FileName;
        startup.PitchDeckUploadedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new PitchDeckInfo(file.FileName, startup.PitchDeckUploadedAt.Value));
    }

    [HttpGet("{id}/pitch-deck")]
    public async Task<IActionResult> DownloadPitchDeck(int id)
    {
        var startup = await _db.Startups.FindAsync(id);
        if (startup is null) return NotFound();

        if (!await CanAccessStartupAsync(startup))
            return Forbid();

        if (string.IsNullOrEmpty(startup.PitchDeckPath))
            return NotFound("This startup has no pitch deck uploaded yet.");

        try
        {
            var stream = _files.OpenRead(startup.PitchDeckPath);
            var contentType = Path.GetExtension(startup.PitchDeckPath).ToLowerInvariant() switch
            {
                ".pdf" => "application/pdf",
                ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                _ => "application/octet-stream"
            };

            return File(stream, contentType, startup.PitchDeckOriginalFileName ?? "pitch-deck");
        }
        catch (FileNotFoundException)
        {
            return NotFound("File missing on server.");
        }
        catch (DirectoryNotFoundException)
        {
            return NotFound("File missing on server.");
        }
    }
}
