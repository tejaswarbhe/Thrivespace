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
public class MentorsController : ControllerBase
{
    private readonly CoreDbContext _db;
    private readonly IdentityDbContext _identityDb;

    public MentorsController(CoreDbContext db, IdentityDbContext identityDb)
    {
        _db = db;
        _identityDb = identityDb;
    }

    private static MentorAssignmentResponse ToResponse(Mentorassignment a) => new(
        a.Id, a.StartupId, a.MentorId, a.AssignedDate, a.Status);

    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<IEnumerable<MentorResponse>>> GetAll()
    {
        var mentors = await _db.Mentors.ToListAsync();
        var userIds = mentors.Select(m => m.UserId).ToList();
        var names = await _identityDb.Users
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Name);

        return mentors.Select(m => new MentorResponse(m.Id, m.UserId, names.GetValueOrDefault(m.UserId, "unknown"),
            m.Expertise, m.ExperienceYears, m.Organization)).ToList();
    }

    // Lets the frontend know which startup<->mentor pairs are already active,
    // so the assignment dropdown can filter them out instead of allowing a
    // duplicate/blind reassignment.
    [HttpGet("assignments")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<IEnumerable<MentorAssignmentResponse>>> GetAssignments()
    {
        var assignments = await _db.Mentorassignments.Where(a => a.Status == "Active").ToListAsync();
        return assignments.Select(ToResponse).ToList();
    }

    [HttpPost("assign")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<MentorAssignmentResponse>> AssignMentor(int startupId, int mentorId)
    {
        var startupExists = await _db.Startups.AnyAsync(s => s.Id == startupId);
        if (!startupExists) return BadRequest($"No startup with id {startupId}.");

        var mentorExists = await _db.Mentors.AnyAsync(m => m.Id == mentorId);
        if (!mentorExists) return BadRequest($"No mentor with id {mentorId}.");

        var alreadyActive = await _db.Mentorassignments.AnyAsync(a =>
            a.StartupId == startupId && a.MentorId == mentorId && a.Status == "Active");
        if (alreadyActive)
            return Conflict("This mentor is already actively assigned to this startup.");

        var assignment = new Mentorassignment
        {
            StartupId = startupId,
            MentorId = mentorId,
            AssignedDate = DateTime.UtcNow,
            Status = "Active"
        };
        _db.Mentorassignments.Add(assignment);
        await _db.SaveChangesAsync();
        return Ok(ToResponse(assignment));
    }

    // Ends an active mentorship - soft delete (Status -> Completed) rather
    // than removing the row, so the assignment history is preserved.
    [HttpDelete("assignments/{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<MentorAssignmentResponse>> Unassign(int id)
    {
        var assignment = await _db.Mentorassignments.FindAsync(id);
        if (assignment is null) return NotFound();

        if (assignment.Status != "Active")
            return BadRequest("This assignment is not currently active.");

        assignment.Status = "Completed";
        await _db.SaveChangesAsync();
        return ToResponse(assignment);
    }
}
