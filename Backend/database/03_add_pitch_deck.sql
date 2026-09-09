-- ============================================================
-- StartupIMS - add pitch deck support to Startups
-- Run this against startupims_core after 02_core_schema.sql
-- ============================================================

USE startupims_core;

ALTER TABLE Startups
    ADD COLUMN PitchDeckPath VARCHAR(500) NULL,
    ADD COLUMN PitchDeckOriginalFileName VARCHAR(255) NULL,
    ADD COLUMN PitchDeckUploadedAt DATETIME NULL;
