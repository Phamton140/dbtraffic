-- DbTraffic - Esquema inicial de base de datos del producto
-- Versión: 0.1.0
-- Motor: SQL Server 2019+

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'DbTraffic')
BEGIN
    CREATE DATABASE DbTraffic;
END
GO

USE DbTraffic;
GO

-- Esquemas
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'catalog')
    EXEC('CREATE SCHEMA catalog');
GO

IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'history')
    EXEC('CREATE SCHEMA history');
GO

IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'rules')
    EXEC('CREATE SCHEMA rules');
GO

IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'metrics')
    EXEC('CREATE SCHEMA metrics');
GO

-- Tabla: Registro de migraciones aplicadas
CREATE TABLE catalog.SchemaMigrations (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    MigrationName NVARCHAR(255) NOT NULL UNIQUE,
    AppliedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);
GO

-- Tabla: Instancias objetivo
CREATE TABLE catalog.Instances (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Name NVARCHAR(200) NOT NULL,
    ConnectionString NVARCHAR(1000) NOT NULL,
    Description NVARCHAR(500) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);
GO

-- Tabla: Procesos registrados
CREATE TABLE catalog.Processes (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    InstanceId UNIQUEIDENTIFIER NOT NULL,
    Name NVARCHAR(200) NOT NULL,
    ProcessType NVARCHAR(100) NOT NULL, -- SQLAgentJob, StoredProcedure, ETL, Manual, Batch, Integration, SAS
    Description NVARCHAR(500) NULL,
    EstimatedDurationMinutes INT NULL,
    PreferredWindowStart TIME NULL,
    PreferredWindowEnd TIME NULL,
    CpuIntensity TINYINT NOT NULL DEFAULT 1, -- 1=Bajo, 2=Medio, 3=Alto, 4=Crítico
    IoIntensity TINYINT NOT NULL DEFAULT 1,
    MemoryIntensity TINYINT NOT NULL DEFAULT 1,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_Processes_Instances FOREIGN KEY (InstanceId) REFERENCES catalog.Instances(Id)
);
GO

CREATE UNIQUE INDEX IX_Processes_Name_InstanceId ON catalog.Processes(Name, InstanceId);
GO

-- Tabla: Objetos SQL asociados a procesos
CREATE TABLE catalog.ProcessObjects (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    ProcessId UNIQUEIDENTIFIER NOT NULL,
    SchemaName NVARCHAR(128) NOT NULL,
    ObjectName NVARCHAR(128) NOT NULL,
    ObjectType NVARCHAR(50) NOT NULL, -- Table, View, StoredProcedure, Function
    IsCritical BIT NOT NULL DEFAULT 0,
    AccessType NVARCHAR(50) NULL, -- Read, Write, Mixed
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_ProcessObjects_Processes FOREIGN KEY (ProcessId) REFERENCES catalog.Processes(Id) ON DELETE CASCADE
);
GO

CREATE UNIQUE INDEX IX_ProcessObjects_Process_Schema_Object ON catalog.ProcessObjects(ProcessId, SchemaName, ObjectName);
GO

-- Tabla: Horarios de ejecución de procesos
CREATE TABLE catalog.ProcessSchedules (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    ProcessId UNIQUEIDENTIFIER NOT NULL,
    DayOfWeek TINYINT NULL, -- 0=Domingo, 6=Sábado; NULL = todos los días
    StartTime TIME NOT NULL,
    DurationMinutes INT NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_ProcessSchedules_Processes FOREIGN KEY (ProcessId) REFERENCES catalog.Processes(Id) ON DELETE CASCADE
);
GO

-- Tabla: Jobs descubiertos en instancias objetivo
CREATE TABLE catalog.DiscoveredJobs (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    InstanceId UNIQUEIDENTIFIER NOT NULL,
    JobId UNIQUEIDENTIFIER NOT NULL,
    Name NVARCHAR(200) NOT NULL,
    Description NVARCHAR(500) NULL,
    Enabled BIT NOT NULL,
    EstimatedDurationMinutes INT NULL,
    LastRunDate DATETIME2 NULL,
    NextRunDate DATETIME2 NULL,
    DiscoveredAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    AssociatedProcessId UNIQUEIDENTIFIER NULL,
    CONSTRAINT FK_DiscoveredJobs_Instances FOREIGN KEY (InstanceId) REFERENCES catalog.Instances(Id),
    CONSTRAINT FK_DiscoveredJobs_Processes FOREIGN KEY (AssociatedProcessId) REFERENCES catalog.Processes(Id)
);
GO

CREATE UNIQUE INDEX IX_DiscoveredJobs_Instance_JobId ON catalog.DiscoveredJobs(InstanceId, JobId);
GO

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
GO

CREATE INDEX IX_DiscoveredJobSchedules_DiscoveredJobId ON catalog.DiscoveredJobSchedules(DiscoveredJobId);
GO

CREATE UNIQUE INDEX IX_DiscoveredJobs_Instance_JobId ON catalog.DiscoveredJobs(InstanceId, JobId);
GO

