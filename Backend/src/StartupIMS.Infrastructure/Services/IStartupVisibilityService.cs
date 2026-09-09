namespace StartupIMS.Infrastructure.Services;

/// <summary>
/// Centralizes the "which startup ids can this Mentor or Founder see" rule -
/// previously copy-pasted identically across StartupsController,
/// ApplicationsController, ProgressReportsController, and FundingController.
/// Admin's "see everything" case is NOT handled here - each controller still
/// checks IsAdmin itself and skips this entirely, since "no filter" isn't a
/// meaningful "list of visible ids."
/// </summary>
public interface IStartupVisibilityService
{
    /// <param name="currentUserId">The caller's user id, from their JWT claim.</param>
    /// <param name="isMentor">True if the caller's role is Mentor; false means Founder.</param>
    Task<List<int>> GetVisibleStartupIdsAsync(int currentUserId, bool isMentor);
}
