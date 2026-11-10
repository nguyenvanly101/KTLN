function Resolve-TravelLocalDbConnection {
 [CmdletBinding()]
 param([Parameter(Mandatory)][string]$ConnectionString)
 if($ConnectionString -notmatch '(?i)(?:Server|Data Source)\s*=\s*\(localdb\)\\([^;]+)'){return [pscustomobject]@{ConnectionString=$ConnectionString;Server=$null;Fallback=$false}}
 $travelInstance=$Matches[1].Trim()
 if($travelInstance -notmatch '^[A-Za-z0-9_-]+$'){throw 'Tên instance LocalDB không hợp lệ.'}
 $travelLocalDb=Get-Command SqlLocalDB.exe -ErrorAction SilentlyContinue
 if(!$travelLocalDb){throw 'Không tìm thấy SqlLocalDB.exe. Cần cài SQL Server Express LocalDB để dùng cấu hình hiện tại.'}
 $travelInfo=& $travelLocalDb.Source info $travelInstance 2>&1 | Out-String
 if($travelInfo -notmatch '(?im)^Name:\s*'){throw "Không đọc được instance $travelInstance. Không tự tạo/xóa instance. Xem docs\TU_XU_LY_LOI_KHOI_DONG.md."}
 if($travelInfo -match '(?im)^State:\s*Running\s*$'){return [pscustomobject]@{ConnectionString=$ConnectionString;Server="(localdb)\$travelInstance";Fallback=$false}}
 Write-Host "Khởi động LocalDB $travelInstance…"
 $travelStart=& $travelLocalDb.Source start $travelInstance 2>&1 | Out-String
 $travelInfo=& $travelLocalDb.Source info $travelInstance 2>&1 | Out-String
 if($travelInfo -match '(?im)^State:\s*Running\s*$'){return [pscustomobject]@{ConnectionString=$ConnectionString;Server="(localdb)\$travelInstance";Fallback=$false}}
 # Some hosts cannot refresh LocalDB alias metadata even after SQL reports ready.
 # Accept only a LOCAL machine pipe from this user's instance log, and prove a
 # Windows-authenticated SQL connection. Never edit registry, recreate the instance,
 # or persist a pipe name that becomes stale after the server restarts.
 $travelSqlCmd=Get-Command sqlcmd.exe -ErrorAction SilentlyContinue
 if(!$travelSqlCmd){throw "LocalDB chưa xác nhận chạy. $travelStart`nXem log theo docs\TU_XU_LY_LOI_KHOI_DONG.md."}
 $travelFolders=@([Environment]::GetFolderPath('LocalApplicationData'),(Join-Path $env:USERPROFILE 'AppData\Local')) | Select-Object -Unique
 for($travelAttempt=0;$travelAttempt -lt 5;$travelAttempt++){
  foreach($travelFolder in $travelFolders){
   $travelLog=Join-Path $travelFolder "Microsoft\Microsoft SQL Server Local DB\Instances\$travelInstance\error.log"
   if(!(Test-Path -LiteralPath $travelLog)){continue}
   $travelText=Get-Content -LiteralPath $travelLog -Tail 180 -ErrorAction SilentlyContinue | Out-String
   $travelPipes=[regex]::Matches($travelText,'\\\\\.\\pipe\\LOCALDB#[0-9A-F]+\\tsql\\query',[Text.RegularExpressions.RegexOptions]::IgnoreCase)
   if($travelPipes.Count -eq 0){continue}
   $travelPipe='np:'+$travelPipes[$travelPipes.Count-1].Value
   $travelProbe=& $travelSqlCmd.Source -S $travelPipe -d master -E -C -I -b -l 3 -t 3 -Q "SET NOCOUNT ON; SELECT N'TRAVEL_LOCALDB_READY';" 2>&1 | Out-String
   if($LASTEXITCODE -ne 0 -or $travelProbe -notmatch 'TRAVEL_LOCALDB_READY'){continue}
   $travelBuilder=[System.Data.Common.DbConnectionStringBuilder]::new()
   # PowerShell's dictionary adapter can treat ConnectionString as a key.
   $travelBuilder.set_ConnectionString($ConnectionString)
   if($travelBuilder.ContainsKey('Data Source')){$travelBuilder['Data Source']=$travelPipe}else{$travelBuilder['Server']=$travelPipe}
   Write-Host 'SQL đã sẵn sàng; dùng pipe đã kiểm tra cho lần chạy này vì alias LocalDB chưa cập nhật.'
   return [pscustomobject]@{ConnectionString=$travelBuilder.get_ConnectionString();Server=$travelPipe;Fallback=$true}
  }
  Start-Sleep -Seconds 2
 }
 throw "LocalDB chưa kết nối được. Web chưa khởi chạy; giữ nguyên dữ liệu và xem docs\TU_XU_LY_LOI_KHOI_DONG.md.`n$travelStart"
}
