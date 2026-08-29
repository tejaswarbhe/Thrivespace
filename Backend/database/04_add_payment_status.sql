-- Add PaymentStatus to FundingRequests table
USE startupims_core;
ALTER TABLE FundingRequests
    ADD COLUMN PaymentStatus VARCHAR(20) NULL AFTER ApprovalStatus;
