-- Sua hau qua cua 037: tai khoan mau May Travel chi co 6 co so "(mau)" va ca 7 dich vu deu gan voi co so "Quan mi Quang (mau)".
-- An co so do lam toan bo dich vu cua nha cung cap mau bien mat khoi trang khach.
-- Khoi phuc co so nay thanh van phong that cua May Travel (bo chu "(mau)" va dong "Khong phai don vi that"); 5 co so mau con lai van an.
-- Chay mot lan (extended property HanhTrinh.KhoiPhucMayTravel = v1). Gia tri cu ghi vao DonDuLieuSaoLuu.
SET ANSI_NULLS ON; SET QUOTED_IDENTIFIER ON; SET XACT_ABORT ON; SET NOCOUNT ON;
IF OBJECT_ID(N'dbo.CoSoDichVu',N'U') IS NOT NULL AND OBJECT_ID(N'dbo.DonDuLieuSaoLuu',N'U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM sys.extended_properties WHERE class=0 AND name=N'HanhTrinh.KhoiPhucMayTravel' AND CAST(value AS nvarchar(20))=N'v1')
BEGIN
 BEGIN TRANSACTION;
 -- Co so mau dang chua dich vu (an boi 037) -> khoi phuc
 DECLARE @b TABLE(Id bigint PRIMARY KEY,ProviderId bigint);
 INSERT @b SELECT b.Id,b.ProviderId FROM dbo.CoSoDichVu b
  WHERE b.Status=N'Hidden' AND b.Name LIKE N'%(mẫu)' AND EXISTS (SELECT 1 FROM dbo.DichVuCoSo l WHERE l.BranchId=b.Id)
   AND EXISTS (SELECT 1 FROM dbo.DonDuLieuSaoLuu s WHERE s.Bang=N'CoSoDichVu' AND s.Ma=b.Id AND s.Cot=N'Status');
 INSERT dbo.DonDuLieuSaoLuu(Bang,Ma,Cot,GiaTriCu,LyDo) SELECT N'CoSoDichVu',b.Id,N'Name',b.Name,N'Khôi phục cơ sở chứa dịch vụ' FROM dbo.CoSoDichVu b JOIN @b x ON x.Id=b.Id;
 UPDATE b SET Status=N'Approved',
   Name=LEFT(p.TenDonVi+N' – văn phòng '+CASE WHEN b.Region LIKE N'%Đà Nẵng%' THEN N'Đà Nẵng' ELSE N'chính' END,200),
   Type=N'HoatDong',
   Address=REPLACE(b.Address,N'Địa chỉ mẫu, ',N''),
   Summary=N'Văn phòng của '+p.TenDonVi+N': nhận đặt phòng, tour, thuê xe và ăn uống do '+p.TenDonVi+N' cung cấp. Nhắn tin để hỏi giá và lịch trống.',
   Phone=COALESCE(b.Phone,p.SoDienThoai),UpdatedAt=SYSUTCDATETIME()
  FROM dbo.CoSoDichVu b JOIN @b x ON x.Id=b.Id JOIN dbo.NhaCungCapDichVu p ON p.MaNhaCungCap=b.ProviderId;
 -- Ban duyet dang cong bo mang cung noi dung moi
 UPDATE r SET Name=b.Name,Type=b.Type,Address=b.Address,Summary=b.Summary,Phone=b.Phone
  FROM dbo.BanCapNhatCoSo r JOIN dbo.CoSoDichVu b ON b.Id=r.BranchId JOIN @b x ON x.Id=b.Id WHERE r.Status=N'Approved' AND r.Number=b.PublishedVersion;
 -- Muc tra cuu cua co so: hien lai (bo chay nen cap nhat ten, tom tat trong vong mot phut)
 UPDATE e SET Status=N'Published',SourceStamp=NULL FROM dbo.DiscoveryEntry e JOIN @b x ON x.Id=e.BranchId;
 EXEC sys.sp_addextendedproperty @name=N'HanhTrinh.KhoiPhucMayTravel',@value=N'v1';
 COMMIT;
END
