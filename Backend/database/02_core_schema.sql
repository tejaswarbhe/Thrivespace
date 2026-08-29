-- ============================================================
-- StartupIMS - Core module database
-- Run this against MySQL: CREATE DATABASE startupims_core;
-- Note: UserId columns here are FK-only references into the Identity
-- database's Users table (different DB/schema) - NOT enforced by a MySQL
-- foreign key, since cross-database FKs aren't supported. Integrity is
-- maintained at the application layer. This is intentional: it's the
-- seam we cut along when Identity becomes its own service later.
-- ============================================================

CREATE DATABASE IF NOT EXISTS startupims_core
    CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

USE startupims_core;

CREATE TABLE Startups (
    Id              INT AUTO_INCREMENT PRIMARY KEY,
    UserId          INT             NOT NULL,   -- references Identity.Users.Id (app-enforced)
    Name            VARCHAR(200)    NOT NULL,
    Domain          VARCHAR(100)    NOT NULL,
    Description     TEXT            NULL,
    FoundingDate    DATE            NOT NULL,
    Status          VARCHAR(20)     NOT NULL DEFAULT 'Applied',

    UNIQUE KEY UX_Startups_UserId (UserId) -- exactly one founder per startup
) ENGINE=InnoDB;

CREATE TABLE Mentors (
    Id              INT AUTO_INCREMENT PRIMARY KEY,
    UserId          INT             NOT NULL,   -- references Identity.Users.Id (app-enforced)
    Expertise       VARCHAR(200)    NOT NULL,
    ExperienceYears INT             NOT NULL DEFAULT 0,
    Organization    VARCHAR(200)    NOT NULL,

    UNIQUE KEY UX_Mentors_UserId (UserId) -- 1:1 with a User
) ENGINE=InnoDB;

CREATE TABLE MentorAssignments (
    Id              INT AUTO_INCREMENT PRIMARY KEY,
    StartupId       INT             NOT NULL,
    MentorId        INT             NOT NULL,
    AssignedDate    DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    Status          VARCHAR(20)     NOT NULL DEFAULT 'Active',

    CONSTRAINT FK_MentorAssignments_Startups
        FOREIGN KEY (StartupId) REFERENCES Startups(Id)
        ON DELETE CASCADE,

    CONSTRAINT FK_MentorAssignments_Mentors
        FOREIGN KEY (MentorId) REFERENCES Mentors(Id)
        ON DELETE RESTRICT,

    KEY IX_MentorAssignments_StartupId (StartupId),
    KEY IX_MentorAssignments_MentorId (MentorId)
) ENGINE=InnoDB;

CREATE TABLE IncubationApplications (
    Id              INT AUTO_INCREMENT PRIMARY KEY,
    StartupId       INT             NOT NULL,
    SubmissionDate  DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    Status          VARCHAR(20)     NOT NULL DEFAULT 'Submitted',
    Remarks         TEXT            NULL,

    CONSTRAINT FK_Applications_Startups
        FOREIGN KEY (StartupId) REFERENCES Startups(Id)
        ON DELETE CASCADE,

    KEY IX_Applications_StartupId (StartupId)
) ENGINE=InnoDB;

CREATE TABLE ProgressReports (
    Id              INT AUTO_INCREMENT PRIMARY KEY,
    StartupId       INT             NOT NULL,
    SubmissionDate  DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    Milestones      TEXT            NOT NULL,
    Remarks         TEXT            NULL,

    CONSTRAINT FK_ProgressReports_Startups
        FOREIGN KEY (StartupId) REFERENCES Startups(Id)
        ON DELETE CASCADE,

    KEY IX_ProgressReports_StartupId (StartupId)
) ENGINE=InnoDB;

CREATE TABLE FundingRequests (
    Id                  INT AUTO_INCREMENT PRIMARY KEY,
    StartupId           INT             NOT NULL,
    Amount              DECIMAL(18,2)   NOT NULL,
    FundingType         VARCHAR(50)     NOT NULL,
    ApprovalStatus      VARCHAR(20)     NOT NULL DEFAULT 'Pending',
    ApprovedByUserId    INT             NULL,   -- references Identity.Users.Id (app-enforced)
    ApprovedDate        DATETIME        NULL,
    TransactionReference VARCHAR(200)   NULL,
    PaymentUrl          VARCHAR(500)    NULL,

    CONSTRAINT FK_FundingRequests_Startups
        FOREIGN KEY (StartupId) REFERENCES Startups(Id)
        ON DELETE CASCADE,

    KEY IX_FundingRequests_StartupId (StartupId)
) ENGINE=InnoDB;
