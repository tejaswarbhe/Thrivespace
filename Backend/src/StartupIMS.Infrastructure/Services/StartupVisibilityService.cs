using Microsoft.EntityFrameworkCore;
using StartupIMS.Infrastructure.Persistence;

namespace StartupIMS.Infrastructure.Services;

public class StartupVisibilityService : IStartupVisibilityService
{
    private readonly CoreDbContext _db;

    public StartupVisibilityService(CoreDbContext db)
    {
        _db = db;
    }

    public async Task<List<int>> GetVisibleStartupIdsAsync(int currentUserId, bool isMentor)
    {
        if (isMentor)
        {
            var mentor = await _db.Mentors.SingleOrDefaultAsync(m => m.UserId == currentUserId);
            if (mentor is null) return new List<int>();

            return await _db.Mentorassignments
                .Where(ma => ma.MentorId == mentor.Id && ma.Status == "Active")
                .Select(ma => ma.StartupId)
                .ToListAsync();
        }

        // Founder
        return await _db.Startups
            .Where(s => s.UserId == currentUserId)
            .Select(s => s.Id)
            .ToListAsync();
    }
}
