-- Don du lieu Kham pha (chay mot lan, danh dau bang extended property HanhTrinh.DonDuLieu = v1).
-- Chi AN, khong xoa: dia diem DangHienThi = 0, muc tra cuu Status = 'Hidden', co so mau Status = 'Hidden'.
-- Gia tri cu duoc luu vao bang dbo.DonDuLieuSaoLuu de khoi phuc khi can.
--  1. Co so "(mau)" do che do demo tao (ghi ro "Khong phai don vi that").
--  2. Muc trung that: cung ten (khong phan biet hoa thuong, dau) va cach nhau duoi 3 km. Giu muc goc, an ban thua.
--     Cung ten nhung khac noi (vd Bai bien My Khe Da Nang va Quang Ngai) thi giu ca hai.
--  3. Ten chung chung hoac khong phai diem tham quan (quang cao, van phong, dai ly tour).
--  4. Ten viet HOA toan bo: doi sang chu thuong co viet hoa dau tu.
SET ANSI_NULLS ON; SET QUOTED_IDENTIFIER ON; SET XACT_ABORT ON; SET NOCOUNT ON;
IF OBJECT_ID(N'dbo.DiscoveryEntry',N'U') IS NOT NULL AND OBJECT_ID(N'dbo.DiaDiemDuLich',N'U') IS NOT NULL AND OBJECT_ID(N'dbo.CoSoDichVu',N'U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM sys.extended_properties WHERE class=0 AND name=N'HanhTrinh.DonDuLieu' AND CAST(value AS nvarchar(20))=N'v1')
BEGIN
 BEGIN TRANSACTION;
 IF OBJECT_ID(N'dbo.DonDuLieuSaoLuu',N'U') IS NULL
  CREATE TABLE dbo.DonDuLieuSaoLuu(Id bigint IDENTITY PRIMARY KEY,Bang nvarchar(50) NOT NULL,Ma bigint NOT NULL,Cot nvarchar(50) NOT NULL,GiaTriCu nvarchar(400) NULL,LyDo nvarchar(200) NOT NULL,ThoiDiem datetime2 NOT NULL DEFAULT SYSUTCDATETIME());

 -- 1. Co so mau
 DECLARE @branch TABLE(Id bigint PRIMARY KEY);
 INSERT @branch SELECT Id FROM dbo.CoSoDichVu WHERE Name LIKE N'%(mẫu)' AND Summary LIKE N'%Không phải đơn vị thật%' AND Status<>N'Hidden';
 INSERT dbo.DonDuLieuSaoLuu(Bang,Ma,Cot,GiaTriCu,LyDo) SELECT N'CoSoDichVu',b.Id,N'Status',b.Status,N'Cơ sở mẫu' FROM dbo.CoSoDichVu b JOIN @branch x ON x.Id=b.Id;
 UPDATE b SET Status=N'Hidden' FROM dbo.CoSoDichVu b JOIN @branch x ON x.Id=b.Id;
 INSERT dbo.DonDuLieuSaoLuu(Bang,Ma,Cot,GiaTriCu,LyDo) SELECT N'DiscoveryEntry',e.Id,N'Status',e.Status,N'Cơ sở mẫu' FROM dbo.DiscoveryEntry e JOIN @branch x ON x.Id=e.BranchId WHERE e.Status<>N'Hidden';
 UPDATE e SET Status=N'Hidden' FROM dbo.DiscoveryEntry e JOIN @branch x ON x.Id=e.BranchId;

 -- Muc dang hien kem toa do (de so trung)
 DECLARE @live TABLE(Id bigint PRIMARY KEY,PlaceId bigint NULL,Title nvarchar(300),Lat float NULL,Lng float NULL,Demo bit);
 INSERT @live SELECT e.Id,e.PlaceId,e.Title,CAST(e.Latitude AS float),CAST(e.Longitude AS float),CASE WHEN e.Slug LIKE N'demo-%' AND e.PlaceId IS NULL THEN 1 ELSE 0 END
  FROM dbo.DiscoveryEntry e WHERE e.Status=N'Published' AND e.Kind=N'Place' AND (e.PlaceId IS NULL OR EXISTS (SELECT 1 FROM dbo.DiaDiemDuLich p WHERE p.MaDiaDiem=e.PlaceId AND p.DangHienThi=1));

 -- 2a. Muc mau (khong gan dia diem) trung voi dia diem that gan do: an muc mau
 DECLARE @hideEntry TABLE(Id bigint PRIMARY KEY,LyDo nvarchar(200));
 INSERT @hideEntry SELECT DISTINCT a.Id,N'Trùng với địa điểm đã có' FROM @live a JOIN @live b ON b.Id<>a.Id AND b.Demo=0 AND b.PlaceId IS NOT NULL
  AND a.Title COLLATE Vietnamese_CI_AI=b.Title COLLATE Vietnamese_CI_AI
  AND a.Lat IS NOT NULL AND b.Lat IS NOT NULL AND SQRT(SQUARE((a.Lat-b.Lat)*111.0)+SQUARE((a.Lng-b.Lng)*111.0*COS(RADIANS(a.Lat))))<3.0
  WHERE a.Demo=1;
 -- 2b. Hai dia diem that trung nhau: giu ma nho hon (nhap truoc, thuong da duoc chon loc), an ma lon hon
 DECLARE @hidePlace TABLE(Id bigint PRIMARY KEY,LyDo nvarchar(200));
 INSERT @hidePlace SELECT DISTINCT a.PlaceId,N'Trùng với địa điểm đã có' FROM @live a JOIN @live b ON b.PlaceId IS NOT NULL AND a.PlaceId>b.PlaceId
  AND a.Title COLLATE Vietnamese_CI_AI=b.Title COLLATE Vietnamese_CI_AI
  AND a.Lat IS NOT NULL AND b.Lat IS NOT NULL AND SQRT(SQUARE((a.Lat-b.Lat)*111.0)+SQUARE((a.Lng-b.Lng)*111.0*COS(RADIANS(a.Lat))))<3.0
  WHERE a.PlaceId IS NOT NULL;

 -- 3. Ten chung chung / khong phai diem tham quan
 INSERT @hidePlace SELECT p.MaDiaDiem,N'Tên chung chung hoặc không phải điểm tham quan' FROM dbo.DiaDiemDuLich p
  WHERE p.DangHienThi=1 AND NOT EXISTS (SELECT 1 FROM @hidePlace h WHERE h.Id=p.MaDiaDiem) AND (
   p.TenDiaDiem COLLATE Vietnamese_CI_AI IN (N'Đặc sản',N'Di tích lịch sử cấp tỉnh',N'Di tích lịch sử cấp quốc gia')
   OR p.TenDiaDiem IN (N'ĐẶC SẢN TRÂN CHÂU ( CÁT BÀ)',N'XE ĐƯA - ĐÓN SÂN BAY ĐÀ NẴNG - ĐIỂM THAM QUAN/ PICK UP SERVICE AT AIRPORT',N'TRUNG TÂM XÚC TIẾN THƯƠNG MẠI DU LỊCH',
    N'TOUR DU LỊCH QUẢNG BÌNH',N'TATILAND - ĐIỂM TƯ VẤN DỰ ÁN - NHÀ MẪU THAM QUAN TRÃI NGHIỆM.',N'DU LỊCH QUY NHƠN - CHỤP ẢNH TOUR'));
 -- Ban sao dinh vi sai cua noi da co (doi chieu tung truong hop): Bao tang Quang Trung o Tay Son (khong phai Quy Nhon),
 -- vinh Vinh Hy o Ninh Thuan (khong phai Cam Lam), Bao tang Hung Vuong da co ban o Viet Tri, "Cho Bao Tang" khong ten duong o Dien Bien.
 INSERT @hidePlace SELECT p.MaDiaDiem,N'Trùng với địa điểm đã có (vị trí sai)' FROM dbo.DiaDiemDuLich p
  WHERE p.DangHienThi=1 AND NOT EXISTS (SELECT 1 FROM @hidePlace h WHERE h.Id=p.MaDiaDiem) AND (
   (p.TenDiaDiem=N'BẢO TÀNG QUAN TRUNG' AND p.DiaChi LIKE N'%Quy Nhơn%')
   OR (p.TenDiaDiem=N'Khu du lịch Vĩnh Hy' AND p.DiaChi LIKE N'%Cam Lâm%')
   OR (p.TenDiaDiem=N'Bảo tàng Hùng Vương' AND p.DiaChi LIKE N'%Hy Cương%')
   OR (p.TenDiaDiem=N'CHỢ BẢO TÀNG' AND p.DiaChi LIKE N'%Địa điểm không tên%'));

 INSERT dbo.DonDuLieuSaoLuu(Bang,Ma,Cot,GiaTriCu,LyDo) SELECT N'DiaDiemDuLich',p.MaDiaDiem,N'DangHienThi',N'1',h.LyDo FROM dbo.DiaDiemDuLich p JOIN @hidePlace h ON h.Id=p.MaDiaDiem;
 UPDATE p SET DangHienThi=0 FROM dbo.DiaDiemDuLich p JOIN @hidePlace h ON h.Id=p.MaDiaDiem;
 INSERT @hideEntry SELECT e.Id,h.LyDo FROM dbo.DiscoveryEntry e JOIN @hidePlace h ON h.Id=e.PlaceId WHERE e.Status<>N'Hidden' AND NOT EXISTS (SELECT 1 FROM @hideEntry x WHERE x.Id=e.Id);
 INSERT dbo.DonDuLieuSaoLuu(Bang,Ma,Cot,GiaTriCu,LyDo) SELECT N'DiscoveryEntry',e.Id,N'Status',e.Status,h.LyDo FROM dbo.DiscoveryEntry e JOIN @hideEntry h ON h.Id=e.Id;
 UPDATE e SET Status=N'Hidden' FROM dbo.DiscoveryEntry e JOIN @hideEntry h ON h.Id=e.Id;

 -- 4. Ten viet HOA toan bo -> chu thuong co viet hoa dau tu (sua ca loi go ĐIỄM/VƯỚN)
 DECLARE @rename TABLE(Cu nvarchar(300) COLLATE DATABASE_DEFAULT,Moi nvarchar(300) COLLATE DATABASE_DEFAULT);
 INSERT @rename VALUES
  (N'THAM QUAN DU LỊCH SINH THÁI XỨ DỪA BẾN TRE',N'Du lịch sinh thái xứ dừa Bến Tre'),
  (N'ĐIỂM THAM QUAN - DU LỊCH SUỐI TIÊN',N'Điểm tham quan Suối Tiên'),
  (N'KHU DU LỊCH SUỐI KHOÁNG NÓNG HUA PE',N'Khu du lịch suối khoáng nóng Hua Pe'),
  (N'ĐIỂM THAM QUAN VƯỜN QUÝT HỒNG BA LIÊN',N'Vườn quýt hồng Ba Liên'),
  (N'KHU DU LỊCH SINH THÁI ĐỒNG QUAN',N'Khu du lịch sinh thái Đồng Quan'),
  (N'DI TÍCH LỊCH SỬ MIẾU BÀ TRÀ QUẬN CÔNG',N'Di tích Miếu Bà Trà Quận Công'),
  (N'BẢO TÀNG TRỊNH CÔNG SƠN',N'Bảo tàng Trịnh Công Sơn'),
  (N'THÁC MÂY',N'Thác Mây'),
  (N'THÁC BẠC BẠCH MÃ',N'Thác Bạc Bạch Mã'),
  (N'BẢO TÀNG QUẢNG TRỊ',N'Bảo tàng Quảng Trị'),
  (N'ĐIỂM THAM QUAN CON ĐƯỜNG TƠ LỤA',N'Con đường tơ lụa'),
  (N'DI TÍCH LỊCH SỬ CẤP TỈNH NHÀ THỜ HỌ HÀ',N'Nhà thờ họ Hà (di tích cấp tỉnh)'),
  (N'HỒNG THẮM QUÁN - FOOD & DRINK',N'Hồng Thắm quán'),
  (N'FLC GRAND HOTEL SAM SON',N'FLC Grand Hotel Sầm Sơn'),
  (N'ĐIỄM THAM QUAN VƯỚN TRÁI CÂY',N'Điểm tham quan vườn trái cây'),
  (N'ĐÌNH ĐỀN DI TÍCH LỊCH SỬ XÓM THƯỢNG',N'Đình đền Xóm Thượng');
 INSERT dbo.DonDuLieuSaoLuu(Bang,Ma,Cot,GiaTriCu,LyDo) SELECT N'DiaDiemDuLich',p.MaDiaDiem,N'TenDiaDiem',p.TenDiaDiem,N'Đổi tên viết hoa toàn bộ' FROM dbo.DiaDiemDuLich p JOIN @rename r ON r.Cu=p.TenDiaDiem COLLATE Vietnamese_CS_AS;
 UPDATE e SET Title=r.Moi FROM dbo.DiscoveryEntry e JOIN dbo.DiaDiemDuLich p ON p.MaDiaDiem=e.PlaceId JOIN @rename r ON r.Cu=p.TenDiaDiem COLLATE Vietnamese_CS_AS;
 UPDATE p SET TenDiaDiem=r.Moi FROM dbo.DiaDiemDuLich p JOIN @rename r ON r.Cu=p.TenDiaDiem COLLATE Vietnamese_CS_AS;

 EXEC sys.sp_addextendedproperty @name=N'HanhTrinh.DonDuLieu',@value=N'v1';
 COMMIT;
END
