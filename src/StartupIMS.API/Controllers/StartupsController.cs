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
public class StartupsController : ControllerBase
{
    private readonly CoreDbContext _db;

    public StartupsController(CoreDbContext db)
    {
        _db = db;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private bool IsAdmin => User.IsInRole("Admin");
    private bool IsMentor => User.IsInRole("Mentor");

    // Admin: all startups. Mentor: only startups assigned to them. Founder: only their own.
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Startup>>> GetAll()
    {
        if (IsAdmin)
            return await _db.Startups.ToListAsync();

        if (IsMentor)
        {
            var mentor = await _db.Mentors.SingleOrDefaultAsync(m => m.UserId == CurrentUserId);
            if (mentor is null) return Ok(Array.Empty<Startup>());

            var startupIds = await _db.Mentorassignments
                .Where(ma => ma.MentorId == mentor.Id)
                .Select(ma => ma.StartupId)
                .ToListAsync();

            return await _db.Startups.Where(s => startupIds.Contains(s.Id)).ToListAsync();
        }

        // Founder: only their own startup
        return await _db.Startups.Where(s => s.UserId == CurrentUserId).ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Startup>> GetById(int id)
    {
        var startup = await _db.Startups.FindAsync(id);
        if (startup is null) return NotFound();

        if (!IsAdmin && !IsMentor && startup.UserId != CurrentUserId)
            return Forbid();

        return startup;
    }

    [HttpPost]
    [Authorize(Policy = "FounderOnly")]
    public async Task<ActionResult<Startup>> Create(CreateStartupRequest request)
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
        return CreatedAtAction(nameof(GetById), new { id = startup.Id }, startup);
    }
}
