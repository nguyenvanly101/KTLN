param([Parameter(Mandatory)][string[]]$Batches)
$ErrorActionPreference='Stop'
$auditRoot=$PSScriptRoot
Set-Location -LiteralPath $auditRoot
$auditDb=[IO.File]::ReadAllText((Join-Path $auditRoot 'audit-db.txt')).Trim()
$auditServer=[IO.File]::ReadAllText((Join-Path $auditRoot 'sql-server.txt')).Trim()
if($auditDb -notmatch '^HoTroDuLich_Branches_Audit_[0-9]{14}$'){throw 'Only the dedicated audit database is allowed'}
$auditPort=5236
$auditBase='http://127.0.0.1:'+$auditPort
$env:ConnectionStrings__HoTroDuLich="Server=$auditServer;Database=$auditDb;Trusted_Connection=True;TrustServerCertificate=True"
$env:ASPNETCORE_ENVIRONMENT='Development'
$env:Demo__Enabled='true'
$env:DataProtection__ThuMucKhoa=Join-Path $auditRoot 'keys'
$env:UploadFiles__ThuMuc=Join-Path $auditRoot 'uploads'
$env:Logging__LogLevel__Default='Warning'
$env:AUDIT_SQL_SERVER=$auditServer
$env:BRANCH_TEST_DB=$auditDb
$env:BRANCH_SQL_SERVER=$auditServer
$env:DISCOVERY_SQL_SERVER=$auditServer
$env:FULLTEXT_TEST_BASE=$auditBase
$env:DISCOVERY_TEST_BASE=$auditBase
$env:TRAVEL_WORKFLOW_DB=$auditDb
$env:TRAVEL_COMPANION_EXTENSION='1'
$logs=Join-Path $auditRoot 'logs'
New-Item -ItemType Directory -Path $logs -Force | Out-Null
$round=[DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss-fff')
$index=0
$hadFailure=$false
foreach($batch in $Batches){
 $index++
 if(netstat -ano -p TCP | Select-String (':'+$auditPort+'\s+.*LISTENING')){throw 'Audit port already in use'}
 $tag=$round+'-'+$index
 $hostOut=Join-Path $logs ($tag+'-server.log')
 $hostErr=Join-Path $logs ($tag+'-server-error.log')
 $dll=Join-Path $auditRoot 'build\HoTroDuLich.Web.dll'
 $app=Start-Process -FilePath (Get-Command dotnet).Source -ArgumentList @($dll,'--contentRoot','D:\KLTN\src\HoTroDuLich.Web','--urls',$auditBase) -WorkingDirectory $auditRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput $hostOut -RedirectStandardError $hostErr
 try {
  $ready=$false
  for($attempt=0;$attempt -lt 30;$attempt++){
   if($app.HasExited){throw ('Audit server failed: '+((Get-Content -LiteralPath $hostErr -Tail 7) -join ' '))}
   try{if((Invoke-RestMethod ($auditBase+'/api/maps/config') -TimeoutSec 2).mapProvider -eq 'OpenStreetMap'){$ready=$true;break}}catch{}
   Start-Sleep -Milliseconds 300
  }
  if(!$ready){throw 'Audit server did not become ready'}
  Write-Host "Testing $batch"
  foreach($step in $batch.Split(',')){
   if($step -notmatch '^[a-z][a-z0-9-]+(?::[a-z-]+)?$'){throw 'Invalid test name'}
   $test,$argument=$step.Split(':',2)
   $testFile=Join-Path $auditRoot ('tests\'+$test+'.cjs')
   if(!(Test-Path -LiteralPath $testFile -PathType Leaf)){throw "Missing test $test"}
   $output=@(& node $testFile @($argument | Where-Object {$_}) 2>&1)
   $code=$LASTEXITCODE
   $log=Join-Path $logs ($tag+'-'+$step.Replace(':','-')+'.log')
   [IO.File]::WriteAllLines($log,[string[]]($output | ForEach-Object {$_ -as [string]}))
   $status=if($code -eq 0){'PASS'}else{'FAIL'}
   Write-Host "$status $step"
   if($code -eq 0){$output | Select-Object -Last 4 | ForEach-Object{Write-Host $_}}
   else{$output | Select-Object -Last 13 | ForEach-Object{Write-Host $_};$hadFailure=$true;break}
  }
 } finally {
  if(!$app.HasExited){Stop-Process -InputObject $app}
  $app.WaitForExit(5000) | Out-Null
 }
}
if($hadFailure){exit 1}
