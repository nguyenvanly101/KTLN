[CmdletBinding()]
param([switch]$Dev)

$ErrorActionPreference = 'Stop'
$travelProject = Join-Path $PSScriptRoot 'src\HoTroDuLich.Web\HoTroDuLich.Web.csproj'
$travelConfig = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'src\HoTroDuLich.Web\appsettings.json') | ConvertFrom-Json
$travelOriginalConnection = $env:ConnectionStrings__HoTroDuLich
$travelConnection = $env:ConnectionStrings__HoTroDuLich
if ([string]::IsNullOrWhiteSpace($travelConnection)) { $travelConnection = $travelConfig.ConnectionStrings.HoTroDuLich }
. (Join-Path $PSScriptRoot 'tools\ResolveLocalDb.ps1')
$travelResolved = Resolve-TravelLocalDbConnection $travelConnection
$travelExitCode=0
try {
    if($travelResolved.Fallback){$env:ConnectionStrings__HoTroDuLich=$travelResolved.ConnectionString}
    if ($Dev) {
        dotnet watch --project $travelProject run --launch-profile http
    } else {
        dotnet run --project $travelProject --launch-profile http
    }
    $travelExitCode=$LASTEXITCODE
} finally {
    $env:ConnectionStrings__HoTroDuLich=$travelOriginalConnection
}
exit $travelExitCode