-- Tabla: Objetos descubiertos en instancias objetivo
CREATE TABLE catalog.DiscoveredObjects (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    InstanceId UNIQUEIDENTIFIER NOT NULL,
    SchemaName NVARCHAR(128) NOT NULL,
    ObjectName NVARCHAR(128) NOT NULL,
    ObjectType NVARCHAR(50) NOT NULL,
    DiscoveredAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_DiscoveredObjects_Instances FOREIGN KEY (InstanceId) REFERENCES catalog.Instances(Id)
);
GO

CREATE UNIQUE INDEX IX_DiscoveredObjects_Instance_Schema_Object ON catalog.DiscoveredObjects(InstanceId, SchemaName, ObjectName);
GO

-- Tabla: Definiciones de reglas
CREATE TABLE rules.RuleDefinitions (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Name NVARCHAR(200) NOT NULL,
    Description NVARCHAR(500) NULL,
    RuleType NVARCHAR(200) NOT NULL, -- Nombre completo de la clase que implementa IRule
    IsActive BIT NOT NULL DEFAULT 1,
    DefaultWeight DECIMAL(5,2) NOT NULL DEFAULT 1.0,
    ConfigurationJson NVARCHAR(MAX) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);
GO

-- Tabla: Evaluaciones de reglas
CREATE TABLE rules.RuleEvaluations (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    RuleDefinitionId UNIQUEIDENTIFIER NOT NULL,
    ProcessId UNIQUEIDENTIFIER NOT NULL,
    EvaluatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    RiskScore DECIMAL(5,2) NOT NULL,
    RiskLevel NVARCHAR(20) NOT NULL, -- Low, Medium, High, Critical
    DetailsJson NVARCHAR(MAX) NULL,
    CONSTRAINT FK_RuleEvaluations_RuleDefinitions FOREIGN KEY (RuleDefinitionId) REFERENCES rules.RuleDefinitions(Id),
    CONSTRAINT FK_RuleEvaluations_Processes FOREIGN KEY (ProcessId) REFERENCES catalog.Processes(Id)
);
GO

CREATE INDEX IX_RuleEvaluations_Process_EvaluatedAt ON rules.RuleEvaluations(ProcessId, EvaluatedAt);
GO

-- Tabla: Historial de ejecuciones
CREATE TABLE history.Executions (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    ProcessId UNIQUEIDENTIFIER NULL,
    InstanceId UNIQUEIDENTIFIER NOT NULL,
    Source NVARCHAR(50) NOT NULL DEFAULT 'Manual', -- Manual, Imported, Discovered
    StartedAt DATETIME2 NOT NULL,
    CompletedAt DATETIME2 NULL,
    DurationMinutes INT NULL,
    Status NVARCHAR(50) NOT NULL, -- Running, Completed, Failed, Cancelled
    AffectedObjectsJson NVARCHAR(MAX) NULL,
    Notes NVARCHAR(1000) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_Executions_Processes FOREIGN KEY (ProcessId) REFERENCES catalog.Processes(Id),
    CONSTRAINT FK_Executions_Instances FOREIGN KEY (InstanceId) REFERENCES catalog.Instances(Id)
);
GO

CREATE INDEX IX_Executions_Process_StartedAt ON history.Executions(ProcessId, StartedAt);
GO

-- Tabla: Conflictos detectados
CREATE TABLE history.Conflicts (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    ProcessId UNIQUEIDENTIFIER NOT NULL,
    ConflictingProcessId UNIQUEIDENTIFIER NOT NULL,
    DetectedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ConflictType NVARCHAR(100) NOT NULL,
    ObjectSchema NVARCHAR(128) NULL,
    ObjectName NVARCHAR(128) NULL,
    Description NVARCHAR(500) NULL,
    CONSTRAINT FK_Conflicts_Process FOREIGN KEY (ProcessId) REFERENCES catalog.Processes(Id),
    CONSTRAINT FK_Conflicts_ConflictingProcess FOREIGN KEY (ConflictingProcessId) REFERENCES catalog.Processes(Id)
);
GO

CREATE INDEX IX_Conflicts_Process_DetectedAt ON history.Conflicts(ProcessId, DetectedAt);
GO

-- Tabla: Snapshots de métricas de instancia
CREATE TABLE metrics.InstanceSnapshots (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    InstanceId UNIQUEIDENTIFIER NOT NULL,
    CapturedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CpuPercent DECIMAL(5,2) NULL,
    MemoryPercent DECIMAL(5,2) NULL,
    ActiveRequests INT NULL,
    BlockingSessions INT NULL,
    WaitTimeMs BIGINT NULL,
    SnapshotJson NVARCHAR(MAX) NULL,
    CONSTRAINT FK_InstanceSnapshots_Instances FOREIGN KEY (InstanceId) REFERENCES catalog.Instances(Id)
);
GO

CREATE INDEX IX_InstanceSnapshots_Instance_CapturedAt ON metrics.InstanceSnapshots(InstanceId, CapturedAt);
GO
