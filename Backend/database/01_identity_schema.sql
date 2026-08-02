-- ============================================================
-- StartupIMS - Identity module database
-- Run this against MySQL first: CREATE DATABASE startupims_identity;
-- ============================================================

CREATE DATABASE IF NOT EXISTS startupims_identity
    CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

USE startupims_identity;

CREATE TABLE Users (
    Id              INT AUTO_INCREMENT PRIMARY KEY,
    Name            VARCHAR(150)    NOT NULL,
    Email           VARCHAR(200)    NOT NULL,
    PasswordHash    VARCHAR(255)    NOT NULL,
    Role            VARCHAR(20)     NOT NULL,   -- 'Admin' | 'Mentor' | 'Founder'
    CreatedAt       DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,

    UNIQUE KEY UX_Users_Email (Email)
) ENGINE=InnoDB;

CREATE TABLE RefreshTokens (
    Id              INT AUTO_INCREMENT PRIMARY KEY,
    UserId          INT             NOT NULL,
    TokenHash       VARCHAR(200)    NOT NULL,
    ExpiresAt       DATETIME        NOT NULL,
    CreatedAt       DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    RevokedAt       DATETIME        NULL,

    CONSTRAINT FK_RefreshTokens_Users
        FOREIGN KEY (UserId) REFERENCES Users(Id)
        ON DELETE CASCADE,

    KEY IX_RefreshTokens_UserId (UserId),
    KEY IX_RefreshTokens_TokenHash (TokenHash)
) ENGINE=InnoDB;
