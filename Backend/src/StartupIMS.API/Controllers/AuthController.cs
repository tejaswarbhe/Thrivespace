using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StartupIMS.Domain.Enums;
using StartupIMS.Infrastructure.Persistence;
using StartupIMS.Infrastructure.Persistence.Scaffolded.Identity;
using CoreEntities = StartupIMS.Infrastructure.Persistence.Scaffolded.Core;
using StartupIMS.Infrastructure.Services;
using StartupIMS.Shared.DTOs;
using StartupIMS.Shared.Settings;
using Microsoft.Extensions.Options;

namespace StartupIMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IdentityDbContext _db;

    // NOTE: reaching into CoreDbContext directly from the Identity module is a
    // pragmatic MVP shortcut, not the long-term pattern - it crosses the module
    // boundary we've been keeping clean everywhere else. When Identity becomes
    // its own microservice, this call needs to become an event
    // ("UserRegistered") that the Core service listens for instead.
    private readonly CoreDbContext _coreDb;

    private readonly IJwtTokenService _jwt;
    private readonly IEmailService _email;
    private readonly JwtSettings _jwtSettings;

    public AuthController(IdentityDbContext db, CoreDbContext coreDb, IJwtTokenService jwt, IEmailService email, IOptions<JwtSettings> jwtSettings)
    {
        _db = db;
        _coreDb = coreDb;
        _jwt = jwt;
        _email = email;
        _jwtSettings = jwtSettings.Value;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        if (await _db.Users.AnyAsync(u => u.Email == request.Email))
            return Conflict("A user with this email already exists.");

        if (request.Role == UserRole.Founder && string.IsNullOrWhiteSpace(request.StartupName))
            return BadRequest("StartupName (and Domain, FoundingDate) are required when registering as a Founder.");

        if (request.Role == UserRole.Mentor && string.IsNullOrWhiteSpace(request.MentorExpertise))
            return BadRequest("MentorExpertise is required when registering as a Mentor.");

        var user = new User
        {
            Name = request.Name,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = request.Role.ToString() // enum -> string for storage
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        if (request.Role == UserRole.Founder)
        {
            _coreDb.Startups.Add(new CoreEntities.Startup
            {
                UserId = user.Id,
                Name = request.StartupName!,
                Domain = request.StartupDomain ?? "",
                Description = request.StartupDescription,
                FoundingDate = request.StartupFoundingDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
                Status = "Applied"
            });
            await _coreDb.SaveChangesAsync();
        }
        else if (request.Role == UserRole.Mentor)
        {
            _coreDb.Mentors.Add(new CoreEntities.Mentor
            {
                UserId = user.Id,
                Expertise = request.MentorExpertise!,
                ExperienceYears = request.MentorExperienceYears ?? 0,
                Organization = request.MentorOrganization ?? ""
            });
            await _coreDb.SaveChangesAsync();
        }

        await _email.SendAsync(
            user.Email,
            "Welcome to StartupIMS",
            $"""
            <h2>Welcome, {user.Name}!</h2>
            <p>Your account has been created as a <strong>{user.Role}</strong>.</p>
            {(request.Role == UserRole.Founder
                ? $"<p>Your startup <strong>{request.StartupName}</strong> is now registered and ready to submit an incubation application.</p>"
                : "")}
            <p>You can now log in and get started.</p>
            """);

        return await IssueTokens(user);
    }
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var user = await _db.Users.SingleOrDefaultAsync(u => u.Email == request.Email);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized("Invalid email or password.");

        return await IssueTokens(user);
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh(RefreshRequest request)
    {
        var incomingHash = _jwt.HashToken(request.RefreshToken);

        var stored = await _db.Refreshtokens
            .Include(rt => rt.User)
            .SingleOrDefaultAsync(rt => rt.TokenHash == incomingHash);

        var isActive = stored is not null
            && stored.RevokedAt == null
            && DateTime.UtcNow < stored.ExpiresAt;

        if (!isActive)
            return Unauthorized("Refresh token is invalid or expired.");

        // Rotate: revoke the old one, issue a new pair
        stored!.RevokedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return await IssueTokens(stored.User);
    }

    private async Task<ActionResult<AuthResponse>> IssueTokens(User user)
    {
        var (accessToken, expiresAt) = _jwt.GenerateAccessToken(user);
        var rawRefreshToken = _jwt.GenerateRefreshToken();

        _db.Refreshtokens.Add(new Refreshtoken
        {
            UserId = user.Id,
            TokenHash = _jwt.HashToken(rawRefreshToken),
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenDays)
        });
        await _db.SaveChangesAsync();

        var roleEnum = Enum.Parse<UserRole>(user.Role); // string -> enum for the response
        return Ok(new AuthResponse(accessToken, rawRefreshToken, expiresAt, roleEnum, user.Id));
    }
}
