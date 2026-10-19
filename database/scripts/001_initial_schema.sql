/*
  Idempotent SQL Server deployment script.
  Use this script for controlled database-admin deployment when automatic
  EF Core migrations are disabled in production.
*/

IF OBJECT_ID(N'dbo.__EFMigrationsHistory', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.__EFMigrationsHistory
    (
        MigrationId nvarchar(150) NOT NULL CONSTRAINT PK___EFMigrationsHistory PRIMARY KEY,
        ProductVersion nvarchar(32) NOT NULL
    );
END;

IF OBJECT_ID(N'dbo.Departments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Departments
    (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_Departments PRIMARY KEY,
        Name nvarchar(150) NOT NULL,
        Code nvarchar(30) NOT NULL,
        IsActive bit NOT NULL CONSTRAINT DF_Departments_IsActive DEFAULT 1
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Departments_Code' AND object_id = OBJECT_ID(N'dbo.Departments'))
    CREATE UNIQUE INDEX IX_Departments_Code ON dbo.Departments(Code);

IF OBJECT_ID(N'dbo.AspNetRoles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AspNetRoles
    (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_AspNetRoles PRIMARY KEY,
        Name nvarchar(256) NULL,
        NormalizedName nvarchar(256) NULL,
        ConcurrencyStamp nvarchar(max) NULL
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'RoleNameIndex' AND object_id = OBJECT_ID(N'dbo.AspNetRoles'))
    CREATE UNIQUE INDEX RoleNameIndex ON dbo.AspNetRoles(NormalizedName) WHERE NormalizedName IS NOT NULL;

IF OBJECT_ID(N'dbo.AspNetUsers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AspNetUsers
    (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_AspNetUsers PRIMARY KEY,
        UserName nvarchar(256) NULL,
        NormalizedUserName nvarchar(256) NULL,
        Email nvarchar(256) NULL,
        NormalizedEmail nvarchar(256) NULL,
        EmailConfirmed bit NOT NULL,
        PasswordHash nvarchar(max) NULL,
        SecurityStamp nvarchar(max) NULL,
        ConcurrencyStamp nvarchar(max) NULL,
        PhoneNumber nvarchar(max) NULL,
        PhoneNumberConfirmed bit NOT NULL,
        TwoFactorEnabled bit NOT NULL,
        LockoutEnd datetimeoffset NULL,
        LockoutEnabled bit NOT NULL,
        AccessFailedCount int NOT NULL,
        DisplayName nvarchar(150) NOT NULL,
        DepartmentId uniqueidentifier NULL,
        IsActive bit NOT NULL,
        CreatedAtUtc datetime2 NOT NULL,
        CONSTRAINT FK_AspNetUsers_Departments
            FOREIGN KEY (DepartmentId) REFERENCES dbo.Departments(Id) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UserNameIndex' AND object_id = OBJECT_ID(N'dbo.AspNetUsers'))
    CREATE UNIQUE INDEX UserNameIndex ON dbo.AspNetUsers(NormalizedUserName) WHERE NormalizedUserName IS NOT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'EmailIndex' AND object_id = OBJECT_ID(N'dbo.AspNetUsers'))
    CREATE INDEX EmailIndex ON dbo.AspNetUsers(NormalizedEmail);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AspNetUsers_DepartmentId' AND object_id = OBJECT_ID(N'dbo.AspNetUsers'))
    CREATE INDEX IX_AspNetUsers_DepartmentId ON dbo.AspNetUsers(DepartmentId);

IF OBJECT_ID(N'dbo.AspNetRoleClaims', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AspNetRoleClaims
    (
        Id int IDENTITY NOT NULL CONSTRAINT PK_AspNetRoleClaims PRIMARY KEY,
        RoleId uniqueidentifier NOT NULL,
        ClaimType nvarchar(max) NULL,
        ClaimValue nvarchar(max) NULL,
        CONSTRAINT FK_AspNetRoleClaims_AspNetRoles
            FOREIGN KEY (RoleId) REFERENCES dbo.AspNetRoles(Id) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AspNetRoleClaims_RoleId' AND object_id = OBJECT_ID(N'dbo.AspNetRoleClaims'))
    CREATE INDEX IX_AspNetRoleClaims_RoleId ON dbo.AspNetRoleClaims(RoleId);

IF OBJECT_ID(N'dbo.AspNetUserClaims', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AspNetUserClaims
    (
        Id int IDENTITY NOT NULL CONSTRAINT PK_AspNetUserClaims PRIMARY KEY,
        UserId uniqueidentifier NOT NULL,
        ClaimType nvarchar(max) NULL,
        ClaimValue nvarchar(max) NULL,
        CONSTRAINT FK_AspNetUserClaims_AspNetUsers
            FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers(Id) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AspNetUserClaims_UserId' AND object_id = OBJECT_ID(N'dbo.AspNetUserClaims'))
    CREATE INDEX IX_AspNetUserClaims_UserId ON dbo.AspNetUserClaims(UserId);

IF OBJECT_ID(N'dbo.AspNetUserLogins', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AspNetUserLogins
    (
        LoginProvider nvarchar(128) NOT NULL,
        ProviderKey nvarchar(128) NOT NULL,
        ProviderDisplayName nvarchar(max) NULL,
        UserId uniqueidentifier NOT NULL,
        CONSTRAINT PK_AspNetUserLogins PRIMARY KEY (LoginProvider, ProviderKey),
        CONSTRAINT FK_AspNetUserLogins_AspNetUsers
            FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers(Id) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AspNetUserLogins_UserId' AND object_id = OBJECT_ID(N'dbo.AspNetUserLogins'))
    CREATE INDEX IX_AspNetUserLogins_UserId ON dbo.AspNetUserLogins(UserId);

IF OBJECT_ID(N'dbo.AspNetUserRoles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AspNetUserRoles
    (
        UserId uniqueidentifier NOT NULL,
        RoleId uniqueidentifier NOT NULL,
        CONSTRAINT PK_AspNetUserRoles PRIMARY KEY (UserId, RoleId),
        CONSTRAINT FK_AspNetUserRoles_AspNetUsers
            FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers(Id) ON DELETE CASCADE,
        CONSTRAINT FK_AspNetUserRoles_AspNetRoles
            FOREIGN KEY (RoleId) REFERENCES dbo.AspNetRoles(Id) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AspNetUserRoles_RoleId' AND object_id = OBJECT_ID(N'dbo.AspNetUserRoles'))
    CREATE INDEX IX_AspNetUserRoles_RoleId ON dbo.AspNetUserRoles(RoleId);

IF OBJECT_ID(N'dbo.AspNetUserTokens', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AspNetUserTokens
    (
        UserId uniqueidentifier NOT NULL,
        LoginProvider nvarchar(128) NOT NULL,
        Name nvarchar(128) NOT NULL,
        Value nvarchar(max) NULL,
        CONSTRAINT PK_AspNetUserTokens PRIMARY KEY (UserId, LoginProvider, Name),
        CONSTRAINT FK_AspNetUserTokens_AspNetUsers
            FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers(Id) ON DELETE CASCADE
    );
END;

IF OBJECT_ID(N'dbo.ProcurementPlans', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProcurementPlans
    (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_ProcurementPlans PRIMARY KEY,
        PlanNumber nvarchar(50) NOT NULL,
        Title nvarchar(200) NOT NULL,
        DepartmentId uniqueidentifier NOT NULL,
        RequesterId uniqueidentifier NOT NULL,
        ManagerId uniqueidentifier NULL,
        PlanningPeriodStart date NOT NULL,
        PlanningPeriodEnd date NOT NULL,
        Priority int NOT NULL,
        Status int NOT NULL,
        BusinessJustification nvarchar(4000) NOT NULL,
        EstimatedBudget decimal(19,4) NOT NULL,
        RequestedDateUtc datetime2 NOT NULL,
        Notes nvarchar(4000) NULL,
        RowVersion rowversion NOT NULL,
        CreatedAtUtc datetime2 NOT NULL,
        UpdatedAtUtc datetime2 NOT NULL,
        CONSTRAINT FK_ProcurementPlans_Departments
            FOREIGN KEY (DepartmentId) REFERENCES dbo.Departments(Id),
        CONSTRAINT FK_ProcurementPlans_Requester
            FOREIGN KEY (RequesterId) REFERENCES dbo.AspNetUsers(Id),
        CONSTRAINT FK_ProcurementPlans_Manager
            FOREIGN KEY (ManagerId) REFERENCES dbo.AspNetUsers(Id)
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ProcurementPlans_PlanNumber' AND object_id = OBJECT_ID(N'dbo.ProcurementPlans'))
    CREATE UNIQUE INDEX IX_ProcurementPlans_PlanNumber ON dbo.ProcurementPlans(PlanNumber);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ProcurementPlans_DepartmentId_Status' AND object_id = OBJECT_ID(N'dbo.ProcurementPlans'))
    CREATE INDEX IX_ProcurementPlans_DepartmentId_Status ON dbo.ProcurementPlans(DepartmentId, Status);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ProcurementPlans_RequesterId_CreatedAtUtc' AND object_id = OBJECT_ID(N'dbo.ProcurementPlans'))
    CREATE INDEX IX_ProcurementPlans_RequesterId_CreatedAtUtc ON dbo.ProcurementPlans(RequesterId, CreatedAtUtc);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ProcurementPlans_ManagerId_Status' AND object_id = OBJECT_ID(N'dbo.ProcurementPlans'))
    CREATE INDEX IX_ProcurementPlans_ManagerId_Status ON dbo.ProcurementPlans(ManagerId, Status);

IF OBJECT_ID(N'dbo.ProcurementPlanItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProcurementPlanItems
    (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_ProcurementPlanItems PRIMARY KEY,
        ProcurementPlanId uniqueidentifier NOT NULL,
        Category nvarchar(100) NOT NULL,
        Description nvarchar(500) NOT NULL,
        Quantity decimal(19,4) NOT NULL,
        EstimatedUnitCost decimal(19,4) NOT NULL,
        EstimatedTotalCost decimal(19,4) NOT NULL,
        RequiredPeriod date NOT NULL,
        Priority int NOT NULL,
        Justification nvarchar(2000) NOT NULL,
        VendorPreference nvarchar(200) NULL,
        Status int NOT NULL,
        CONSTRAINT FK_ProcurementPlanItems_Plans
            FOREIGN KEY (ProcurementPlanId) REFERENCES dbo.ProcurementPlans(Id) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ProcurementPlanItems_ProcurementPlanId' AND object_id = OBJECT_ID(N'dbo.ProcurementPlanItems'))
    CREATE INDEX IX_ProcurementPlanItems_ProcurementPlanId ON dbo.ProcurementPlanItems(ProcurementPlanId);

IF OBJECT_ID(N'dbo.AuditEntries', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AuditEntries
    (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_AuditEntries PRIMARY KEY,
        ProcurementPlanId uniqueidentifier NOT NULL,
        ActorUserId uniqueidentifier NULL,
        Action nvarchar(80) NOT NULL,
        PreviousStatus nvarchar(80) NULL,
        NewStatus nvarchar(80) NULL,
        Comment nvarchar(2000) NULL,
        MetadataJson nvarchar(8000) NULL,
        CreatedAtUtc datetime2 NOT NULL,
        CONSTRAINT FK_AuditEntries_Plans
            FOREIGN KEY (ProcurementPlanId) REFERENCES dbo.ProcurementPlans(Id) ON DELETE CASCADE,
        CONSTRAINT FK_AuditEntries_Actor
            FOREIGN KEY (ActorUserId) REFERENCES dbo.AspNetUsers(Id) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AuditEntries_Plan_Created' AND object_id = OBJECT_ID(N'dbo.AuditEntries'))
    CREATE INDEX IX_AuditEntries_Plan_Created ON dbo.AuditEntries(ProcurementPlanId, CreatedAtUtc);

IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = N'20260918000000_InitialCreate')
    INSERT INTO dbo.__EFMigrationsHistory(MigrationId, ProductVersion) VALUES (N'20260918000000_InitialCreate', N'8.0.19');
