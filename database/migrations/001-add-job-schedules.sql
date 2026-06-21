-- Migration 001: Add job schedule support to discovered jobs
-- Adds duration/last-run columns to DiscoveredJobs and creates DiscoveredJobSchedules table.

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('catalog.DiscoveredJobs') AND name = 'EstimatedDurationMinutes')
BEGIN
    ALTER TABLE catalog.DiscoveredJobs ADD EstimatedDurationMinutes INT NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('catalog.DiscoveredJobs') AND name = 'LastRunDate')
BEGIN
    ALTER TABLE catalog.DiscoveredJobs ADD LastRunDate DATETIME2 NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('catalog.DiscoveredJobs') AND name = 'NextRunDate')
BEGIN
    ALTER TABLE catalog.DiscoveredJobs ADD NextRunDate DATETIME2 NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables t INNER JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'catalog' AND t.name = 'DiscoveredJobSchedules')
BEGIN
    CREATE TABLE catalog.DiscoveredJobSchedules (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        DiscoveredJobId UNIQUEIDENTIFIER NOT NULL,
        ScheduleId INT NOT NULL,
        Name NVARCHAR(200) NOT NULL,
        FrequencyType INT NOT NULL,
        FrequencyInterval INT NOT NULL,
        FrequencySubdayType INT NOT NULL,
        FrequencySubdayInterval INT NOT NULL,
        FrequencyRelativeInterval INT NOT NULL,
        FrequencyRecurrenceFactor INT NOT NULL,
        ActiveStartTime TIME NOT NULL,
        ActiveEndTime TIME NOT NULL,
        Description NVARCHAR(500) NOT NULL,
        DiscoveredAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_DiscoveredJobSchedules_DiscoveredJobs FOREIGN KEY (DiscoveredJobId) REFERENCES catalog.DiscoveredJobs(Id) ON DELETE CASCADE
    );

    CREATE INDEX IX_DiscoveredJobSchedules_DiscoveredJobId ON catalog.DiscoveredJobSchedules(DiscoveredJobId);
END
GO
