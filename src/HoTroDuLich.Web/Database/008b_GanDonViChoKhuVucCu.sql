-- BAN NHAP - CHUA CHAY. Chay SAU 008 va SAU KHI da nap du lieu DonViHanhChinh + TenGoiKhacDonVi.
-- Tao thu tuc gan DonViId cho khu vuc cu dang chuoi. Chay: EXEC dbo.usp_GanDonViChoKhuVucCu @Regime='Cu';
-- @Regime = he dia gioi dung lam goc ('Cu' = 63 tinh/huyen/xa, 'Moi' = 34 tinh/xa).
-- Quy tac: gia tri khop dung MOT don vi (ten hoac ten goi khac, khong phan biet hoa/thuong/dau) -> tu gan.
--          ten trung o nhieu cap (vd Bac Giang vua la tinh vua la thanh pho): lay cap cao nhat neu cap do chi co 1 don vi.
--          khong khop -> hang cho (NoMatch). khop nhieu don vi -> hang cho (Ambiguous). Gia tri "Viet Nam" bi bo qua.
-- Co the chay lai: chi xu ly ban ghi con DonViId IS NULL.
-- Dung web truoc khi chay (cap nhat lam doi RowVersion cua dich vu/co so).
CREATE OR ALTER PROCEDURE dbo.usp_GanDonViChoKhuVucCu @Regime varchar(4)='Cu'
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 IF NOT EXISTS (SELECT 1 FROM dbo.DonViHanhChinh WHERE Status='Active' AND Regime=@Regime) THROW 51001,N'Chưa nạp dữ liệu địa giới.',1;
 BEGIN TRANSACTION;
 CREATE TABLE #Cand (Folded nvarchar(200) COLLATE Latin1_General_100_CI_AI NOT NULL, UnitId bigint NOT NULL, [Level] tinyint NOT NULL);
 INSERT #Cand SELECT NameFolded, Id, [Level] FROM dbo.DonViHanhChinh WHERE Status='Active' AND Regime=@Regime;
 INSERT #Cand SELECT REPLACE(REPLACE(FullName,N'đ',N'd'),N'Đ',N'D') COLLATE Latin1_General_100_CI_AI, Id, [Level] FROM dbo.DonViHanhChinh WHERE Status='Active' AND Regime=@Regime;
 INSERT #Cand SELECT a.NameFolded, a.UnitId, d.[Level] FROM dbo.TenGoiKhacDonVi a JOIN dbo.DonViHanhChinh d ON d.Id=a.UnitId WHERE d.Status='Active' AND d.Regime=@Regime;
 -- N = so don vi o cap cao nhat co ten do; chi tu gan khi N = 1
 CREATE TABLE #Count (Folded nvarchar(200) COLLATE Latin1_General_100_CI_AI NOT NULL PRIMARY KEY, N int NOT NULL, UnitId bigint NOT NULL);
 INSERT #Count
 SELECT x.Folded, COUNT(*), MIN(x.UnitId)
 FROM (SELECT DISTINCT c.Folded, c.UnitId, c.[Level] FROM #Cand c) x
 JOIN (SELECT Folded, MIN([Level]) AS MinLevel FROM #Cand GROUP BY Folded) m ON m.Folded=x.Folded AND m.MinLevel=x.[Level]
 GROUP BY x.Folded;
 DECLARE @map TABLE (T sysname NOT NULL, K sysname NOT NULL, C sysname NOT NULL);
 INSERT @map VALUES
  (N'DiaDiemDuLich',N'MaDiaDiem',N'KhuVuc'),(N'DichVuDuLich',N'MaDichVu',N'KhuVucPhucVu'),(N'BanCapNhatDichVu',N'MaBanCapNhat',N'KhuVucPhucVu'),
  (N'CoSoDichVu',N'Id',N'Region'),(N'BanCapNhatCoSo',N'Id',N'Region'),(N'MocDiaDiem',N'Id',N'Region'),(N'DiscoveryEntry',N'Id',N'Region');
 DECLARE @t sysname,@k sysname,@c sysname,@sql nvarchar(max);
 DECLARE m CURSOR LOCAL FAST_FORWARD FOR SELECT T,K,C FROM @map;
 OPEN m; FETCH NEXT FROM m INTO @t,@k,@c;
 WHILE @@FETCH_STATUS=0
 BEGIN
  IF OBJECT_ID(N'dbo.'+QUOTENAME(@t),N'U') IS NOT NULL AND COL_LENGTH(N'dbo.'+@t,N'DonViId') IS NOT NULL
  BEGIN
   -- gan tu dong khi khop dung mot don vi
   SET @sql=N'UPDATE r SET r.[DonViId]=c.UnitId FROM dbo.'+QUOTENAME(@t)+N' r JOIN #Count c ON c.N=1 AND c.Folded=REPLACE(REPLACE(r.'+QUOTENAME(@c)+N',N''đ'',N''d''),N''Đ'',N''D'') COLLATE Latin1_General_100_CI_AI WHERE r.[DonViId] IS NULL;';
   EXEC sys.sp_executesql @sql;
   -- khong khop hoac khop nhieu: dua vao hang cho (bo qua "Viet Nam")
   SET @sql=N'INSERT dbo.ChuyenDoiDiaGioiCho (TableName,RecordId,OldText,Reason)
    SELECT N'''+@t+N''', r.'+QUOTENAME(@k)+N', LEFT(r.'+QUOTENAME(@c)+N',200), CASE WHEN c.Folded IS NULL THEN ''NoMatch'' ELSE ''Ambiguous'' END
    FROM dbo.'+QUOTENAME(@t)+N' r
    LEFT JOIN #Count c ON c.Folded=REPLACE(REPLACE(r.'+QUOTENAME(@c)+N',N''đ'',N''d''),N''Đ'',N''D'') COLLATE Latin1_General_100_CI_AI
    WHERE r.[DonViId] IS NULL AND REPLACE(REPLACE(r.'+QUOTENAME(@c)+N',N''đ'',N''d''),N''Đ'',N''D'') COLLATE Latin1_General_100_CI_AI NOT IN (N''viet nam'')
      AND NOT EXISTS (SELECT 1 FROM dbo.ChuyenDoiDiaGioiCho q WHERE q.TableName=N'''+@t+N''' AND q.RecordId=r.'+QUOTENAME(@k)+N');';
   EXEC sys.sp_executesql @sql;
  END;
  FETCH NEXT FROM m INTO @t,@k,@c;
 END;
 CLOSE m; DEALLOCATE m;
 COMMIT TRANSACTION;
 SELECT TableName, Reason, COUNT(*) AS Total FROM dbo.ChuyenDoiDiaGioiCho WHERE Status='Pending' GROUP BY TableName, Reason ORDER BY TableName, Reason;
END;
