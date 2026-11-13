-- Lich trinh mau (quan tri soan, khach chep ve ke hoach cua minh). Chay lap an toan.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
DECLARE @lock int;
EXEC @lock=sys.sp_getapplock @Resource=N'HoTroDuLich.LichMau.v1',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=30000;
IF @lock<0 THROW 51000,N'Không lấy được khóa nâng cấp lịch mẫu.',1;
IF OBJECT_ID(N'dbo.LichTrinhMau',N'U') IS NULL AND OBJECT_ID(N'dbo.DonViHanhChinh',N'U') IS NOT NULL
 EXEC sys.sp_executesql N'CREATE TABLE dbo.LichTrinhMau(
  Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_LichTrinhMau PRIMARY KEY,
  DonViId bigint NOT NULL CONSTRAINT FK_LichMau_DonVi FOREIGN KEY REFERENCES dbo.DonViHanhChinh(Id),
  Title nvarchar(150) NOT NULL,
  Days int NOT NULL,
  Style varchar(12) NOT NULL CONSTRAINT DF_LichMau_Style DEFAULT ''CanBang'',
  Summary nvarchar(600) NULL,
  Status varchar(10) NOT NULL CONSTRAINT DF_LichMau_Status DEFAULT ''Draft'',
  Uses int NOT NULL CONSTRAINT DF_LichMau_Uses DEFAULT 0,
  CreatedAt datetime2(0) NOT NULL CONSTRAINT DF_LichMau_Created DEFAULT SYSUTCDATETIME(),
  UpdatedAt datetime2(0) NOT NULL CONSTRAINT DF_LichMau_Updated DEFAULT SYSUTCDATETIME(),
  CreatedBy bigint NOT NULL,
  CONSTRAINT CK_LichMau_Days CHECK (Days BETWEEN 1 AND 14),
  CONSTRAINT CK_LichMau_Style CHECK (Style IN (''TietKiem'',''CanBang'',''TraiNghiem'')),
  CONSTRAINT CK_LichMau_Status CHECK (Status IN (''Draft'',''Published'')));
  CREATE INDEX IX_LichMau_DonVi ON dbo.LichTrinhMau(DonViId,Status);';
IF OBJECT_ID(N'dbo.LichTrinhMauMuc',N'U') IS NULL AND OBJECT_ID(N'dbo.LichTrinhMau',N'U') IS NOT NULL
 EXEC sys.sp_executesql N'CREATE TABLE dbo.LichTrinhMauMuc(
  Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_LichTrinhMauMuc PRIMARY KEY,
  TemplateId bigint NOT NULL CONSTRAINT FK_LichMauMuc_Tpl FOREIGN KEY REFERENCES dbo.LichTrinhMau(Id),
  DayNo int NOT NULL,
  SortOrder int NOT NULL,
  [Time] time(0) NULL,
  Content nvarchar(300) NOT NULL,
  Cost decimal(18,0) NULL,
  ServiceId bigint NULL,
  PlaceId bigint NULL);
  CREATE INDEX IX_LichMauMuc_Tpl ON dbo.LichTrinhMauMuc(TemplateId,DayNo,SortOrder);';
COMMIT TRANSACTION;
