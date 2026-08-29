using Microsoft.EntityFrameworkCore;
using StartupIMS.Infrastructure.Persistence;
using StartupIMS.Infrastructure.Persistence.Scaffolded.Core;
using StartupIMS.Infrastructure.Services;
using Xunit;

namespace StartupIMS.Tests;

public class StartupVisibilityServiceTests
{
    private static CoreDbContext CreateInMemoryDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<CoreDbContext>()
            .UseInMemoryDatabase(dbName) // unique name per test = isolated data
            .Options;
        return new CoreDbContext(options);
    }

    [Fact]
    public async Task Founder_SeesOnlyTheirOwnStartup()
    {
        await using var db = CreateInMemoryDb(nameof(Founder_SeesOnlyTheirOwnStartup));

        db.Startups.AddRange(
            new Startup { Id = 1, UserId = 100, Name = "Mine", Domain = "Tech", FoundingDate = DateOnly.FromDateTime(DateTime.Today), Status = "Applied" },
            new Startup { Id = 2, UserId = 200, Name = "Someone Else's", Domain = "Tech", FoundingDate = DateOnly.FromDateTime(DateTime.Today), Status = "Applied" }
        );
        await db.SaveChangesAsync();

        var service = new StartupVisibilityService(db);
        var visibleIds = await service.GetVisibleStartupIdsAsync(currentUserId: 100, isMentor: false);

        var id = Assert.Single(visibleIds);
        Assert.Equal(1, id);
    }

    [Fact]
    public async Task Mentor_SeesOnlyActivelyAssignedStartups()
    {
        await using var db = CreateInMemoryDb(nameof(Mentor_SeesOnlyActivelyAssignedStartups));

        db.Startups.AddRange(
            new Startup { Id = 1, UserId = 100, Name = "Assigned", Domain = "Tech", FoundingDate = DateOnly.FromDateTime(DateTime.Today), Status = "Applied" },
            new Startup { Id = 2, UserId = 200, Name = "NotAssigned", Domain = "Tech", FoundingDate = DateOnly.FromDateTime(DateTime.Today), Status = "Applied" },
            new Startup { Id = 3, UserId = 300, Name = "CompletedAssignment", Domain = "Tech", FoundingDate = DateOnly.FromDateTime(DateTime.Today), Status = "Applied" }
        );
        db.Mentors.Add(new Mentor { Id = 1, UserId = 999, Expertise = "Fundraising", ExperienceYears = 5, Organization = "Indie" });
        db.Mentorassignments.AddRange(
            new Mentorassignment { Id = 1, StartupId = 1, MentorId = 1, Status = "Active" },
            // A completed (not active) assignment should NOT count as currently visible
            new Mentorassignment { Id = 2, StartupId = 3, MentorId = 1, Status = "Completed" }
        );
        await db.SaveChangesAsync();

        var service = new StartupVisibilityService(db);
        var visibleIds = await service.GetVisibleStartupIdsAsync(currentUserId: 999, isMentor: true);

        var id = Assert.Single(visibleIds);
        Assert.Equal(1, id); // only the Active assignment's startup - not the Completed one, not the unassigned one
    }

    [Fact]
    public async Task Mentor_WithNoProfileRow_SeesNothing_RatherThanThrowing()
    {
        // Guards against a null-reference crash if a Mentor role user somehow
        // has no Mentor profile row yet (e.g. a data inconsistency) - should
        // degrade to "sees nothing," not a 500 error.
        await using var db = CreateInMemoryDb(nameof(Mentor_WithNoProfileRow_SeesNothing_RatherThanThrowing));

        var service = new StartupVisibilityService(db);
        var visibleIds = await service.GetVisibleStartupIdsAsync(currentUserId: 999, isMentor: true);

        Assert.Empty(visibleIds);
    }

    [Fact]
    public async Task Founder_WithNoStartup_SeesEmptyList_RatherThanThrowing()
    {
        await using var db = CreateInMemoryDb(nameof(Founder_WithNoStartup_SeesEmptyList_RatherThanThrowing));

        var service = new StartupVisibilityService(db);
        var visibleIds = await service.GetVisibleStartupIdsAsync(currentUserId: 100, isMentor: false);

        Assert.Empty(visibleIds);
    }
}
