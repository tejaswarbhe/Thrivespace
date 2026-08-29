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
public class ApplicationsController : ControllerBase
{
    private static readonly string[] ValidStatuses = { "Submitted", "UnderReview", "Accepted", "Rejected" };

    private readonly CoreDbContext _db;
    private readonly IStartupVisibilityService _visibility;

    public ApplicationsController(CoreDbContext db, IStartupVisibilityService visibility)
    {
        _db = db;
        _visibility = visibility;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private bool IsAdmin => User.IsInRole("Admin");
    private bool IsMentor => User.IsInRole("Mentor");

    private static ApplicationResponse ToResponse(Incubationapplication a) => new(
        a.Id, a.StartupId, a.SubmissionDate, a.Status, a.Remarks);

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ApplicationResponse>>> GetAll()
    {
        List<Incubationapplication> apps;

        if (IsAdmin)
        {
            apps = await _db.Incubationapplications.ToListAsync();
        }
        else
        {
            var startupIds = await _visibility.GetVisibleStartupIdsAsync(CurrentUserId, IsMentor);
            apps = await _db.Incubationapplications.Where(a => startupIds.Contains(a.StartupId)).ToListAsync();
        }

        return apps.Select(ToResponse).ToList();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApplicationResponse>> GetById(int id)
    {
        var application = await _db.Incubationapplications.FindAsync(id);
        if (application is null) return NotFound();

        if (!IsAdmin)
        {
            var visibleIds = await _visibility.GetVisibleStartupIdsAsync(CurrentUserId, IsMentor);
            if (!visibleIds.Contains(application.StartupId)) return Forbid();
        }

        return ToResponse(application);
    }

    [HttpPost]
    [Authorize(Policy = "FounderOnly")]
    public async Task<ActionResult<ApplicationResponse>> Create(CreateApplicationRequest request)
    {
        var startup = await _db.Startups.SingleOrDefaultAsync(s => s.UserId == CurrentUserId);
        if (startup is null)
            return BadRequest("You must create a startup before submitting an application.");

        var HasOpenApplication = await _db.Incubationapplications
            .Where(a => a.StartupId == startup.Id)
            .AnyAsync(a => a.Status != "Rejected");
        if (HasOpenApplication)
            return Conflict("You have already submitted your application.You can re-apply if Rejected");

        var application = new Incubationapplication
        {
            StartupId = startup.Id,
            SubmissionDate = DateTime.UtcNow,
            Status = "Submitted",
            Remarks = request.Remarks
        };

        _db.Incubationapplications.Add(application);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = application.Id }, ToResponse(application));
    }

    [HttpPut("{id}/status")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApplicationResponse>> UpdateStatus(int id, UpdateApplicationStatusRequest request)
    {
        if (!ValidStatuses.Contains(request.Status))
            return BadRequest($"Status must be one of: {string.Join(", ", ValidStatuses)}.");

        var application = await _db.Incubationapplications.FindAsync(id);
        if (application is null) return NotFound();

        application.Status = request.Status;
        await _db.SaveChangesAsync();
        return ToResponse(application);
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "FounderOnly")]
    public async Task<IActionResult> Withdraw(int id)
    {
        var application = await _db.Incubationapplications.FindAsync(id);
        if (application is null) return NotFound();

        var startup = await _db.Startups.FindAsync(application.StartupId);
        if (startup is null || startup.UserId != CurrentUserId)
            return Forbid();

        if (application.Status != "Submitted")
            return BadRequest("Only applications still in 'Submitted' status can be withdrawn.");

        _db.Incubationapplications.Remove(application);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
