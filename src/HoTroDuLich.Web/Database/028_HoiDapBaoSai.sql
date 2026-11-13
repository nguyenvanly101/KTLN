-- Hoi dap theo diem den va bao sai thong tin co so. Chay lap an toan.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
DECLARE @lock int;
EXEC @lock=sys.sp_getapplock @Resource=N'HoTroDuLich.HoiDapBaoSai.v1',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=30000;
IF @lock<0 THROW 51000,N'Không lấy được khóa nâng cấp hỏi đáp.',1;
IF OBJECT_ID(N'dbo.CauHoi',N'U') IS NULL AND OBJECT_ID(N'dbo.DonViHanhChinh',N'U') IS NOT NULL
 EXEC sys.sp_executesql N'CREATE TABLE dbo.CauHoi(
  Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_CauHoi PRIMARY KEY,
  DonViId bigint NOT NULL CONSTRAINT FK_CauHoi_DonVi FOREIGN KEY REFERENCES dbo.DonViHanhChinh(Id),
  AuthorId bigint NOT NULL CONSTRAINT FK_CauHoi_Author FOREIGN KEY REFERENCES dbo.TaiKhoan(MaTaiKhoan),
  Title nvarchar(200) NOT NULL,
  Body nvarchar(2000) NULL,
  AcceptedAnswerId bigint NULL,
  CreatedAt datetime2(0) NOT NULL CONSTRAINT DF_CauHoi_Created DEFAULT SYSUTCDATETIME(),
  DeletedAt datetime2(0) NULL);
  CREATE INDEX IX_CauHoi_DonVi ON dbo.CauHoi(DonViId,Id DESC);';
IF OBJECT_ID(N'dbo.TraLoi',N'U') IS NULL AND OBJECT_ID(N'dbo.CauHoi',N'U') IS NOT NULL
 EXEC sys.sp_executesql N'CREATE TABLE dbo.TraLoi(
  Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_TraLoi PRIMARY KEY,
  QuestionId bigint NOT NULL CONSTRAINT FK_TraLoi_Q FOREIGN KEY REFERENCES dbo.CauHoi(Id),
  AuthorId bigint NOT NULL CONSTRAINT FK_TraLoi_Author FOREIGN KEY REFERENCES dbo.TaiKhoan(MaTaiKhoan),
  Body nvarchar(2000) NOT NULL,
  CreatedAt datetime2(0) NOT NULL CONSTRAINT DF_TraLoi_Created DEFAULT SYSUTCDATETIME(),
  DeletedAt datetime2(0) NULL);
  CREATE INDEX IX_TraLoi_Q ON dbo.TraLoi(QuestionId,Id);';
IF OBJECT_ID(N'dbo.BinhChonTraLoi',N'U') IS NULL AND OBJECT_ID(N'dbo.TraLoi',N'U') IS NOT NULL
 EXEC sys.sp_executesql N'CREATE TABLE dbo.BinhChonTraLoi(
  AnswerId bigint NOT NULL CONSTRAINT FK_BcTl_A FOREIGN KEY REFERENCES dbo.TraLoi(Id),
  AccountId bigint NOT NULL CONSTRAINT FK_BcTl_Acc FOREIGN KEY REFERENCES dbo.TaiKhoan(MaTaiKhoan),
  CreatedAt datetime2(0) NOT NULL CONSTRAINT DF_BcTl_Created DEFAULT SYSUTCDATETIME(),
  CONSTRAINT PK_BinhChonTraLoi PRIMARY KEY (AnswerId,AccountId));';
IF OBJECT_ID(N'dbo.BaoSaiCoSo',N'U') IS NULL AND OBJECT_ID(N'dbo.TaiKhoan',N'U') IS NOT NULL
 EXEC sys.sp_executesql N'CREATE TABLE dbo.BaoSaiCoSo(
  Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_BaoSaiCoSo PRIMARY KEY,
  BranchId bigint NOT NULL,
  ReporterId bigint NOT NULL CONSTRAINT FK_BaoSai_Rep FOREIGN KEY REFERENCES dbo.TaiKhoan(MaTaiKhoan),
  Field varchar(12) NOT NULL,
  Detail nvarchar(500) NOT NULL,
  Status varchar(10) NOT NULL CONSTRAINT DF_BaoSai_Status DEFAULT ''Open'',
  ResponseNote nvarchar(300) NULL,
  CreatedAt datetime2(0) NOT NULL CONSTRAINT DF_BaoSai_Created DEFAULT SYSUTCDATETIME(),
  ResolvedAt datetime2(0) NULL,
  CONSTRAINT CK_BaoSai_Field CHECK (Field IN (''Gio'',''Gia'',''DiaChi'',''DaDong'',''LienHe'',''Khac'')),
  CONSTRAINT CK_BaoSai_Status CHECK (Status IN (''Open'',''Fixed'',''Rejected'')));
  CREATE INDEX IX_BaoSai_Branch ON dbo.BaoSaiCoSo(BranchId,Status,Id DESC);';
COMMIT TRANSACTION;
