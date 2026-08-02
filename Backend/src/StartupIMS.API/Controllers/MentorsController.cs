using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StartupIMS.Infrastructure.Persistence;
using StartupIMS.Infrastructure.Persistence.Scaffolded.Core;

namespace StartupIMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MentorsController : ControllerBase
{
    private readonly CoreDbContext _db;

    public MentorsController(CoreDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<IEnumerable<Mentor>>> GetAll() =>
        await _db.Mentors.ToListAsync();

    [HttpPost("assign")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<Mentorassignment>> AssignMentor(int startupId, int mentorId)
    {
        var assignment = new Mentorassignment
        {
            StartupId = startupId,
            MentorId = mentorId,
            Status = "Active"
        };
        _db.Mentorassignments.Add(assignment);
        await _db.SaveChangesAsync();
        return Ok(assignment);
    }
}
