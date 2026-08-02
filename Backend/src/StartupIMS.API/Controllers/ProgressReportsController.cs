using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StartupIMS.Infrastructure.Persistence;
using StartupIMS.Infrastructure.Persistence.Scaffolded.Core;
using StartupIMS.Shared.DTOs;

namespace StartupIMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProgressReportsController : ControllerBase
{
    private readonly CoreDbContext _db;

    public ProgressReportsController(CoreDbContext db)
    {
        _db = db;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private bool IsAdmin => User.IsInRole("Admin");
    private bool IsMentor => User.IsInRole("Mentor");

    private async Task<List<int>> GetVisibleStartupIdsAsync()
    {
        if (IsMentor)
        {
            var mentor = await _db.Mentors.SingleOrDefaultAsync(m => m.UserId == CurrentUserId);
            if (mentor is null) return new List<int>();

            return await _db.Mentorassignments
                .Where(ma => ma.MentorId == mentor.Id)
                .Select(ma => ma.StartupId)
                .ToListAsync();
        }

        return await _db.Startups
            .Where(s => s.UserId == CurrentUserId)
            .Select(s => s.Id)
            .ToListAsync();
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Progressreport>>> GetAll()
    {
        if (IsAdmin)
            return await _db.Progressreports.ToListAsync();

        var startupIds = await GetVisibleStartupIdsAsync();
        return await _db.Progressreports
            .Where(p => startupIds.Contains(p.StartupId))
            .ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Progressreport>> GetById(int id)
    {
        var report = await _db.Progressreports.FindAsync(id);
        if (report is null) return NotFound();

        if (!IsAdmin)
        {
            var visibleIds = await GetVisibleStartupIdsAsync();
            if (!visibleIds.Contains(report.StartupId)) return Forbid();
        }

        return report;
    }

    [HttpPost]
    [Authorize(Policy = "FounderOnly")]
    public async Task<ActionResult<Progressreport>> Create(CreateProgressReportRequest request)
    {
        var startup = await _db.Startups.SingleOrDefaultAsync(s => s.UserId == CurrentUserId);
        if (startup is null)
            return BadRequest("You must create a startup before submitting a progress report.");

        var report = new Progressreport
        {
            StartupId = startup.Id,
            SubmissionDate = DateTime.UtcNow,
            Milestones = request.Milestones,
            Remarks = request.Remarks
        };

        _db.Progressreports.Add(report);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = report.Id }, report);
    }
}
