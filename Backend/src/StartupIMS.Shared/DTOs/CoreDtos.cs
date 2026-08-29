using System.ComponentModel.DataAnnotations;

namespace StartupIMS.Shared.DTOs;

// --- Applications ---
public record CreateApplicationRequest([StringLength(2000)] string? Remarks);

public record UpdateApplicationStatusRequest([Required] string Status); // "Submitted" | "UnderReview" | "Accepted" | "Rejected"

// --- Progress Reports ---
public record CreateProgressReportRequest(
    [Required] int StartupId,
    [Required, StringLength(4000, MinimumLength = 3)] string Milestones
);

public record UpdateProgressReportRequest(
    [Required] bool IsCompleted,
    [StringLength(2000)] string? Remarks
);

// --- Funding ---
public record CreateFundingRequest(
    [Required, Range(0.01, 100_000_000)] decimal Amount,
    [Required, StringLength(50)] string FundingType
);

public record UpdateFundingApprovalRequest(
    [Required] string ApprovalStatus, // "Approved" | "Rejected" | "Disbursed"
    [StringLength(200)] string? TransactionReference
);
