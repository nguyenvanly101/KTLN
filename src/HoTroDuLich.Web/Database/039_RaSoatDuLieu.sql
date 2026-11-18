-- Ra soat du lieu (chay lai an toan, moi buoc tu kiem tra):
--  1. Ten dich vu / co so go nham giu Shift ("HOàng GIa") -> sua; tu nay TextClean.Name chuan hoa khi luu.
--  2. Chi muc cho cac cot khoa ngoai hay duoc tra cuu nhung chua co chi muc.
SET ANSI_NULLS ON; SET QUOTED_IDENTIFIER ON; SET XACT_ABORT ON; SET NOCOUNT ON;
IF OBJECT_ID(N'dbo.DichVuDuLich',N'U') IS NOT NULL
BEGIN
 UPDATE dbo.DichVuDuLich SET TenDichVu=N'Khách Sạn Hoàng Gia' WHERE TenDichVu=N'Khách Sạn HOàng GIa' COLLATE Vietnamese_CS_AS;
 IF OBJECT_ID(N'dbo.BanCapNhatDichVu',N'U') IS NOT NULL
  UPDATE dbo.BanCapNhatDichVu SET TenDichVu=N'Khách Sạn Hoàng Gia' WHERE TenDichVu=N'Khách Sạn HOàng GIa' COLLATE Vietnamese_CS_AS;
END
IF OBJECT_ID(N'dbo.NhomChatChuyenDi',N'U') IS NOT NULL AND COL_LENGTH(N'dbo.NhomChatChuyenDi',N'ConversationId') IS NOT NULL
 AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.NhomChatChuyenDi') AND name=N'IX_NhomChatChuyenDi_Conversation')
 CREATE INDEX IX_NhomChatChuyenDi_Conversation ON dbo.NhomChatChuyenDi(ConversationId);
IF OBJECT_ID(N'dbo.BaoCaoChat',N'U') IS NOT NULL
 AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.BaoCaoChat') AND name=N'IX_BaoCaoChat_Status')
 CREATE INDEX IX_BaoCaoChat_Status ON dbo.BaoCaoChat(Status,ConversationId);
IF OBJECT_ID(N'dbo.CamXuc',N'U') IS NOT NULL AND COL_LENGTH(N'dbo.CamXuc',N'AccountId') IS NOT NULL
 AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.CamXuc') AND name=N'IX_CamXuc_Account')
 CREATE INDEX IX_CamXuc_Account ON dbo.CamXuc(AccountId);
IF OBJECT_ID(N'dbo.BaoCaoBaiViet',N'U') IS NOT NULL
 AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.BaoCaoBaiViet') AND name=N'IX_BaoCaoBaiViet_Status')
 CREATE INDEX IX_BaoCaoBaiViet_Status ON dbo.BaoCaoBaiViet(Status);
-- 3. Muc mau (demo-*) trung voi dia diem that co ten dai hon o gan (vd "Bà Nà Hills" va "Bà Nà Hills và Cầu Vàng", cach duoi 1 km): an muc mau.
IF OBJECT_ID(N'dbo.DiscoveryEntry',N'U') IS NOT NULL AND OBJECT_ID(N'dbo.DonDuLieuSaoLuu',N'U') IS NOT NULL
BEGIN
 DECLARE @demo TABLE(Id bigint PRIMARY KEY);
 INSERT @demo SELECT a.Id FROM dbo.DiscoveryEntry a WHERE a.Status=N'Published' AND a.Slug LIKE N'demo-%' AND a.PlaceId IS NULL AND a.Kind=N'Place' AND a.Latitude IS NOT NULL
  AND EXISTS (SELECT 1 FROM dbo.DiscoveryEntry b WHERE b.Id<>a.Id AND b.Status=N'Published' AND b.PlaceId IS NOT NULL AND b.Latitude IS NOT NULL
   AND b.Title COLLATE Vietnamese_CI_AI LIKE a.Title COLLATE Vietnamese_CI_AI+N'%'
   AND SQRT(SQUARE((CAST(a.Latitude AS float)-CAST(b.Latitude AS float))*111.0)+SQUARE((CAST(a.Longitude AS float)-CAST(b.Longitude AS float))*111.0*COS(RADIANS(CAST(a.Latitude AS float)))))<1.0);
 INSERT dbo.DonDuLieuSaoLuu(Bang,Ma,Cot,GiaTriCu,LyDo) SELECT N'DiscoveryEntry',Id,N'Status',N'Published',N'Mục mẫu trùng địa điểm đã có' FROM @demo;
 UPDATE e SET Status=N'Hidden' FROM dbo.DiscoveryEntry e JOIN @demo d ON d.Id=e.Id;
END
