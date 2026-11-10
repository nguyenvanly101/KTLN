[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$travelRoot = Split-Path -Parent $PSScriptRoot
$travelSource = Join-Path $travelRoot 'src\HoTroDuLich.Web\App_Data'
$travelDestination = Join-Path $travelRoot ('preview\backups\App_Data_before_companion_' + (Get-Date -Format 'yyyyMMdd_HHmmss') + '.zip')
if (Test-Path -LiteralPath $travelDestination) { throw 'Bản sao lưu đã tồn tại.' }
Add-Type -AssemblyName System.IO.Compression.FileSystem
$travelFiles = @(Get-ChildItem -LiteralPath $travelSource -File -Recurse | ForEach-Object {
    [pscustomobject]@{ Path = [IO.Path]::GetRelativePath($travelSource, $_.FullName).Replace('\', '/'); Sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash }
})
[IO.Compression.ZipFile]::CreateFromDirectory($travelSource, $travelDestination, [IO.Compression.CompressionLevel]::Optimal, $false)
$travelZip = [IO.Compression.ZipFile]::OpenRead($travelDestination)
try {
    $travelEntries = @($travelZip.Entries | Where-Object { $_.Name })
    if ($travelEntries.Count -ne $travelFiles.Count) { throw 'Số tệp trong ZIP không khớp.' }
    foreach ($travelFile in $travelFiles) {
        $travelEntry = $travelZip.GetEntry($travelFile.Path)
        if (!$travelEntry) { throw "ZIP thiếu tệp $($travelFile.Path)" }
        $travelStream = $travelEntry.Open()
        try { $travelHash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($travelStream)) }
        finally { $travelStream.Dispose() }
        if ($travelHash -ne $travelFile.Sha256) { throw "SHA-256 không khớp: $($travelFile.Path)" }
    }
} finally { $travelZip.Dispose() }
[pscustomobject]@{ passed = $true; checks = $travelFiles.Count + 1; backup = $travelDestination; files = $travelFiles; date = [DateTime]::UtcNow.ToString('o') } |
    ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $travelRoot 'tests\companion-appdata-backup-results.json') -Encoding utf8
Write-Output "PASS App_Data backup: $($travelFiles.Count) files, SHA-256 verified"
