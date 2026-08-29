using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StartupIMS.Infrastructure.Persistence;
using StartupIMS.Infrastructure.Persistence.Scaffolded.Core;
using StartupIMS.Infrastructure.Services;
using StartupIMS.Shared.DTOs;

namespace StartupIMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProgressReportsController : ControllerBase
{
    private readonly CoreDbContext _db;
    private readonly IStartupVisibilityService _visibility;

    public ProgressReportsController(CoreDbContext db, IStartupVisibilityService visibility)
    {
        _db = db;
        _visibility = visibility;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private bool IsAdmin => User.IsInRole("Admin");
    private bool IsMentor => User.IsInRole("Mentor");

    private static ProgressReportResponse ToResponse(Progressreport p) => new(
    p.Id, p.StartupId, p.CreatedByMentorId, p.SubmissionDate, p.Milestones, p.IsCompleted, p.CompletedDate, p.Remarks);

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProgressReportResponse>>> GetAll()
    {
        List<Progressreport> reports;

        if (IsAdmin)
        {
            reports = await _db.Progressreports.ToListAsync();
        }
        else
        {
            var startupIds = await _visibility.GetVisibleStartupIdsAsync(CurrentUserId, IsMentor);
            reports = await _db.Progressreports.Where(p => startupIds.Contains(p.StartupId)).ToListAsync();
        }

        return reports.Select(ToResponse).ToList();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProgressReportResponse>> GetById(int id)
    {
        var report = await _db.Progressreports.FindAsync(id);
        if (report is null) return NotFound();

        if (!IsAdmin)
        {
            var visibleIds = await _visibility.GetVisibleStartupIdsAsync(CurrentUserId, IsMentor);
            if (!visibleIds.Contains(report.StartupId)) return Forbid();
        }

        return ToResponse(report);
    }

    [HttpPost]
    [Authorize(Policy = "MentorOnly")]
    public async Task<ActionResult<ProgressReportResponse>> Create(CreateProgressReportRequest request)
    {
        var mentor = await _db.Mentors.SingleOrDefaultAsync(m => m.UserId == CurrentUserId);
        if (mentor is null) return Forbid();

        var visibleIds = await _visibility.GetVisibleStartupIdsAsync(CurrentUserId, isMentor: true);
        if (!visibleIds.Contains(request.StartupId))
            return Forbid(); // not your assigned startup

        var report = new Progressreport
        {
            StartupId = request.StartupId,
            CreatedByMentorId = mentor.Id,
            SubmissionDate = DateTime.UtcNow,
            Milestones = request.Milestones,
            IsCompleted = false
        };

        _db.Progressreports.Add(report);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = report.Id }, ToResponse(report));
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "FounderOnly")]
    public async Task<ActionResult<ProgressReportResponse>> Update(int id, UpdateProgressReportRequest request)
    {
        var report = await _db.Progressreports.FindAsync(id);
        if (report is null) return NotFound();

        var startup = await _db.Startups.FindAsync(report.StartupId);
        if (startup is null || startup.UserId != CurrentUserId)
            return Forbid();

        report.IsCompleted = request.IsCompleted;
        report.CompletedDate = request.IsCompleted ? DateTime.UtcNow : null;
        report.Remarks = request.Remarks;
        await _db.SaveChangesAsync();
        return ToResponse(report);
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "MentorOnly")]
    public async Task<IActionResult> Delete(int id)
    {
        var report = await _db.Progressreports.FindAsync(id);
        if (report is null) return NotFound();

        var mentor = await _db.Mentors.SingleOrDefaultAsync(m => m.UserId == CurrentUserId);
        if (mentor is null || report.CreatedByMentorId != mentor.Id)
            return Forbid();

        _db.Progressreports.Remove(report);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
