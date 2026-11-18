# Xuat du lieu that da lam sach vao thu muc data/ de dua len kho.
#  1. Sao luu CSDL that (chi doc) -> phuc hoi thanh ban sao HoTroDuLich_Sach
#  2. Chay lam-sach.sql tren ban sao (email/sdt/ten that, IP, giay to, tep chat, anh dai dien)
#  3. Sao luu ban sach -> data/HoTroDuLich.zip ; chep cac anh dich vu / co so / bai viet dang dung -> data/uploads
#  4. Xoa ban sao
# Dung: powershell -File tools/data-export/xuat-du-lieu.ps1
$ErrorActionPreference = 'Stop'
$root = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$server = '(localdb)\MSSQLLocalDB'
$work = Join-Path $env:TEMP ('hanhtrinh-xuat-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force $work | Out-Null
$data = Join-Path $root 'data'; New-Item -ItemType Directory -Force $data | Out-Null
function Sql($db, $q) { & sqlcmd -I -S $server -d $db -C -b -Q $q; if ($LASTEXITCODE) { throw "sqlcmd loi: $q" } }

$raw = Join-Path $work 'that.bak'
Sql 'master' "BACKUP DATABASE [HoTroDuLich] TO DISK=N'$raw' WITH INIT, COPY_ONLY"
$files = & sqlcmd -I -S $server -C -h -1 -W -s '|' -Q "SET NOCOUNT ON; RESTORE FILELISTONLY FROM DISK=N'$raw'" | Where-Object { $_ -match '\|' }
$dataName = ($files[0] -split '\|')[0]; $logName = ($files[1] -split '\|')[0]
Sql 'master' "RESTORE DATABASE [HoTroDuLich_Sach] FROM DISK=N'$raw' WITH MOVE N'$dataName' TO N'$work\sach.mdf', MOVE N'$logName' TO N'$work\sach_log.ldf', REPLACE"
try {
  & sqlcmd -I -S $server -d HoTroDuLich_Sach -C -b -f 65001 -i (Join-Path $PSScriptRoot 'lam-sach.sql'); if ($LASTEXITCODE) { throw 'lam-sach.sql loi' }
  # Khong chep anh do nguoi dung tai len (lam-sach.sql da go tham chieu): kho cong khai chi co anh Wikimedia (anh-tra-cuu).
  $up = Join-Path $data 'uploads'; if (Test-Path $up) { Remove-Item -Recurse -Force $up }
  Sql 'master' "IF DB_ID('HoTroDuLich_Moi') IS NOT NULL BEGIN ALTER DATABASE [HoTroDuLich_Moi] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [HoTroDuLich_Moi]; END; DBCC CLONEDATABASE ([HoTroDuLich_Sach],[HoTroDuLich_Moi]) WITH NO_STATISTICS, NO_QUERYSTORE, VERIFY_CLONEDB; ALTER DATABASE [HoTroDuLich_Moi] SET READ_WRITE; ALTER DATABASE [HoTroDuLich_Moi] SET RECOVERY SIMPLE"
  & sqlcmd -I -S $server -d HoTroDuLich_Moi -C -b -f 65001 -i (Join-Path $PSScriptRoot 'chep-sang-csdl-moi.sql'); if ($LASTEXITCODE) { throw 'chep-sang-csdl-moi.sql loi' }
  Sql 'HoTroDuLich_Moi' "CHECKPOINT; DBCC SHRINKFILE (2, 1)"
  $clean = Join-Path $work 'HoTroDuLich.bak'
  Sql 'master' "BACKUP DATABASE [HoTroDuLich_Moi] TO DISK=N'$clean' WITH INIT, COPY_ONLY"
  $zip = Join-Path $data 'HoTroDuLich.zip'; if (Test-Path $zip) { Remove-Item $zip }
  Compress-Archive -Path $clean -DestinationPath $zip -CompressionLevel Optimal
  Write-Host ("Da xuat: " + $zip + " (" + [math]::Round((Get-Item $zip).Length / 1MB, 1) + " MB), anh: " + (Get-ChildItem -Recurse -File $up -ErrorAction SilentlyContinue | Measure-Object).Count)
}
finally {
  & sqlcmd -I -S $server -C -Q "IF DB_ID('HoTroDuLich_Sach') IS NOT NULL BEGIN ALTER DATABASE [HoTroDuLich_Sach] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [HoTroDuLich_Sach]; END" | Out-Null
  & sqlcmd -I -S $server -C -Q "IF DB_ID('HoTroDuLich_Moi') IS NOT NULL BEGIN ALTER DATABASE [HoTroDuLich_Moi] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [HoTroDuLich_Moi]; END" | Out-Null
  Remove-Item -Recurse -Force $work -ErrorAction SilentlyContinue
}
