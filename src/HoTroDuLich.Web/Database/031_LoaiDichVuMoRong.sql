-- Mo rong danh muc loai dich vu: them nhom Y te & an toan, Tien ich & mua sam va cac loai chi tiet cho di chuyen, an uong, luu tru. Chay lap an toan.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
DECLARE @lock int;
EXEC @lock=sys.sp_getapplock @Resource=N'HoTroDuLich.LoaiDichVuMoRong.v1',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=30000;
IF @lock<0 THROW 51000,N'Không lấy được khóa nâng cấp loại dịch vụ.',1;

IF OBJECT_ID(N'dbo.LoaiDichVu',N'U') IS NOT NULL
BEGIN
 IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name=N'CK_LoaiDV_Group' AND definition NOT LIKE N'%Health%')
 BEGIN
  ALTER TABLE dbo.LoaiDichVu DROP CONSTRAINT CK_LoaiDV_Group;
  ALTER TABLE dbo.LoaiDichVu ADD CONSTRAINT CK_LoaiDV_Group CHECK (GroupCode IN ('Place','Eat','Stay','Move','Experience','Info','Local','Health','Utility','Other'));
 END;
 DECLARE @seed TABLE(Code varchar(20),Label nvarchar(60),G varchar(12),S int,Bk bit,Po bit,Br bit,Pr bit,Hr bit,Lo bit);
 INSERT @seed VALUES
  ('CaPhe',N'Cà phê & đồ uống','Eat',21,1,1,1,1,1,1),
  ('QuanChay',N'Quán chay','Eat',22,1,1,1,1,1,1),
  ('ChoAmThuc',N'Chợ & ẩm thực đường phố','Eat',23,0,0,0,0,1,1),
  ('NhaNghi',N'Nhà nghỉ','Stay',31,1,1,1,1,1,1),
  ('CamTrai',N'Cắm trại','Stay',32,1,1,1,1,1,1),
  ('BenXe',N'Bến xe & nhà xe','Move',41,1,1,1,1,1,1),
  ('ThueXe',N'Thuê xe','Move',42,1,1,1,1,1,1),
  ('SanBay',N'Sân bay','Move',43,0,0,0,0,1,1),
  ('GaTau',N'Ga tàu hỏa','Move',44,0,0,0,0,1,1),
  ('BenTau',N'Bến tàu & phà','Move',45,0,0,0,0,1,1),
  ('TramXang',N'Trạm xăng','Move',46,0,0,0,1,1,1),
  ('SuaXe',N'Sửa xe','Move',47,0,1,1,1,1,1),
  ('BaiDoXe',N'Bãi đỗ xe','Move',48,0,1,1,1,1,1),
  ('BenhVien',N'Bệnh viện','Health',130,0,0,0,0,1,1),
  ('PhongKham',N'Phòng khám','Health',131,0,1,1,1,1,1),
  ('NhaThuoc',N'Nhà thuốc','Health',132,0,1,1,1,1,1),
  ('CongAn',N'Công an','Health',133,0,0,0,0,1,1),
  ('CuuHo',N'Cứu hộ & cứu nạn','Health',134,0,0,0,0,1,1),
  ('Atm',N'ATM & ngân hàng','Utility',140,0,0,0,0,1,1),
  ('DoiTien',N'Đổi tiền','Utility',141,0,1,1,1,1,1),
  ('BuuDien',N'Bưu điện','Utility',142,0,0,0,0,1,1),
  ('SieuThi',N'Siêu thị & cửa hàng tiện lợi','Utility',143,0,1,1,1,1,1),
  ('TrungTamTM',N'Trung tâm thương mại','Utility',144,0,0,0,0,1,1),
  ('GiatUi',N'Giặt ủi','Utility',145,1,1,1,1,1,1),
  ('NhaVeSinh',N'Nhà vệ sinh công cộng','Utility',146,0,0,0,0,1,1),
  ('SimWifi',N'SIM & Wi-Fi','Utility',147,0,1,1,1,1,1),
  ('GuiHanhLy',N'Gửi hành lý','Utility',148,1,1,1,1,1,1),
  ('QuaLuuNiem',N'Quà lưu niệm','Utility',149,0,1,1,1,1,1);
 INSERT dbo.LoaiDichVu(Code,Label,GroupCode,SortOrder,Bookable,ProviderPostable,ForBranch,HasPrice,HasHours,HasLocation,TimeBound,Builtin)
  SELECT s.Code,s.Label,s.G,s.S,s.Bk,s.Po,s.Br,s.Pr,s.Hr,s.Lo,0,1 FROM @seed s WHERE NOT EXISTS (SELECT 1 FROM dbo.LoaiDichVu x WHERE x.Code=s.Code);
END;
COMMIT TRANSACTION;
