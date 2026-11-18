-- Lam sach ban sao CSDL truoc khi dua len kho cong khai. CHI chay tren ban sao (vd HoTroDuLich_Sach), khong chay tren CSDL that.
-- Giu: dia diem, diem den, dich vu, co so, bai viet, danh gia, chuyen di, tai khoan mau (@hanhtrinh.vn).
-- Thay / bo: email va so dien thoai that, ten that, mat khau that (doi thanh mat khau mau), dia chi IP trong nhat ky,
-- giay to xac minh / giai trinh, tep gui trong chat, anh dai dien / anh bia nguoi dung tai len.
SET NOCOUNT ON; SET XACT_ABORT ON;
IF DB_NAME() IN (N'HoTroDuLich') BEGIN RAISERROR(N'Khong chay tren CSDL that.',16,1); RETURN; END
BEGIN TRANSACTION;

DECLARE @demoHash nvarchar(max)=(SELECT MatKhauBam FROM dbo.TaiKhoan WHERE Email=N'khach@hanhtrinh.vn');
DECLARE @real TABLE(Id bigint PRIMARY KEY);
INSERT @real SELECT MaTaiKhoan FROM dbo.TaiKhoan WHERE Email NOT LIKE N'%@hanhtrinh.vn' AND Email NOT LIKE N'%@example.com';

-- Tai khoan that -> tai khoan mau an danh, dang nhap bang mat khau mau
UPDATE a SET Email=N'nguoidung'+CAST(a.MaTaiKhoan AS nvarchar(20))+N'@example.com', SoDienThoai=NULL, MatKhauBam=@demoHash, PhaiDoiMatKhau=0
 FROM dbo.TaiKhoan a JOIN @real r ON r.Id=a.MaTaiKhoan;
UPDATE h SET TenHienThi=N'Người dùng '+CAST(h.MaTaiKhoan AS nvarchar(20)), GioiThieu=NULL, NoiSinhSong=NULL, SoThich=NULL
 FROM dbo.HoSoNguoiDung h JOIN @real r ON r.Id=h.MaTaiKhoan;
UPDATE p SET TenDonVi=N'Nhà cung cấp '+CAST(p.MaNhaCungCap AS nvarchar(20)), NguoiLienHe=N'Người liên hệ', SoDienThoai=N'0900000000',
  EmailLienHe=N'nhacungcap'+CAST(p.MaNhaCungCap AS nvarchar(20))+N'@example.com', DiaChi=N'Địa chỉ đã ẩn'
 FROM dbo.NhaCungCapDichVu p JOIN @real r ON r.Id=p.MaTaiKhoan;

-- Ho so xac minh cua nha cung cap that: nguoi dai dien va noi dung dang ky (tung co nguoi go nham mat khau vao day)
UPDATE h SET NguoiDaiDien=N'Người đại diện', NoiDungDangKy=N'Hồ sơ đăng ký (đã ẩn khi xuất dữ liệu)'
 FROM dbo.HoSoXacMinhNhaCungCap h JOIN dbo.NhaCungCapDichVu p ON p.MaNhaCungCap=h.MaNhaCungCap JOIN @real r ON r.Id=p.MaTaiKhoan;

-- Anh do nguoi dung tai len cho dich vu / co so / bai viet: khong dua len kho cong khai (co the la anh co ban quyen, anh ca nhan).
-- Dich vu, co so se hien anh mac dinh theo loai.
UPDATE dbo.DichVuDuLich SET AnhJson=NULL WHERE AnhJson IS NOT NULL;
UPDATE dbo.BanCapNhatDichVu SET AnhJson=NULL WHERE AnhJson IS NOT NULL;
UPDATE dbo.CoSoDichVu SET PhotosJson=NULL WHERE PhotosJson IS NOT NULL;
UPDATE dbo.BanCapNhatCoSo SET PhotosJson=NULL WHERE PhotosJson IS NOT NULL;
UPDATE dbo.BaiViet SET PhotosJson=NULL WHERE PhotosJson IS NOT NULL;
UPDATE dbo.DiscoveryEntry SET ImagePath=NULL WHERE ImagePath LIKE N'/service-photos/%' OR ImagePath LIKE N'/co-so/%';

-- Anh dai dien / anh bia do nguoi dung tai len
UPDATE dbo.HoSoNguoiDung SET AnhDaiDien=NULL, AnhBia=NULL WHERE AnhDaiDien IS NOT NULL OR AnhBia IS NOT NULL;

-- Nhat ky: bo dia chi IP
UPDATE dbo.NhatKyThaoTac SET DiaChiIP=NULL WHERE DiaChiIP IS NOT NULL;

-- Giay to xac minh / giai trinh (tep rieng tu)
DELETE FROM dbo.TaiLieuXacMinh;
IF OBJECT_ID(N'dbo.TaiLieuGiaiTrinh',N'U') IS NOT NULL DELETE FROM dbo.TaiLieuGiaiTrinh;

-- Tep / anh gui trong chat
-- Danh dau nhu tin da thu hoi (giu dong de khong vo lien ket tra loi).
UPDATE dbo.TinNhanChat SET ImagePath=NULL, FilePath=NULL, FileName=NULL, DeletedAt=ISNULL(DeletedAt,SYSUTCDATETIME()) WHERE ImagePath IS NOT NULL OR FilePath IS NOT NULL;

COMMIT;

-- Ghi lai moi trang du lieu: gia tri cu bi ghi de / dong da xoa con nam trong vung trong cua trang va se lot vao ban sao luu.
DECLARE @t sysname, @q nvarchar(max);
DECLARE c CURSOR LOCAL FAST_FORWARD FOR SELECT QUOTENAME(s.name)+'.'+QUOTENAME(t.name) FROM sys.tables t JOIN sys.schemas s ON s.schema_id=t.schema_id;
OPEN c; FETCH NEXT FROM c INTO @t;
WHILE @@FETCH_STATUS=0 BEGIN SET @q=N'ALTER TABLE '+@t+N' REBUILD; ALTER INDEX ALL ON '+@t+N' REBUILD;'; EXEC sp_executesql @q; FETCH NEXT FROM c INTO @t; END
CLOSE c; DEALLOCATE c;
CHECKPOINT;
SELECT (SELECT COUNT(*) FROM dbo.TaiKhoan WHERE Email NOT LIKE N'%@hanhtrinh.vn' AND Email NOT LIKE N'%@example.com') AS EmailThatConLai,
       (SELECT COUNT(*) FROM dbo.NhatKyThaoTac WHERE DiaChiIP IS NOT NULL) AS IpConLai,
       (SELECT COUNT(*) FROM dbo.TaiLieuXacMinh) AS GiayToConLai;
