namespace StartupIMS.Domain.Enums;

public enum ApplicationStatus
{
    Submitted = 0,
    UnderReview = 1,
    Accepted = 2,
    Rejected = 3
}

public enum StartupStatus
{
    Applied = 0,
    Incubating = 1,
    Graduated = 2,
    Dropped = 3
}

public enum MentorAssignmentStatus
{
    Active = 0,
    Completed = 1,
    Cancelled = 2
}

public enum FundingApprovalStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    Disbursed = 3
}
