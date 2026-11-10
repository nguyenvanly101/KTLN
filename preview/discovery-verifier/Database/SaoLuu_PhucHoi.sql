-- SQLCMD mode. Supply DatabaseName, BackupDirectory and Khoi (A/B/C/D).
-- Schedule A daily at 01:00; B every 30 minutes; C Sunday 02:00. Keep 14 days.
-- D: restore to an isolated database and validate before directing live traffic to it.
DECLARE @db sysname=N'$(DatabaseName)', @folder nvarchar(4000)=N'$(BackupDirectory)', @block char(1)='$(Khoi)';
DECLARE @file nvarchar(4000), @sql nvarchar(max);
IF @block IN ('A','B')
BEGIN
 SET @file=@folder+N'\'+@db+N'_'+CONVERT(char(8),GETDATE(),112)+N'_'+REPLACE(CONVERT(char(8),GETDATE(),108),':','')+CASE @block WHEN 'A' THEN N'.bak' ELSE N'.trn' END;
 SET @sql=CASE @block WHEN 'A' THEN N'BACKUP DATABASE ' ELSE N'BACKUP LOG ' END+QUOTENAME(@db)+N' TO DISK=@path WITH CHECKSUM, INIT;';
 EXEC sys.sp_executesql @sql,N'@path nvarchar(4000)',@path=@file;
 RESTORE VERIFYONLY FROM DISK=@file WITH CHECKSUM;
END;
IF @block='C'
BEGIN
 SET @sql=N'DBCC CHECKDB ('+QUOTENAME(@db)+N') WITH NO_INFOMSGS;';
 EXEC sys.sp_executesql @sql;
END;
IF @block='D'
BEGIN
 -- Explicit inputs prevent restoring over the source database by accident.
 DECLARE @restoreDb sysname=N'$(RestoreDatabase)', @full nvarchar(4000)=N'$(FullBackup)', @log nvarchar(4000)=N'$(LogBackup)', @stop datetime2(0)='$(StopAt)';
 IF @restoreDb=@db THROW 50001, 'RestoreDatabase must be a separate verification database.',1;
 -- Review logical file names using RESTORE FILELISTONLY and supply distinct data/log files.
 SET @sql=N'RESTORE DATABASE '+QUOTENAME(@restoreDb)+N' FROM DISK=@full WITH NORECOVERY, MOVE N''$(LogicalData)'' TO N''$(RestoreDataFile)'', MOVE N''$(LogicalLog)'' TO N''$(RestoreLogFile)''; RESTORE LOG '+QUOTENAME(@restoreDb)+N' FROM DISK=@log WITH STOPAT=@stop, RECOVERY; DBCC CHECKDB ('+QUOTENAME(@restoreDb)+N') WITH NO_INFOMSGS;';
 EXEC sys.sp_executesql @sql,N'@full nvarchar(4000),@log nvarchar(4000),@stop datetime2(0)',@full,@log,@stop;
END;
