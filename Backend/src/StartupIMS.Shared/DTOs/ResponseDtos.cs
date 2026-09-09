namespace StartupIMS.Shared.DTOs;

public record StartupResponse(
    int Id,
    int UserId,
    string Name,
    string Domain,
    string? Description,
    DateOnly FoundingDate,
    string Status,
    string? PitchDeckOriginalFileName,
    DateTime? PitchDeckUploadedAt
);

public record ApplicationResponse(
    int Id,
    int StartupId,
    DateTime SubmissionDate,
    string Status,
    string? Remarks
);

public record ProgressReportResponse(
    int Id,
    int StartupId,
    int? CreatedByMentorId,
    DateTime SubmissionDate,
    string Milestones,
    bool IsCompleted,
    DateTime? CompletedDate,
    string? Remarks
);

public record FundingResponse(
    int Id,
    int StartupId,
    decimal Amount,
    string FundingType,
    string ApprovalStatus,
    string? PaymentStatus,
    int? ApprovedByUserId,
    DateTime? ApprovedDate,
    string? TransactionReference,
    string? PaymentUrl
);

public record MentorResponse(
    int Id,
    int UserId,
    string Name,
    string Expertise,
    int ExperienceYears,
    string Organization
);

public record MentorAssignmentResponse(
    int Id,
    int StartupId,
    int MentorId,
    DateTime AssignedDate,
    string Status
);
