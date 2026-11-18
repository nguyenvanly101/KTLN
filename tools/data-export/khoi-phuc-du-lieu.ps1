# Khoi phuc du lieu mau (da lam sach) tu data/HoTroDuLich.zip vao LocalDB va chep anh vao App_Data/uploads.
# Dung: powershell -File tools/data-export/khoi-phuc-du-lieu.ps1 [-Ten HoTroDuLich] [-GhiDe]
# Tai khoan mau: khach@hanhtrinh.vn, nhacungcap@hanhtrinh.vn, admin@hanhtrinh.vn - mat khau chung HanhTrinh@2026.
param([string]$Ten = 'HoTroDuLich', [switch]$GhiDe)
$ErrorActionPreference = 'Stop'
$root = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$server = '(localdb)\MSSQLLocalDB'
$zip = Join-Path $root 'data\HoTroDuLich.zip'; if (-not (Test-Path $zip)) { throw "Khong thay $zip" }
$exists = & sqlcmd -I -S $server -C -h -1 -W -Q "SET NOCOUNT ON; SELECT CASE WHEN DB_ID('$Ten') IS NULL THEN 0 ELSE 1 END"
if ($exists.Trim() -eq '1' -and -not $GhiDe) { throw "CSDL $Ten da ton tai. Them -GhiDe neu muon thay bang du lieu mau (du lieu hien tai se mat)." }
$work = Join-Path $env:TEMP ('hanhtrinh-khoiphuc-' + [guid]::NewGuid().ToString('N')); Expand-Archive $zip $work
$bak = Join-Path $work 'HoTroDuLich.bak'
$files = & sqlcmd -I -S $server -C -h -1 -W -s '|' -Q "SET NOCOUNT ON; RESTORE FILELISTONLY FROM DISK=N'$bak'" | Where-Object { $_ -match '\|' }
$dataName = ($files[0] -split '\|')[0]; $logName = ($files[1] -split '\|')[0]
$dir = $env:USERPROFILE
& sqlcmd -I -S $server -C -b -Q "IF DB_ID('$Ten') IS NOT NULL ALTER DATABASE [$Ten] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; RESTORE DATABASE [$Ten] FROM DISK=N'$bak' WITH MOVE N'$dataName' TO N'$dir\$Ten.mdf', MOVE N'$logName' TO N'$dir\${Ten}_log.ldf', REPLACE; ALTER DATABASE [$Ten] SET MULTI_USER"
if ($LASTEXITCODE) { throw 'Khoi phuc that bai' }
$src = Join-Path $root 'data\uploads'
if (Test-Path $src) { $dst = Join-Path $root 'src\HoTroDuLich.Web\App_Data\uploads'; New-Item -ItemType Directory -Force $dst | Out-Null; Copy-Item -Recurse -Force (Join-Path $src '*') $dst }
Remove-Item -Recurse -Force $work
Write-Host "Da khoi phuc CSDL $Ten. Chay .\start.ps1 de mo website."
