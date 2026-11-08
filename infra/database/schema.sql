-- Reference schema only. Apply database changes through KinTraffic.Api EF Core migrations.
-- Keep this file aligned with the model; do not execute it against a migration-managed database.

CREATE TABLE AuditLogs (
    Id NVARCHAR(64) NOT NULL PRIMARY KEY,
    Entity NVARCHAR(128) NOT NULL,
    Action NVARCHAR(64) NOT NULL,
    Actor NVARCHAR(128) NOT NULL,
    OccurredAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

CREATE INDEX IX_AuditLogs_Entity_OccurredAt ON AuditLogs (Entity, OccurredAt);

CREATE TABLE Contracts (
    Id NVARCHAR(64) NOT NULL PRIMARY KEY,
    VendorName NVARCHAR(200) NOT NULL,
    ServiceType NVARCHAR(100) NOT NULL,
    StartDate DATE NOT NULL,
    EndDate DATE NOT NULL,
    Status NVARCHAR(32) NOT NULL
);

CREATE TABLE InventoryItems (
    Id NVARCHAR(64) NOT NULL PRIMARY KEY,
    Sku NVARCHAR(64) NOT NULL,
    Name NVARCHAR(200) NOT NULL,
    QuantityOnHand INT NOT NULL,
    Location NVARCHAR(100) NOT NULL,
    ReorderLevel INT NOT NULL
);

CREATE INDEX IX_InventoryItems_Sku ON InventoryItems (Sku);

CREATE TABLE Invoices (
    Id NVARCHAR(64) NOT NULL PRIMARY KEY,
    ContractId NVARCHAR(64) NOT NULL,
    Amount DECIMAL(18, 2) NOT NULL,
    IssuedAt DATETIME2 NOT NULL,
    Status NVARCHAR(32) NOT NULL
);

CREATE INDEX IX_Invoices_ContractId ON Invoices (ContractId);

CREATE TABLE Junctions (
    Id NVARCHAR(64) NOT NULL PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Location NVARCHAR(200) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

CREATE TABLE RoadSegments (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Name NVARCHAR(150) NOT NULL,
    CorridorName NVARCHAR(150) NULL,
    Geometry NVARCHAR(MAX) NOT NULL,
    LengthMeters DECIMAL(10, 2) NULL
);

CREATE TABLE TrafficSignals (
    Id NVARCHAR(64) NOT NULL PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    Status NVARCHAR(32) NOT NULL,
    CycleTime INT NOT NULL,
    QueueLength INT NOT NULL,
    UpdatedAt DATETIME2 NOT NULL
);

CREATE TABLE Devices (
    Id NVARCHAR(64) NOT NULL PRIMARY KEY,
    JunctionId NVARCHAR(64) NOT NULL,
    Name NVARCHAR(100) NOT NULL,
    Type NVARCHAR(50) NOT NULL,
    Status NVARCHAR(50) NOT NULL,
    SerialNumber NVARCHAR(100) NULL,
    InstalledAt DATETIME2 NULL,
    CONSTRAINT FK_Devices_Junctions_JunctionId FOREIGN KEY (JunctionId)
        REFERENCES Junctions (Id)
);

CREATE INDEX IX_Devices_JunctionId ON Devices (JunctionId);

CREATE TABLE Faults (
    Id NVARCHAR(64) NOT NULL PRIMARY KEY,
    DeviceId NVARCHAR(64) NOT NULL,
    FaultType NVARCHAR(100) NULL,
    Severity NVARCHAR(32) NOT NULL,
    Status NVARCHAR(32) NOT NULL,
    Description NVARCHAR(1000) NOT NULL,
    DetectedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    ResolvedAt DATETIME2 NULL,
    CONSTRAINT FK_Faults_Devices_DeviceId FOREIGN KEY (DeviceId)
        REFERENCES Devices (Id)
);

CREATE INDEX IX_Faults_DeviceId_Status ON Faults (DeviceId, Status);

CREATE TABLE WorkOrders (
    Id NVARCHAR(64) NOT NULL PRIMARY KEY,
    FaultId NVARCHAR(64) NOT NULL,
    AssignedTo NVARCHAR(64) NULL,
    Status NVARCHAR(32) NOT NULL,
    Priority NVARCHAR(32) NOT NULL,
    ScheduledFor DATETIME2 NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CompletedAt DATETIME2 NULL,
    CONSTRAINT FK_WorkOrders_Faults_FaultId FOREIGN KEY (FaultId)
        REFERENCES Faults (Id)
);

CREATE INDEX IX_WorkOrders_AssignedTo ON WorkOrders (AssignedTo);
CREATE INDEX IX_WorkOrders_FaultId ON WorkOrders (FaultId);

CREATE TABLE Telemetry (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    DeviceId NVARCHAR(64) NOT NULL,
    Metric NVARCHAR(50) NOT NULL,
    Value FLOAT NOT NULL,
    RecordedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Telemetry_Devices_DeviceId FOREIGN KEY (DeviceId)
        REFERENCES Devices (Id)
);

CREATE INDEX IX_Telemetry_DeviceId_RecordedAt ON Telemetry (DeviceId, RecordedAt);

CREATE TABLE TrafficStates (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    RoadSegmentId INT NOT NULL,
    CongestionLevel NVARCHAR(50) NOT NULL,
    AverageSpeedKph DECIMAL(10, 2) NOT NULL,
    VehicleCount INT NOT NULL,
    RecordedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_TrafficStates_RoadSegments_RoadSegmentId FOREIGN KEY (RoadSegmentId)
        REFERENCES RoadSegments (Id)
);

CREATE INDEX IX_TrafficStates_RoadSegmentId_RecordedAt ON TrafficStates (RoadSegmentId, RecordedAt);
