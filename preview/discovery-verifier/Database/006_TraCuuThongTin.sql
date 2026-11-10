-- Information discovery extension 006: additive, original 46 tables/rows retained.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
DECLARE @lock int;
EXEC @lock=sys.sp_getapplock @Resource=N'HoTroDuLich.Discovery.v1',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=30000;
IF @lock<0 THROW 51000,N'Không lấy được khóa nâng cấp tra cứu.',1;
IF OBJECT_ID(N'dbo.DiscoveryEntry',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.[DiscoveryEntry] (
  [Id] bigint IDENTITY(1,1) NOT NULL,
  [Slug] varchar(160) NOT NULL,
  [Kind] varchar(25) NOT NULL,
  [ParentId] bigint NULL,
  [PlaceId] bigint NULL,
  [ServiceId] bigint NULL,
  [Title] nvarchar(200) NOT NULL,
  [Region] nvarchar(100) NOT NULL,
  [Summary] nvarchar(700) NOT NULL,
  [Body] nvarchar(max) NOT NULL,
  [Address] nvarchar(400) NULL,
  [Latitude] decimal(9,6) NULL,
  [Longitude] decimal(9,6) NULL,
  [LocationNote] nvarchar(300) NULL,
  [SourceName] nvarchar(200) NOT NULL,
  [SourceStamp] varchar(64) NULL,
  [SourceUrl] nvarchar(1000) NULL,
  [CheckedOn] date NULL,
  [ValidFrom] date NULL,
  [ValidTo] date NULL,
  [PriceFrom] decimal(18,2) NULL,
  [PriceTo] decimal(18,2) NULL,
  [PriceUnit] nvarchar(100) NULL,
  [Includes] nvarchar(1000) NULL,
  [Excludes] nvarchar(1000) NULL,
  [Hours] nvarchar(700) NULL,
  [OpenDays] varchar(7) NULL,
  [OpenTime] time(0) NULL,
  [CloseTime] time(0) NULL,
  [DurationMinutes] int NULL,
  [Tags] nvarchar(500) NOT NULL,
  [TransportMode] varchar(25) NULL,
  [Origin] nvarchar(200) NULL,
  [Destination] nvarchar(200) NULL,
  [Contact] nvarchar(300) NULL,
  [Status] varchar(20) NOT NULL,
  [CreatedBy] bigint NULL,
  [UpdatedBy] bigint NULL,
  [UpdatedAt] datetime2(0) NOT NULL,
  [RowVersion] rowversion NOT NULL,
  CONSTRAINT [PK_DiscoveryEntry] PRIMARY KEY ([Id]),
  CONSTRAINT [CK_DS_Kind] CHECK (Kind IN ('Destination','Area','Place','Service','Guide','Transport','Event','Amenity')),
  CONSTRAINT [CK_DS_Link] CHECK ((PlaceId IS NULL OR ServiceId IS NULL) AND (PlaceId IS NULL OR Kind='Place') AND (ServiceId IS NULL OR Kind='Service')),
  CONSTRAINT [CK_DS_Coord] CHECK ((Latitude IS NULL AND Longitude IS NULL) OR (Latitude IS NOT NULL AND Longitude IS NOT NULL AND Latitude BETWEEN -90 AND 90 AND Longitude BETWEEN -180 AND 180)),
  CONSTRAINT [CK_DS_Price] CHECK ((PriceFrom IS NULL OR PriceFrom>=0) AND (PriceTo IS NULL OR PriceTo>=0) AND (PriceFrom IS NULL OR PriceTo IS NULL OR PriceTo>=PriceFrom)),
  CONSTRAINT [CK_DS_Dates] CHECK (ValidFrom IS NULL OR ValidTo IS NULL OR ValidTo>=ValidFrom),
  CONSTRAINT [CK_DS_Duration] CHECK (DurationMinutes IS NULL OR DurationMinutes BETWEEN 1 AND 1440),
  CONSTRAINT [CK_DS_Status] CHECK (Status IN ('Draft','Published','Hidden')),
  CONSTRAINT [CK_DS_Title] CHECK (LEN(LTRIM(RTRIM(Title)))>0),
  CONSTRAINT [CK_DS_Self] CHECK (ParentId IS NULL OR ParentId<>Id),
  CONSTRAINT [CK_DS_Hours] CHECK ((OpenTime IS NULL AND CloseTime IS NULL AND OpenDays IS NULL) OR (OpenTime IS NOT NULL AND CloseTime IS NOT NULL AND OpenDays IS NOT NULL AND LEN(OpenDays)>0 AND OpenDays NOT LIKE '%[^1234567]%')),
  CONSTRAINT [FK_DiscoveryEntry_ParentId] FOREIGN KEY ([ParentId]) REFERENCES dbo.[DiscoveryEntry] ([Id]),
  CONSTRAINT [FK_DiscoveryEntry_PlaceId] FOREIGN KEY ([PlaceId]) REFERENCES dbo.[DiaDiemDuLich] ([MaDiaDiem]),
  CONSTRAINT [FK_DiscoveryEntry_ServiceId] FOREIGN KEY ([ServiceId]) REFERENCES dbo.[DichVuDuLich] ([MaDichVu]),
  CONSTRAINT [FK_DiscoveryEntry_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES dbo.[TaiKhoan] ([MaTaiKhoan]),
  CONSTRAINT [FK_DiscoveryEntry_UpdatedBy] FOREIGN KEY ([UpdatedBy]) REFERENCES dbo.[TaiKhoan] ([MaTaiKhoan])
 );
 CREATE UNIQUE INDEX [UX_DS_Slug] ON dbo.[DiscoveryEntry] ([Slug]);
 CREATE UNIQUE INDEX [UX_DS_Place] ON dbo.[DiscoveryEntry] ([PlaceId]) WHERE PlaceId IS NOT NULL;
 CREATE UNIQUE INDEX [UX_DS_Service] ON dbo.[DiscoveryEntry] ([ServiceId]) WHERE ServiceId IS NOT NULL;
 CREATE INDEX [IX_DS_Search] ON dbo.[DiscoveryEntry] ([Status],[Kind],[Region]);
 CREATE INDEX [IX_DS_Parent] ON dbo.[DiscoveryEntry] ([ParentId],[Status]);
 CREATE INDEX [IX_DS_Dates] ON dbo.[DiscoveryEntry] ([ValidFrom],[ValidTo]);
END;
IF OBJECT_ID(N'dbo.DiscoveryFeature',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.[DiscoveryFeature] (
  [EntryId] bigint NOT NULL,
  [Code] varchar(40) NOT NULL,
  [State] varchar(12) NOT NULL,
  [Note] nvarchar(500) NULL,
  CONSTRAINT [PK_DiscoveryFeature] PRIMARY KEY ([EntryId],[Code]),
  CONSTRAINT [CK_DSF_State] CHECK (State IN ('Yes','No','Unknown')),
  CONSTRAINT [CK_DSF_Code] CHECK (Code IN ('AccessibleEntrance','AccessibleRestroom','Parking','Seating','Children','Indoor','PublicTransport','BookingRequired','Luggage','Charging','FreeEntry','Vegetarian')),
  CONSTRAINT [FK_DiscoveryFeature_EntryId] FOREIGN KEY ([EntryId]) REFERENCES dbo.[DiscoveryEntry] ([Id])
 );
END;
IF OBJECT_ID(N'dbo.DiscoveryPrice',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.[DiscoveryPrice] (
  [Id] bigint IDENTITY(1,1) NOT NULL,
  [EntryId] bigint NOT NULL,
  [Label] nvarchar(100) NOT NULL,
  [Amount] decimal(18,2) NULL,
  [Unit] nvarchar(100) NOT NULL,
  [Conditions] nvarchar(1000) NOT NULL,
  [Included] nvarchar(1000) NOT NULL,
  [Extra] nvarchar(1000) NOT NULL,
  [Active] bit NOT NULL,
  [From] date NULL,
  [Until] date NULL,
  CONSTRAINT [PK_DiscoveryPrice] PRIMARY KEY ([Id]),
  CONSTRAINT [CK_DSP_Amount] CHECK (Amount IS NULL OR Amount BETWEEN 0 AND 1000000000),
  CONSTRAINT [CK_DSP_Dates] CHECK ([From] IS NULL OR Until IS NULL OR Until>=[From]),
  CONSTRAINT [FK_DiscoveryPrice_EntryId] FOREIGN KEY ([EntryId]) REFERENCES dbo.[DiscoveryEntry] ([Id])
 );
 CREATE INDEX [IX_DSP_Entry] ON dbo.[DiscoveryPrice] ([EntryId]);
END;
IF OBJECT_ID(N'dbo.DiscoveryQuestion',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.[DiscoveryQuestion] (
  [Id] bigint IDENTITY(1,1) NOT NULL,
  [EntryId] bigint NOT NULL,
  [OwnerId] bigint NOT NULL,
  [Question] nvarchar(1000) NOT NULL,
  [Answer] nvarchar(4000) NULL,
  [AnswerSource] nvarchar(1000) NULL,
  [AnsweredBy] bigint NULL,
  [AnsweredAt] datetime2(0) NULL,
  [Status] varchar(20) NOT NULL,
  [SubmissionId] uniqueidentifier NOT NULL,
  [CreatedAt] datetime2(0) NOT NULL,
  [UpdatedAt] datetime2(0) NOT NULL,
  [RowVersion] rowversion NOT NULL,
  CONSTRAINT [PK_DiscoveryQuestion] PRIMARY KEY ([Id]),
  CONSTRAINT [CK_DSQ_Status] CHECK (Status IN ('Pending','Answered','Hidden')),
  CONSTRAINT [CK_DSQ_Answer] CHECK (Status<>'Answered' OR (Answer IS NOT NULL AND LEN(Answer)>0 AND AnsweredBy IS NOT NULL AND AnsweredAt IS NOT NULL)),
  CONSTRAINT [FK_DiscoveryQuestion_EntryId] FOREIGN KEY ([EntryId]) REFERENCES dbo.[DiscoveryEntry] ([Id]),
  CONSTRAINT [FK_DiscoveryQuestion_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES dbo.[TaiKhoan] ([MaTaiKhoan]),
  CONSTRAINT [FK_DiscoveryQuestion_AnsweredBy] FOREIGN KEY ([AnsweredBy]) REFERENCES dbo.[TaiKhoan] ([MaTaiKhoan])
 );
 CREATE INDEX [IX_DSQ_Entry] ON dbo.[DiscoveryQuestion] ([EntryId],[Status]);
 CREATE UNIQUE INDEX [UX_DSQ_Send] ON dbo.[DiscoveryQuestion] ([OwnerId],[SubmissionId]);
END;
IF OBJECT_ID(N'dbo.DiscoveryExperience',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.[DiscoveryExperience] (
  [Id] bigint IDENTITY(1,1) NOT NULL,
  [EntryId] bigint NOT NULL,
  [OwnerId] bigint NOT NULL,
  [Title] nvarchar(150) NOT NULL,
  [Body] nvarchar(2000) NOT NULL,
  [Topics] varchar(200) NOT NULL,
  [VisitedOn] date NOT NULL,
  [Rating] int NOT NULL,
  [Status] varchar(20) NOT NULL,
  [ModeratedBy] bigint NULL,
  [ModerationNote] nvarchar(500) NULL,
  [SubmissionId] uniqueidentifier NOT NULL,
  [CreatedAt] datetime2(0) NOT NULL,
  [UpdatedAt] datetime2(0) NOT NULL,
  [RowVersion] rowversion NOT NULL,
  CONSTRAINT [PK_DiscoveryExperience] PRIMARY KEY ([Id]),
  CONSTRAINT [CK_DSE_Status] CHECK (Status IN ('Pending','Published','Hidden')),
  CONSTRAINT [CK_DSE_Rating] CHECK (Rating BETWEEN 1 AND 5),
  CONSTRAINT [FK_DiscoveryExperience_EntryId] FOREIGN KEY ([EntryId]) REFERENCES dbo.[DiscoveryEntry] ([Id]),
  CONSTRAINT [FK_DiscoveryExperience_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES dbo.[TaiKhoan] ([MaTaiKhoan]),
  CONSTRAINT [FK_DiscoveryExperience_ModeratedBy] FOREIGN KEY ([ModeratedBy]) REFERENCES dbo.[TaiKhoan] ([MaTaiKhoan])
 );
 CREATE INDEX [IX_DSE_Entry] ON dbo.[DiscoveryExperience] ([EntryId],[Status],[VisitedOn]);
 CREATE UNIQUE INDEX [UX_DSE_Send] ON dbo.[DiscoveryExperience] ([OwnerId],[SubmissionId]);
END;
IF OBJECT_ID(N'dbo.DiscoveryReport',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.[DiscoveryReport] (
  [Id] bigint IDENTITY(1,1) NOT NULL,
  [EntryId] bigint NOT NULL,
  [OwnerId] bigint NOT NULL,
  [Category] varchar(30) NOT NULL,
  [Body] nvarchar(2000) NOT NULL,
  [EvidenceUrl] nvarchar(1000) NULL,
  [Status] varchar(20) NOT NULL,
  [Response] nvarchar(1000) NULL,
  [HandledBy] bigint NULL,
  [SubmissionId] uniqueidentifier NOT NULL,
  [CreatedAt] datetime2(0) NOT NULL,
  [UpdatedAt] datetime2(0) NOT NULL,
  [RowVersion] rowversion NOT NULL,
  CONSTRAINT [PK_DiscoveryReport] PRIMARY KEY ([Id]),
  CONSTRAINT [CK_DSR_Status] CHECK (Status IN ('Pending','Checking','Resolved','Rejected')),
  CONSTRAINT [CK_DSR_Category] CHECK (Category IN ('Price','Hours','Address','Accessibility','Closed','Content','Other')),
  CONSTRAINT [CK_DSR_Response] CHECK (Status NOT IN ('Resolved','Rejected') OR (Response IS NOT NULL AND HandledBy IS NOT NULL)),
  CONSTRAINT [FK_DiscoveryReport_EntryId] FOREIGN KEY ([EntryId]) REFERENCES dbo.[DiscoveryEntry] ([Id]),
  CONSTRAINT [FK_DiscoveryReport_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES dbo.[TaiKhoan] ([MaTaiKhoan]),
  CONSTRAINT [FK_DiscoveryReport_HandledBy] FOREIGN KEY ([HandledBy]) REFERENCES dbo.[TaiKhoan] ([MaTaiKhoan])
 );
 CREATE INDEX [IX_DSR_Status] ON dbo.[DiscoveryReport] ([Status],[CreatedAt]);
 CREATE UNIQUE INDEX [UX_DSR_Send] ON dbo.[DiscoveryReport] ([OwnerId],[SubmissionId]);
END;
IF OBJECT_ID(N'dbo.DiscoveryFollow',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.[DiscoveryFollow] (
  [EntryId] bigint NOT NULL,
  [OwnerId] bigint NOT NULL,
  [Active] bit NOT NULL,
  [UpdatedAt] datetime2(0) NOT NULL,
  [RowVersion] rowversion NOT NULL,
  CONSTRAINT [PK_DiscoveryFollow] PRIMARY KEY ([EntryId],[OwnerId]),
  CONSTRAINT [FK_DiscoveryFollow_EntryId] FOREIGN KEY ([EntryId]) REFERENCES dbo.[DiscoveryEntry] ([Id]),
  CONSTRAINT [FK_DiscoveryFollow_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES dbo.[TaiKhoan] ([MaTaiKhoan])
 );
 CREATE INDEX [IX_DSF_Owner] ON dbo.[DiscoveryFollow] ([OwnerId],[Active]);
END;
IF OBJECT_ID(N'dbo.DiscoveryRevision',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.[DiscoveryRevision] (
  [Id] bigint IDENTITY(1,1) NOT NULL,
  [EntryId] bigint NOT NULL,
  [ActorId] bigint NULL,
  [BeforeJson] nvarchar(max) NOT NULL,
  [AfterJson] nvarchar(max) NOT NULL,
  [Summary] nvarchar(500) NOT NULL,
  [Public] bit NOT NULL,
  [CreatedAt] datetime2(0) NOT NULL,
  CONSTRAINT [PK_DiscoveryRevision] PRIMARY KEY ([Id]),
  CONSTRAINT [FK_DiscoveryRevision_EntryId] FOREIGN KEY ([EntryId]) REFERENCES dbo.[DiscoveryEntry] ([Id]),
  CONSTRAINT [FK_DiscoveryRevision_ActorId] FOREIGN KEY ([ActorId]) REFERENCES dbo.[TaiKhoan] ([MaTaiKhoan])
 );
 CREATE INDEX [IX_DSRevision_Entry] ON dbo.[DiscoveryRevision] ([EntryId],[Id]);
END;
IF OBJECT_ID(N'dbo.DiscoveryProposal',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.[DiscoveryProposal] (
  [Id] bigint IDENTITY(1,1) NOT NULL,
  [EntryId] bigint NOT NULL,
  [OwnerId] bigint NOT NULL,
  [BaseVersion] varchar(20) NOT NULL,
  [Payload] nvarchar(max) NOT NULL,
  [Status] varchar(20) NOT NULL,
  [Reason] nvarchar(500) NULL,
  [ReviewedBy] bigint NULL,
  [SubmissionId] uniqueidentifier NOT NULL,
  [CreatedAt] datetime2(0) NOT NULL,
  [UpdatedAt] datetime2(0) NOT NULL,
  [RowVersion] rowversion NOT NULL,
  CONSTRAINT [PK_DiscoveryProposal] PRIMARY KEY ([Id]),
  CONSTRAINT [CK_DSProposal_Status] CHECK (Status IN ('Pending','Accepted','Rejected')),
  CONSTRAINT [FK_DiscoveryProposal_EntryId] FOREIGN KEY ([EntryId]) REFERENCES dbo.[DiscoveryEntry] ([Id]),
  CONSTRAINT [FK_DiscoveryProposal_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES dbo.[TaiKhoan] ([MaTaiKhoan]),
  CONSTRAINT [FK_DiscoveryProposal_ReviewedBy] FOREIGN KEY ([ReviewedBy]) REFERENCES dbo.[TaiKhoan] ([MaTaiKhoan])
 );
 CREATE INDEX [IX_DSProposal_Status] ON dbo.[DiscoveryProposal] ([Status],[CreatedAt]);
 CREATE UNIQUE INDEX [UX_DSProposal_Send] ON dbo.[DiscoveryProposal] ([OwnerId],[SubmissionId]);
END;
IF OBJECT_ID(N'dbo.DiscoveryCollection',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.[DiscoveryCollection] (
  [Id] bigint IDENTITY(1,1) NOT NULL,
  [OwnerId] bigint NOT NULL,
  [Name] nvarchar(100) NOT NULL,
  [Note] nvarchar(500) NULL,
  [Archived] bit NOT NULL,
  [UpdatedAt] datetime2(0) NOT NULL,
  [RowVersion] rowversion NOT NULL,
  CONSTRAINT [PK_DiscoveryCollection] PRIMARY KEY ([Id]),
  CONSTRAINT [FK_DiscoveryCollection_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES dbo.[TaiKhoan] ([MaTaiKhoan])
 );
 CREATE INDEX [IX_DSCollection_Owner] ON dbo.[DiscoveryCollection] ([OwnerId],[Archived]);
END;
IF OBJECT_ID(N'dbo.DiscoveryCollectionItem',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.[DiscoveryCollectionItem] (
  [CollectionId] bigint NOT NULL,
  [EntryId] bigint NOT NULL,
  [Note] nvarchar(500) NULL,
  [Removed] bit NOT NULL,
  [UpdatedAt] datetime2(0) NOT NULL,
  [RowVersion] rowversion NOT NULL,
  CONSTRAINT [PK_DiscoveryCollectionItem] PRIMARY KEY ([CollectionId],[EntryId]),
  CONSTRAINT [FK_DiscoveryCollectionItem_CollectionId] FOREIGN KEY ([CollectionId]) REFERENCES dbo.[DiscoveryCollection] ([Id]),
  CONSTRAINT [FK_DiscoveryCollectionItem_EntryId] FOREIGN KEY ([EntryId]) REFERENCES dbo.[DiscoveryEntry] ([Id])
 );
 CREATE INDEX [IX_DSCollectionItem_Entry] ON dbo.[DiscoveryCollectionItem] ([EntryId]);
END;
IF OBJECT_ID(N'dbo.DiscoveryRoute',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.[DiscoveryRoute] (
  [Id] bigint IDENTITY(1,1) NOT NULL,
  [OwnerId] bigint NOT NULL,
  [Name] nvarchar(100) NOT NULL,
  [PointsJson] nvarchar(2000) NOT NULL,
  [Mode] varchar(12) NOT NULL,
  [Archived] bit NOT NULL,
  [SubmissionId] uniqueidentifier NOT NULL,
  [UpdatedAt] datetime2(0) NOT NULL,
  [RowVersion] rowversion NOT NULL,
  CONSTRAINT [PK_DiscoveryRoute] PRIMARY KEY ([Id]),
  CONSTRAINT [CK_DSRoute_Mode] CHECK (Mode IN ('car','foot','bike')),
  CONSTRAINT [FK_DiscoveryRoute_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES dbo.[TaiKhoan] ([MaTaiKhoan])
 );
 CREATE INDEX [IX_DSRoute_Owner] ON dbo.[DiscoveryRoute] ([OwnerId],[Archived]);
 CREATE UNIQUE INDEX [UX_DSRoute_Send] ON dbo.[DiscoveryRoute] ([OwnerId],[SubmissionId]);
END;
IF DATABASE_PRINCIPAL_ID(N'r_ungdung') IS NOT NULL
BEGIN
 DENY DELETE ON OBJECT::dbo.[DiscoveryEntry] TO [r_ungdung];
 DENY DELETE ON OBJECT::dbo.[DiscoveryFeature] TO [r_ungdung];
 DENY DELETE ON OBJECT::dbo.[DiscoveryPrice] TO [r_ungdung];
 DENY DELETE ON OBJECT::dbo.[DiscoveryQuestion] TO [r_ungdung];
 DENY DELETE ON OBJECT::dbo.[DiscoveryExperience] TO [r_ungdung];
 DENY DELETE ON OBJECT::dbo.[DiscoveryReport] TO [r_ungdung];
 DENY DELETE ON OBJECT::dbo.[DiscoveryFollow] TO [r_ungdung];
 DENY DELETE ON OBJECT::dbo.[DiscoveryRevision] TO [r_ungdung];
 DENY DELETE ON OBJECT::dbo.[DiscoveryProposal] TO [r_ungdung];
 DENY DELETE ON OBJECT::dbo.[DiscoveryCollection] TO [r_ungdung];
 DENY DELETE ON OBJECT::dbo.[DiscoveryCollectionItem] TO [r_ungdung];
 DENY DELETE ON OBJECT::dbo.[DiscoveryRoute] TO [r_ungdung];
 DENY UPDATE ON OBJECT::dbo.DiscoveryRevision TO [r_ungdung];
END;
COMMIT TRANSACTION;
