using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StartupIMS.Infrastructure.Persistence;
using StartupIMS.Infrastructure.Persistence.Scaffolded.Core;
using StartupIMS.Infrastructure.Services;
using StartupIMS.Shared.DTOs;
using StartupIMS.Shared.Settings;

namespace StartupIMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FundingController : ControllerBase
{
    private static readonly string[] ValidApprovalStatuses = { "Approved", "Rejected", "Disbursed" };

    private readonly CoreDbContext _db;
    private readonly IStartupVisibilityService _visibility;
    private readonly IdentityDbContext _identityDb;
    private readonly IEmailService _email;
    private readonly IPaymentServiceClient _paymentClient;
    private readonly PaymentServiceSettings _paymentSettings;
    private readonly ILogger<FundingController> _logger;

    public FundingController(
        CoreDbContext db,
        IStartupVisibilityService visibility,
        IdentityDbContext identityDb,
        IEmailService email,
        IPaymentServiceClient paymentClient,
        IOptions<PaymentServiceSettings> paymentSettings,
        ILogger<FundingController> logger)
    {
        _db = db;
        _visibility = visibility;
        _identityDb = identityDb;
        _email = email;
        _paymentClient = paymentClient;
        _paymentSettings = paymentSettings.Value;
        _logger = logger;
    }

    private async Task<string?> GetFounderEmailAsync(int startupId)
    {
        var startup = await _db.Startups.FindAsync(startupId);
        if (startup is null) return null;

        var founder = await _identityDb.Users.FindAsync(startup.UserId);
        return founder?.Email;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private bool IsAdmin => User.IsInRole("Admin");
    private bool IsMentor => User.IsInRole("Mentor");

    private static FundingResponse ToResponse(Fundingrequest f) => new(
        f.Id, f.StartupId, f.Amount, f.FundingType, f.ApprovalStatus, f.PaymentStatus,
        f.ApprovedByUserId, f.ApprovedDate, f.TransactionReference, f.PaymentUrl);

    [HttpGet]
    public async Task<ActionResult<IEnumerable<FundingResponse>>> GetAll()
    {
        List<Fundingrequest> requests;

        if (IsAdmin)
        {
            requests = await _db.Fundingrequests.ToListAsync();
        }
        else
        {
            var startupIds = await _visibility.GetVisibleStartupIdsAsync(CurrentUserId, IsMentor);
            requests = await _db.Fundingrequests.Where(f => startupIds.Contains(f.StartupId)).ToListAsync();
        }

        return requests.Select(ToResponse).ToList();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<FundingResponse>> GetById(int id)
    {
        var funding = await _db.Fundingrequests.FindAsync(id);
        if (funding is null) return NotFound();

        if (!IsAdmin)
        {
            var visibleIds = await _visibility.GetVisibleStartupIdsAsync(CurrentUserId, IsMentor);
            if (!visibleIds.Contains(funding.StartupId)) return Forbid();
        }

        return ToResponse(funding);
    }

    [HttpPost]
    [Authorize(Policy = "FounderOnly")]
    public async Task<ActionResult<FundingResponse>> Create(CreateFundingRequest request)
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

        var founderEmail = await GetFounderEmailAsync(startup.Id);
        if (founderEmail is not null)
        {
            await _email.SendAsync(
                founderEmail,
                "Funding request submitted",
                $"""
                <h2>Funding request received</h2>
                <p>Your request for <strong>${request.Amount:N2}</strong> ({request.FundingType}) has been submitted and is now <strong>Pending</strong> review.</p>
                <p>We'll email you again once a decision has been made.</p>
                """);
        }

        return CreatedAtAction(nameof(GetById), new { id = funding.Id }, ToResponse(funding));
    }

    [HttpPut("{id}/approval")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<FundingResponse>> UpdateApproval(int id, UpdateFundingApprovalRequest request)
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

        // Approval hands off to the payment microservice to actually move
        // money.
        if (request.ApprovalStatus == "Approved")
        {
            try
            {
                var paymentSession = await _paymentClient.InitiatePaymentAsync(funding.Id, funding.Amount, "USD");
                funding.PaymentUrl = paymentSession.PaymentUrl;
                funding.PaymentStatus = "PENDING";
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initiate payment with payment service for FundingRequest {Id}", funding.Id);
                return StatusCode(502, "Approval recorded, but failed to initiate payment gateway link.");
            }
        }

        var founderEmail = await GetFounderEmailAsync(funding.StartupId);
        if (founderEmail is not null)
        {
            await _email.SendAsync(
                founderEmail,
                $"Funding request {request.ApprovalStatus.ToLowerInvariant()}",
                $"""
                <h2>Funding decision update</h2>
                <p>Your request for <strong>${funding.Amount:N2}</strong> ({funding.FundingType}) has been marked as <strong>{request.ApprovalStatus}</strong>.</p>
                {(request.TransactionReference is not null ? $"<p>Transaction reference: {request.TransactionReference}</p>" : "")}
                """);
        }

        return ToResponse(funding);
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "FounderOnly")]
    public async Task<IActionResult> Cancel(int id)
    {
        var funding = await _db.Fundingrequests.FindAsync(id);
        if (funding is null) return NotFound();

        var startup = await _db.Startups.FindAsync(funding.StartupId);
        if (startup is null || startup.UserId != CurrentUserId)
            return Forbid();

        if (funding.ApprovalStatus != "Pending")
            return BadRequest("Only funding requests still 'Pending' can be cancelled.");

        _db.Fundingrequests.Remove(funding);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // Called by the Java payment microservice, not a logged-in user - so this
    // deliberately bypasses the class-level [Authorize] JWT requirement and
    // validates a shared static key instead. A JWT represents a human
    // session; this call has no human behind it.
    [HttpPost("payment-webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> PaymentWebhook(FundingPaymentWebhookRequest request)
    {
        if (!Request.Headers.TryGetValue("X-Service-Key", out var providedKey) ||
            providedKey != _paymentSettings.InboundApiKey ||
            string.IsNullOrEmpty(_paymentSettings.InboundApiKey))
        {
            return Unauthorized();
        }

        var funding = await _db.Fundingrequests.FindAsync(request.FundingRequestId);
        if (funding is null) return NotFound();

        // Idempotency check: don't process if already in a final state
        if (funding.PaymentStatus == request.Status && (request.Status == "SUCCESS" || request.Status == "FAILED"))
        {
            _logger.LogInformation("Webhook idempotency: FundingRequest {Id} is already {Status}", funding.Id, request.Status);
            return Ok();
        }

        funding.PaymentStatus = request.Status;
        if (!string.IsNullOrEmpty(request.TransactionReference))
        {
            funding.TransactionReference = request.TransactionReference;
        }
        await _db.SaveChangesAsync();

        _logger.LogInformation("Payment webhook processed for FundingRequest {Id}: {Status}", funding.Id, request.Status);

        var founderEmail = await GetFounderEmailAsync(funding.StartupId);
        if (founderEmail is not null)
        {
            string emailSubject = request.Status == "SUCCESS" ? "Payment Successful - StartupIMS" : "Payment Failed - StartupIMS";
            await _email.SendAsync(
                founderEmail,
                emailSubject,
                $"""
                <h2>Payment update</h2>
                <p>Your funding payment of <strong>${funding.Amount:N2}</strong> has been marked as <strong>{request.Status}</strong>.</p>
                {(request.TransactionReference is not null ? $"<p>Transaction reference: {request.TransactionReference}</p>" : "")}
                """);
        }

        return Ok();
    }
}
