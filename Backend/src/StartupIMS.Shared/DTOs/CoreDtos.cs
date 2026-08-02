namespace StartupIMS.Shared.DTOs;

// --- Applications ---
public record CreateApplicationRequest(string? Remarks);

public record UpdateApplicationStatusRequest(string Status); // "Submitted" | "UnderReview" | "Accepted" | "Rejected"

// --- Progress Reports ---
public record CreateProgressReportRequest(string Milestones, string? Remarks);

// --- Funding ---
public record CreateFundingRequest(decimal Amount, string FundingType);

public record UpdateFundingApprovalRequest(string ApprovalStatus, string? TransactionReference); // "Approved" | "Rejected" | "Disbursed"
