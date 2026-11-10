[CmdletBinding(SupportsShouldProcess)]
param(
 [ValidateSet('A','B','C')][string]$Khoi='A',
 [ValidatePattern('^[A-Za-z][A-Za-z0-9_]{0,100}$')][string]$DatabaseName='HoTroDuLich',
 [string]$Server='(localdb)\MSSQLLocalDB',
 [string]$BackupDirectory='',
 [switch]$DonDep
)
$ErrorActionPreference='Stop'
$travelRoot=Split-Path -Parent $PSScriptRoot
if(!$BackupDirectory){$BackupDirectory=Join-Path $travelRoot 'preview\backups'}
$travelBackupRoot=[IO.Path]::GetFullPath($BackupDirectory)
New-Item -ItemType Directory -Path $travelBackupRoot -Force | Out-Null
$travelStamp=Get-Date -Format 'yyyyMMdd_HHmmss'
$travelDb='['+$DatabaseName+']'
if($Khoi -eq 'C'){
 $travelSqlOutput=@(sqlcmd -S $Server -d master -E -C -b -l 15 -t 600 -Q "DBCC CHECKDB ($travelDb) WITH NO_INFOMSGS; PRINT 'FULLTEXT_CHECKDB_OK';")
 $travelSqlOutput | Write-Output
 if($LASTEXITCODE -ne 0 -or !($travelSqlOutput -match '^FULLTEXT_CHECKDB_OK$')){throw 'CHECKDB chưa xác nhận hoàn thành.'}
 Write-Output 'CHECKDB thành công.'
 return
}
$travelType=if($Khoi -eq 'A'){'full'}else{'log'}
$travelExtension=if($Khoi -eq 'A'){'bak'}else{'trn'}
$travelFile=Join-Path $travelBackupRoot ($DatabaseName+'_'+$travelStamp+'_'+$travelType+'.'+$travelExtension)
if(Test-Path -LiteralPath $travelFile){throw 'Tên bản sao lưu đã tồn tại.'}
$travelSqlPath=$travelFile.Replace("'","''")
$travelCommand=if($Khoi -eq 'A'){'BACKUP DATABASE'}else{'BACKUP LOG'}
$travelSqlOutput=@(sqlcmd -S $Server -d master -E -C -b -l 15 -t 600 -Q "$travelCommand $travelDb TO DISK=N'$travelSqlPath' WITH CHECKSUM; RESTORE VERIFYONLY FROM DISK=N'$travelSqlPath' WITH CHECKSUM; PRINT 'FULLTEXT_BACKUP_OK';")
$travelSqlOutput | Write-Output
if($LASTEXITCODE -ne 0 -or !($travelSqlOutput -match '^FULLTEXT_BACKUP_OK$')){throw 'Sao lưu SQL/VERIFYONLY chưa xác nhận hoàn thành.'}
$travelPrivateArchive=$null
if($Khoi -eq 'A'){
 $travelPrivateFolder=Join-Path $travelRoot 'src\HoTroDuLich.Web\App_Data'
 if(Test-Path -LiteralPath $travelPrivateFolder){
  $travelPrivateArchive=Join-Path $travelBackupRoot ($DatabaseName+'_'+$travelStamp+'_App_Data.zip')
  Compress-Archive -LiteralPath $travelPrivateFolder -DestinationPath $travelPrivateArchive -CompressionLevel Optimal
 }
}
# Retain a full backup at/before the 14-day boundary and every later log. Cleanup
# is opt-in, never recursive, and applies only to filenames generated here.
if($DonDep){
 $travelCutoff=[DateTime]::UtcNow.AddDays(-14)
 $travelFulls=@(Get-ChildItem -LiteralPath $travelBackupRoot -File -Filter ($DatabaseName+'_*_full.bak') | Sort-Object LastWriteTimeUtc)
 $travelBaseline=$travelFulls | Where-Object LastWriteTimeUtc -le $travelCutoff | Select-Object -Last 1
 if(!$travelBaseline){$travelBaseline=$travelFulls | Select-Object -First 1}
 if($travelBaseline){
  $travelPattern='^'+[regex]::Escape($DatabaseName)+'_\d{8}_\d{6}_(full\.bak|log\.trn|App_Data\.zip)$'
  foreach($travelOld in (Get-ChildItem -LiteralPath $travelBackupRoot -File | Where-Object {$_.Name -match $travelPattern -and $_.LastWriteTimeUtc -lt $travelBaseline.LastWriteTimeUtc})){
   $travelResolved=[IO.Path]::GetFullPath($travelOld.FullName)
   if(!$travelResolved.StartsWith($travelBackupRoot+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Đường dẫn dọn dẹp nằm ngoài thư mục sao lưu.'}
   if($PSCmdlet.ShouldProcess($travelResolved,'Xóa bản sao lưu cũ hơn chuỗi giữ lại')){Remove-Item -LiteralPath $travelResolved}
  }
 }
}
[pscustomobject]@{Database=$DatabaseName;Block=$Khoi;Backup=$travelFile;PrivateFiles=$travelPrivateArchive;Verified=$true} | ConvertTo-Json
