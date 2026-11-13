-- Mang xa hoi kieu Facebook: ket ban, bai viet, binh luan, cam xuc, chat nhom. Chay lap an toan, khong xoa du lieu.
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
EXEC @lock=sys.sp_getapplock @Resource=N'HoTroDuLich.MangXaHoi.v1',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=30000;
IF @lock<0 THROW 51000,N'Không lấy được khóa nâng cấp mạng xã hội.',1;

IF OBJECT_ID(N'dbo.KetBan',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.KetBan(
  Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_KetBan PRIMARY KEY,
  RequesterId bigint NOT NULL CONSTRAINT FK_KetBan_Req FOREIGN KEY REFERENCES dbo.TaiKhoan(MaTaiKhoan),
  AddresseeId bigint NOT NULL CONSTRAINT FK_KetBan_Add FOREIGN KEY REFERENCES dbo.TaiKhoan(MaTaiKhoan),
  Status varchar(10) NOT NULL CONSTRAINT DF_KetBan_Status DEFAULT 'Pending',
  CreatedAt datetime2(0) NOT NULL CONSTRAINT DF_KetBan_Created DEFAULT SYSUTCDATETIME(),
  RespondedAt datetime2(0) NULL,
  LowId AS (CASE WHEN RequesterId<AddresseeId THEN RequesterId ELSE AddresseeId END) PERSISTED,
  HighId AS (CASE WHEN RequesterId<AddresseeId THEN AddresseeId ELSE RequesterId END) PERSISTED,
  CONSTRAINT CK_KetBan_Status CHECK (Status IN ('Pending','Accepted','Declined')),
  CONSTRAINT CK_KetBan_Self CHECK (RequesterId<>AddresseeId));
 CREATE UNIQUE INDEX UX_KetBan_Pair ON dbo.KetBan(LowId,HighId);
 CREATE INDEX IX_KetBan_Addressee ON dbo.KetBan(AddresseeId,Status);
 CREATE INDEX IX_KetBan_Requester ON dbo.KetBan(RequesterId,Status);
END;

IF OBJECT_ID(N'dbo.BaiViet',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.BaiViet(
  Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_BaiViet PRIMARY KEY,
  AuthorId bigint NOT NULL CONSTRAINT FK_BaiViet_Author FOREIGN KEY REFERENCES dbo.TaiKhoan(MaTaiKhoan),
  Content nvarchar(4000) NOT NULL CONSTRAINT DF_BaiViet_Content DEFAULT N'',
  Visibility varchar(10) NOT NULL CONSTRAINT DF_BaiViet_Vis DEFAULT 'Public',
  PhotosJson nvarchar(1500) NULL,
  CreatedAt datetime2(0) NOT NULL CONSTRAINT DF_BaiViet_Created DEFAULT SYSUTCDATETIME(),
  UpdatedAt datetime2(0) NULL,
  DeletedAt datetime2(0) NULL,
  CONSTRAINT CK_BaiViet_Vis CHECK (Visibility IN ('Public','Friends','Private')),
  CONSTRAINT CK_BaiViet_Content CHECK (LEN(Content)>0 OR PhotosJson IS NOT NULL));
 CREATE INDEX IX_BaiViet_Author ON dbo.BaiViet(AuthorId,CreatedAt DESC) WHERE DeletedAt IS NULL;
 CREATE INDEX IX_BaiViet_Feed ON dbo.BaiViet(CreatedAt DESC) WHERE DeletedAt IS NULL;
END;

IF OBJECT_ID(N'dbo.BinhLuan',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.BinhLuan(
  Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_BinhLuan PRIMARY KEY,
  PostId bigint NOT NULL CONSTRAINT FK_BinhLuan_Post FOREIGN KEY REFERENCES dbo.BaiViet(Id),
  AuthorId bigint NOT NULL CONSTRAINT FK_BinhLuan_Author FOREIGN KEY REFERENCES dbo.TaiKhoan(MaTaiKhoan),
  ParentId bigint NULL CONSTRAINT FK_BinhLuan_Parent FOREIGN KEY REFERENCES dbo.BinhLuan(Id),
  Content nvarchar(1500) NOT NULL,
  CreatedAt datetime2(0) NOT NULL CONSTRAINT DF_BinhLuan_Created DEFAULT SYSUTCDATETIME(),
  UpdatedAt datetime2(0) NULL,
  DeletedAt datetime2(0) NULL,
  CONSTRAINT CK_BinhLuan_Content CHECK (LEN(Content)>0));
 CREATE INDEX IX_BinhLuan_Post ON dbo.BinhLuan(PostId,Id) WHERE DeletedAt IS NULL;
END;

IF OBJECT_ID(N'dbo.CamXuc',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.CamXuc(
  Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_CamXuc PRIMARY KEY,
  TargetType varchar(10) NOT NULL,
  TargetId bigint NOT NULL,
  AccountId bigint NOT NULL CONSTRAINT FK_CamXuc_Account FOREIGN KEY REFERENCES dbo.TaiKhoan(MaTaiKhoan),
  Emoji varchar(10) NOT NULL CONSTRAINT DF_CamXuc_Emoji DEFAULT 'like',
  CreatedAt datetime2(0) NOT NULL CONSTRAINT DF_CamXuc_Created DEFAULT SYSUTCDATETIME(),
  CONSTRAINT CK_CamXuc_Type CHECK (TargetType IN ('Post','Comment')),
  CONSTRAINT CK_CamXuc_Emoji CHECK (Emoji IN ('like','love','haha','wow','sad','angry')),
  CONSTRAINT UX_CamXuc UNIQUE (TargetType,TargetId,AccountId));
 CREATE INDEX IX_CamXuc_Target ON dbo.CamXuc(TargetType,TargetId);
END;

-- Chat nhom
IF OBJECT_ID(N'dbo.CuocTroChuyen',N'U') IS NOT NULL
BEGIN
 IF COL_LENGTH(N'dbo.CuocTroChuyen',N'IsGroup') IS NULL
  EXEC sys.sp_executesql N'ALTER TABLE dbo.CuocTroChuyen ADD IsGroup bit NOT NULL CONSTRAINT DF_Chat_IsGroup DEFAULT 0;';
 IF COL_LENGTH(N'dbo.CuocTroChuyen',N'Title') IS NULL
  EXEC sys.sp_executesql N'ALTER TABLE dbo.CuocTroChuyen ADD Title nvarchar(100) NULL;';
 IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name=N'CK_Chat_Kind' AND definition NOT LIKE N'%IsGroup%')
  EXEC sys.sp_executesql N'ALTER TABLE dbo.CuocTroChuyen DROP CONSTRAINT CK_Chat_Kind;';
 IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name=N'CK_Chat_Kind')
  EXEC sys.sp_executesql N'ALTER TABLE dbo.CuocTroChuyen ADD CONSTRAINT CK_Chat_Kind CHECK ((IsGroup=0 AND BranchId IS NOT NULL AND PeerId IS NULL) OR (IsGroup=0 AND BranchId IS NULL AND PeerId IS NOT NULL AND PeerId<>TravelerId) OR (IsGroup=1 AND BranchId IS NULL AND PeerId IS NULL AND Title IS NOT NULL));';
END;
IF OBJECT_ID(N'dbo.TinNhanChat',N'U') IS NOT NULL AND EXISTS (SELECT 1 FROM sys.check_constraints WHERE name=N'CK_Msg_Side' AND definition NOT LIKE N'%Member%')
BEGIN
 EXEC sys.sp_executesql N'ALTER TABLE dbo.TinNhanChat DROP CONSTRAINT CK_Msg_Side;';
 EXEC sys.sp_executesql N'ALTER TABLE dbo.TinNhanChat ADD CONSTRAINT CK_Msg_Side CHECK (Side IN (''Traveler'',''Branch'',''System'',''Member''));';
END;
IF OBJECT_ID(N'dbo.ThanhVienChat',N'U') IS NULL AND OBJECT_ID(N'dbo.CuocTroChuyen',N'U') IS NOT NULL
BEGIN
 CREATE TABLE dbo.ThanhVienChat(
  ConversationId bigint NOT NULL CONSTRAINT FK_TVChat_Conv FOREIGN KEY REFERENCES dbo.CuocTroChuyen(Id),
  AccountId bigint NOT NULL CONSTRAINT FK_TVChat_Account FOREIGN KEY REFERENCES dbo.TaiKhoan(MaTaiKhoan),
  Role varchar(10) NOT NULL CONSTRAINT DF_TVChat_Role DEFAULT 'Member',
  ReadId bigint NOT NULL CONSTRAINT DF_TVChat_Read DEFAULT 0,
  JoinedAt datetime2(0) NOT NULL CONSTRAINT DF_TVChat_Joined DEFAULT SYSUTCDATETIME(),
  LeftAt datetime2(0) NULL,
  CONSTRAINT PK_TVChat PRIMARY KEY (ConversationId,AccountId),
  CONSTRAINT CK_TVChat_Role CHECK (Role IN ('Admin','Member')));
 CREATE INDEX IX_TVChat_Account ON dbo.ThanhVienChat(AccountId) WHERE LeftAt IS NULL;
END;
COMMIT TRANSACTION;
