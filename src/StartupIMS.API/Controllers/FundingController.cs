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
public class FundingController : ControllerBase
{
    private static readonly string[] ValidApprovalStatuses = { "Approved", "Rejected", "Disbursed" };

    private readonly CoreDbContext _db;

    public FundingController(CoreDbContext db)
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
    public async Task<ActionResult<IEnumerable<Fundingrequest>>> GetAll()
    {
        if (IsAdmin)
            return await _db.Fundingrequests.ToListAsync();

        var startupIds = await GetVisibleStartupIdsAsync();
        return await _db.Fundingrequests
            .Where(f => startupIds.Contains(f.StartupId))
            .ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Fundingrequest>> GetById(int id)
    {
        var funding = await _db.Fundingrequests.FindAsync(id);
        if (funding is null) return NotFound();

        if (!IsAdmin)
        {
            var visibleIds = await GetVisibleStartupIdsAsync();
            if (!visibleIds.Contains(funding.StartupId)) return Forbid();
        }

        return funding;
    }

    [HttpPost]
    [Authorize(Policy = "FounderOnly")]
    public async Task<ActionResult<Fundingrequest>> Create(CreateFundingRequest request)
    {
        var startup = await _db.Startups.SingleOrDefaultAsync(s => s.UserId == CurrentUserId);
        if (startup is null)
            return BadRequest("You must create a startup before requesting funding.");

        var funding = new Fundingrequest
        {
            StartupId = startup.Id,
            Amount = request.Amount,
            FundingType = request.FundingType,
            ApprovalStatus = "Pending"
        };

        _db.Fundingrequests.Add(funding);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = funding.Id }, funding);
    }

    [HttpPut("{id}/approval")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<Fundingrequest>> UpdateApproval(int id, UpdateFundingApprovalRequest request)
    {
        if (!ValidApprovalStatuses.Contains(request.ApprovalStatus))
            return BadRequest($"ApprovalStatus must be one of: {string.Join(", ", ValidApprovalStatuses)}.");

        var funding = await _db.Fundingrequests.FindAsync(id);
        if (funding is null) return NotFound();

        funding.ApprovalStatus = request.ApprovalStatus;
        funding.ApprovedByUserId = CurrentUserId;
        funding.ApprovedDate = DateTime.UtcNow;
        if (request.TransactionReference is not null)
            funding.TransactionReference = request.TransactionReference;

        await _db.SaveChangesAsync();
        return funding;
    }
}
