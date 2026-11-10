[CmdletBinding()]
param([string]$Server='(localdb)\MSSQLLocalDB')
$ErrorActionPreference='Stop'
$travelRoot=[IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
$travelStamp=Get-Date -Format 'yyyyMMdd_HHmmss'
$travelBackupRoot=Join-Path $travelRoot 'preview\backups'
New-Item -ItemType Directory -Path $travelBackupRoot -Force | Out-Null
Add-Type -AssemblyName System.IO.Compression.FileSystem
function New-VerifiedArchive([string]$Source,[string]$Destination,[bool]$FilterBuild){
 if(Test-Path -LiteralPath $Destination){throw 'Bản sao lưu đã tồn tại.'}
 $travelFiles=@(Get-ChildItem -LiteralPath $Source -File -Recurse | Where-Object { !$FilterBuild -or $_.FullName -notmatch '[\\/](bin|obj)[\\/]' })
 $travelEntries=@()
 $travelZip=[IO.Compression.ZipFile]::Open($Destination,[IO.Compression.ZipArchiveMode]::Create)
 try{foreach($travelFile in $travelFiles){
  $travelRelative=[IO.Path]::GetRelativePath($Source,$travelFile.FullName).Replace('\','/')
  [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($travelZip,$travelFile.FullName,$travelRelative,[IO.Compression.CompressionLevel]::Optimal)|Out-Null
  $travelEntries+=@{Path=$travelRelative;Sha256=(Get-FileHash -LiteralPath $travelFile.FullName -Algorithm SHA256).Hash}
 }}finally{$travelZip.Dispose()}
 $travelZip=[IO.Compression.ZipFile]::OpenRead($Destination)
 try{foreach($travelEntry in $travelEntries){
  $travelStream=$travelZip.GetEntry($travelEntry.Path).Open()
  try{$travelHash=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($travelStream))}finally{$travelStream.Dispose()}
  if($travelHash -ne $travelEntry.Sha256){throw 'Kiểm tra hash bản sao lưu không khớp.'}
 }}finally{$travelZip.Dispose()}
 return @{Path=$Destination;Files=$travelEntries;Verified=$true}
}
$travelSource=New-VerifiedArchive (Join-Path $travelRoot 'src') (Join-Path $travelBackupRoot ('source_before_workflow_'+$travelStamp+'.zip')) $true
$travelPrivate=New-VerifiedArchive (Join-Path $travelRoot 'src\HoTroDuLich.Web\App_Data') (Join-Path $travelBackupRoot ('App_Data_before_workflow_'+$travelStamp+'.zip')) $false
$travelSqlFile=Join-Path $travelBackupRoot ('HoTroDuLich_before_workflow_'+$travelStamp+'.bak')
if(Test-Path -LiteralPath $travelSqlFile){throw 'Bản sao lưu SQL đã tồn tại.'}
$travelEscaped=$travelSqlFile.Replace("'","''")
$travelOutput=@(sqlcmd -S $Server -d master -E -C -I -b -l 15 -t 180 -Q "BACKUP DATABASE [HoTroDuLich] TO DISK=N'$travelEscaped' WITH COPY_ONLY,CHECKSUM; RESTORE VERIFYONLY FROM DISK=N'$travelEscaped' WITH CHECKSUM; PRINT 'WORKFLOW_BACKUP_OK';")
if($LASTEXITCODE -ne 0 -or !($travelOutput -match '^WORKFLOW_BACKUP_OK$')){throw ($travelOutput -join "`n")}
@{Date=[DateTime]::UtcNow.ToString('o');Source=$travelSource;PrivateFiles=$travelPrivate;Sql=@{Path=$travelSqlFile;Verified=$true}}|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $travelRoot 'preview\workflow-backup.json') -Encoding utf8
Write-Output "Đã xác minh ZIP source, App_Data và SQL COPY_ONLY: $travelSqlFile"
