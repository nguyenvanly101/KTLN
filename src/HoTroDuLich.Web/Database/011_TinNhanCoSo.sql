-- Tin nhan truc tiep giua khach va co so (giong hop thu cua Trang tren mang xa hoi). Chay lap an toan, khong xoa du lieu.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
DECLARE @lock int;
EXEC @lock=sys.sp_getapplock @Resource=N'HoTroDuLich.TinNhanCoSo.v1',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=30000;
IF @lock<0 THROW 51000,N'Không lấy được khóa nâng cấp tin nhắn.',1;

IF OBJECT_ID(N'dbo.CuocTroChuyen',N'U') IS NULL
CREATE TABLE dbo.CuocTroChuyen(
 Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_CuocTroChuyen PRIMARY KEY,
 BranchId bigint NOT NULL CONSTRAINT FK_Chat_Branch FOREIGN KEY REFERENCES dbo.CoSoDichVu(Id),
 TravelerId bigint NOT NULL CONSTRAINT FK_Chat_Traveler FOREIGN KEY REFERENCES dbo.TaiKhoan(MaTaiKhoan),
 State varchar(10) NOT NULL CONSTRAINT DF_Chat_State DEFAULT 'Open',
 Label nvarchar(30) NULL,
 InternalNote nvarchar(500) NULL,
 LastMessageId bigint NOT NULL CONSTRAINT DF_Chat_Last DEFAULT 0,
 LastMessageAt datetime2(3) NOT NULL CONSTRAINT DF_Chat_LastAt DEFAULT SYSUTCDATETIME(),
 LastPreview nvarchar(120) NULL,
 LastFromTraveler bit NOT NULL CONSTRAINT DF_Chat_LastFrom DEFAULT 1,
 TravelerReadId bigint NOT NULL CONSTRAINT DF_Chat_TRead DEFAULT 0,
 BranchReadId bigint NOT NULL CONSTRAINT DF_Chat_BRead DEFAULT 0,
 TravelerReadAt datetime2(3) NULL,
 BranchReadAt datetime2(3) NULL,
 CreatedAt datetime2(3) NOT NULL CONSTRAINT DF_Chat_Created DEFAULT SYSUTCDATETIME(),
 CONSTRAINT CK_Chat_State CHECK (State IN ('Open','Done')),
 CONSTRAINT UX_Chat_Pair UNIQUE (BranchId,TravelerId));
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_Chat_Traveler' AND object_id=OBJECT_ID(N'dbo.CuocTroChuyen'))
 CREATE INDEX IX_Chat_Traveler ON dbo.CuocTroChuyen(TravelerId,LastMessageAt DESC);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_Chat_Branch' AND object_id=OBJECT_ID(N'dbo.CuocTroChuyen'))
 CREATE INDEX IX_Chat_Branch ON dbo.CuocTroChuyen(BranchId,State,LastMessageAt DESC);

IF OBJECT_ID(N'dbo.TinNhanChat',N'U') IS NULL
CREATE TABLE dbo.TinNhanChat(
 Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_TinNhanChat PRIMARY KEY,
 ConversationId bigint NOT NULL CONSTRAINT FK_Msg_Chat FOREIGN KEY REFERENCES dbo.CuocTroChuyen(Id),
 SenderId bigint NULL CONSTRAINT FK_Msg_Sender FOREIGN KEY REFERENCES dbo.TaiKhoan(MaTaiKhoan),
 Side varchar(10) NOT NULL,
 Kind varchar(10) NOT NULL CONSTRAINT DF_Msg_Kind DEFAULT 'Text',
 Body nvarchar(2000) NOT NULL CONSTRAINT DF_Msg_Body DEFAULT N'',
 ImagePath nvarchar(300) NULL,
 CreatedAt datetime2(3) NOT NULL CONSTRAINT DF_Msg_Created DEFAULT SYSUTCDATETIME(),
 CONSTRAINT CK_Msg_Side CHECK (Side IN ('Traveler','Branch','System')),
 CONSTRAINT CK_Msg_Kind CHECK (Kind IN ('Text','Image','Auto')),
 CONSTRAINT CK_Msg_Content CHECK (LEN(Body)>0 OR ImagePath IS NOT NULL));
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_Msg_Chat' AND object_id=OBJECT_ID(N'dbo.TinNhanChat'))
 CREATE INDEX IX_Msg_Chat ON dbo.TinNhanChat(ConversationId,Id);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_Msg_Sender' AND object_id=OBJECT_ID(N'dbo.TinNhanChat'))
 CREATE INDEX IX_Msg_Sender ON dbo.TinNhanChat(SenderId,CreatedAt) WHERE SenderId IS NOT NULL;

IF OBJECT_ID(N'dbo.TinMauChat',N'U') IS NULL
CREATE TABLE dbo.TinMauChat(
 Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_TinMauChat PRIMARY KEY,
 BranchId bigint NOT NULL CONSTRAINT FK_Quick_Branch FOREIGN KEY REFERENCES dbo.CoSoDichVu(Id),
 Title nvarchar(40) NOT NULL,
 Body nvarchar(1000) NOT NULL,
 CreatedAt datetime2(0) NOT NULL CONSTRAINT DF_Quick_Created DEFAULT SYSUTCDATETIME());
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_Quick_Branch' AND object_id=OBJECT_ID(N'dbo.TinMauChat'))
 CREATE INDEX IX_Quick_Branch ON dbo.TinMauChat(BranchId);

IF OBJECT_ID(N'dbo.CaiDatChatCoSo',N'U') IS NULL
CREATE TABLE dbo.CaiDatChatCoSo(
 BranchId bigint NOT NULL CONSTRAINT PK_ChatSetting PRIMARY KEY CONSTRAINT FK_ChatSetting_Branch FOREIGN KEY REFERENCES dbo.CoSoDichVu(Id),
 Accepting bit NOT NULL CONSTRAINT DF_ChatSetting_On DEFAULT 1,
 ReplyWithin varchar(10) NOT NULL CONSTRAINT DF_ChatSetting_Eta DEFAULT 'Hour',
 AutoReply nvarchar(500) NULL,
 AutoReplyOffHours bit NOT NULL CONSTRAINT DF_ChatSetting_Off DEFAULT 1,
 Greeting nvarchar(300) NULL,
 UpdatedAt datetime2(0) NOT NULL CONSTRAINT DF_ChatSetting_Upd DEFAULT SYSUTCDATETIME(),
 CONSTRAINT CK_ChatSetting_Eta CHECK (ReplyWithin IN ('Minutes','Hour','Day')));
COMMIT TRANSACTION;
