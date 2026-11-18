-- Chep du lieu da lam sach tu HoTroDuLich_Sach sang HoTroDuLich_Moi (ban sao chi co cau truc, tao bang DBCC CLONEDATABASE).
-- Ly do: sao luu thang ban da lam sach van mang gia tri cu trong trang trong / chi muc toan van. CSDL moi chi chua gia tri hien tai.
-- Chay trong ngu canh HoTroDuLich_Moi.
SET NOCOUNT ON; SET XACT_ABORT ON;
IF DB_NAME()<>N'HoTroDuLich_Moi' BEGIN RAISERROR(N'Chay trong HoTroDuLich_Moi.',16,1); RETURN; END
EXEC sp_MSforeachtable N'ALTER TABLE ? NOCHECK CONSTRAINT ALL; ALTER TABLE ? DISABLE TRIGGER ALL;';
DECLARE @t sysname, @s sysname, @cols nvarchar(max), @ident bit, @q nvarchar(max);
DECLARE c CURSOR LOCAL FAST_FORWARD FOR
 SELECT s.name, t.name, OBJECTPROPERTY(t.object_id,'TableHasIdentity') FROM sys.tables t JOIN sys.schemas s ON s.schema_id=t.schema_id WHERE t.is_ms_shipped=0;
OPEN c; FETCH NEXT FROM c INTO @s,@t,@ident;
WHILE @@FETCH_STATUS=0
BEGIN
 SELECT @cols=STRING_AGG(CAST(QUOTENAME(c.name) AS nvarchar(max)),',') WITHIN GROUP (ORDER BY c.column_id)
  FROM sys.columns c JOIN sys.types ty ON ty.user_type_id=c.user_type_id
  WHERE c.object_id=OBJECT_ID(QUOTENAME(@s)+'.'+QUOTENAME(@t)) AND c.is_computed=0 AND ty.name NOT IN ('timestamp','rowversion') AND c.generated_always_type=0;
 SET @q=CASE WHEN @ident=1 THEN N'SET IDENTITY_INSERT '+QUOTENAME(@s)+'.'+QUOTENAME(@t)+N' ON;' ELSE N'' END
  +N'INSERT INTO '+QUOTENAME(@s)+'.'+QUOTENAME(@t)+N'('+@cols+N') SELECT '+@cols+N' FROM HoTroDuLich_Sach.'+QUOTENAME(@s)+'.'+QUOTENAME(@t)+N';'
  +CASE WHEN @ident=1 THEN N'SET IDENTITY_INSERT '+QUOTENAME(@s)+'.'+QUOTENAME(@t)+N' OFF;' ELSE N'' END;
 EXEC sp_executesql @q;
 FETCH NEXT FROM c INTO @s,@t,@ident;
END
CLOSE c; DEALLOCATE c;
EXEC sp_MSforeachtable N'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL; ALTER TABLE ? ENABLE TRIGGER ALL;';
-- Dong bo gia tri identity theo du lieu da chep
EXEC sp_MSforeachtable N'IF OBJECTPROPERTY(OBJECT_ID(''?''),''TableHasIdentity'')=1 DBCC CHECKIDENT(''?'', RESEED) WITH NO_INFOMSGS;';
EXEC sp_updatestats;
SELECT (SELECT SUM(p.rows) FROM sys.partitions p JOIN sys.tables t ON t.object_id=p.object_id WHERE p.index_id IN (0,1)) AS TongSoDong;
