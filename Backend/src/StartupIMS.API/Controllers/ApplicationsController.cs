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
public class ApplicationsController : ControllerBase
{
    private static readonly string[] ValidStatuses = { "Submitted", "UnderReview", "Accepted", "Rejected" };

    private readonly CoreDbContext _db;

    public ApplicationsController(CoreDbContext db)
    {
        _db = db;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private bool IsAdmin => User.IsInRole("Admin");
    private bool IsMentor => User.IsInRole("Mentor");

    // Same visibility rule as StartupsController: Mentor -> assigned startups, Founder -> own startup
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
    public async Task<ActionResult<IEnumerable<Incubationapplication>>> GetAll()
    {
        if (IsAdmin)
            return await _db.Incubationapplications.ToListAsync();

        var startupIds = await GetVisibleStartupIdsAsync();
        return await _db.Incubationapplications
            .Where(a => startupIds.Contains(a.StartupId))
            .ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Incubationapplication>> GetById(int id)
    {
        var application = await _db.Incubationapplications.FindAsync(id);
        if (application is null) return NotFound();

        if (!IsAdmin)
        {
            var visibleIds = await GetVisibleStartupIdsAsync();
            if (!visibleIds.Contains(application.StartupId)) return Forbid();
        }

        return application;
    }

    [HttpPost]
    [Authorize(Policy = "FounderOnly")]
    public async Task<ActionResult<Incubationapplication>> Create(CreateApplicationRequest request)
    {
        var startup = await _db.Startups.SingleOrDefaultAsync(s => s.UserId == CurrentUserId);
        if (startup is null)
            return BadRequest("You must create a startup before submitting an application.");

        var application = new Incubationapplication
        {
            StartupId = startup.Id,
            SubmissionDate = DateTime.UtcNow,
            Status = "Submitted",
            Remarks = request.Remarks
        };

        _db.Incubationapplications.Add(application);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = application.Id }, application);
    }

    [HttpPut("{id}/status")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<Incubationapplication>> UpdateStatus(int id, UpdateApplicationStatusRequest request)
    {
        if (!ValidStatuses.Contains(request.Status))
            return BadRequest($"Status must be one of: {string.Join(", ", ValidStatuses)}.");

        var application = await _db.Incubationapplications.FindAsync(id);
        if (application is null) return NotFound();

        application.Status = request.Status;
        await _db.SaveChangesAsync();
        return application;
    }
}
