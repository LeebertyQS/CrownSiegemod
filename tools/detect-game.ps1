#requires -Version 5.1
<#
    detect-game.ps1 - locate the Crown Siege install on THIS machine and
    generate local.props so MSBuild can reference the game assemblies.

    Usage:
      powershell -ExecutionPolicy Bypass -File tools\detect-game.ps1
      powershell -ExecutionPolicy Bypass -File tools\detect-game.ps1 -GameHome "E:\Steam\steamapps\common\Crown Siege"

    Output: local.props at repo root (gitignored). Nothing else is touched.
#>
param(
    [string]$GameHome
)

$ErrorActionPreference = 'Stop'

$repoRoot      = Split-Path -Parent $PSScriptRoot
$expectedGuid  = 'd07d7b203ec34f6b9a35c7796923a472'
$expectedUnity = '6000.6.7f1'

function Test-GameHome {
    param([string]$Path)
    if ([string]::IsNullOrWhiteSpace($Path)) { return $false }
    $dll = Join-Path $Path 'CrownSiege_Data\Managed\Assembly-CSharp.dll'
    return (Test-Path -LiteralPath $dll)
}

function Get-SteamLibraryPaths {
    $libs = @()
    foreach ($key in @('HKCU:\Software\Valve\Steam', 'HKLM:\SOFTWARE\WOW6432Node\Valve\Steam')) {
        try {
            $steamPath = (Get-ItemProperty -Path $key -ErrorAction Stop).SteamPath
            if ($steamPath) {
                $steamPath = $steamPath -replace '/', '\'
                $libs += $steamPath
                $vdf = Join-Path $steamPath 'steamapps\libraryfolders.vdf'
                if (Test-Path -LiteralPath $vdf) {
                    $raw = Get-Content -LiteralPath $vdf -Raw
                    foreach ($m in [regex]::Matches($raw, '"path"\s*"([^"]+)"')) {
                        $p = $m.Groups[1].Value -replace '\\\\', '\'
                        if ($p) { $libs += $p }
                    }
                }
            }
        } catch { }
    }
    return ($libs | Select-Object -Unique)
}

function Find-GameHome {
    $candidates = New-Object System.Collections.Generic.List[string]
    if ($GameHome)                 { $candidates.Add($GameHome) }
    if ($env:CROWN_SIEGE_HOME)     { $candidates.Add($env:CROWN_SIEGE_HOME) }
    foreach ($lib in Get-SteamLibraryPaths) {
        $candidates.Add((Join-Path $lib 'steamapps\common\Crown Siege'))
    }
    $candidates.Add('C:\Program Files (x86)\Steam\steamapps\common\Crown Siege')
    $candidates.Add('D:\SteamLibrary\steamapps\common\Crown Siege')

    foreach ($c in $candidates) {
        if (Test-GameHome -Path $c) { return $c }
    }
    return $null
}

Write-Host '[1/4] searching for Crown Siege ...'
$found = Find-GameHome
if (-not $found) {
    Write-Host ''
    Write-Host 'FAIL: Crown Siege not found.' -ForegroundColor Red
    Write-Host 'Run again with an explicit path:'
    Write-Host '  powershell -ExecutionPolicy Bypass -File tools\detect-game.ps1 -GameHome "X:\...\steamapps\common\Crown Siege"'
    exit 1
}
Write-Host "      found: $found" -ForegroundColor Green

Write-Host '[2/4] checking game build fingerprint ...'
$boot = Join-Path $found 'CrownSiege_Data\boot.config'
$guid = $null
if (Test-Path -LiteralPath $boot) {
    $line = Select-String -LiteralPath $boot -Pattern 'build-guid=([0-9a-f]{32})' -AllMatches
    if ($line.Matches.Count -gt 0) { $guid = $line.Matches[0].Groups[1].Value }
}
if (-not $guid) {
    Write-Host '      WARN: build-guid not readable from boot.config' -ForegroundColor Yellow
} elseif ($guid -ne $expectedGuid) {
    Write-Host "      WARN: build-guid mismatch" -ForegroundColor Yellow
    Write-Host "            expected $expectedGuid"
    Write-Host "            actual   $guid"
    Write-Host '            The game was updated. Re-run the P0/P1/P2 probes before committing anything.'
} else {
    Write-Host "      build-guid OK ($guid)" -ForegroundColor Green
}

$mgr = Join-Path $found 'CrownSiege_Data\Managed'
$acd = Join-Path $mgr 'Assembly-CSharp.dll'
$size = (Get-Item -LiteralPath $acd).Length
Write-Host "[3/4] Assembly-CSharp.dll = $size bytes (Unity $expectedUnity expected)"

Write-Host '[4/4] writing local.props ...'
$escaped = $found.Replace('&', '&amp;')
$xml = @"
<Project>
  <PropertyGroup>
    <CrownSiegeHome>$escaped</CrownSiegeHome>
  </PropertyGroup>
</Project>
"@
$out = Join-Path $repoRoot 'local.props'
[System.IO.File]::WriteAllText($out, $xml, (New-Object System.Text.UTF8Encoding($false)))

Write-Host "      written: $out" -ForegroundColor Green
Write-Host ''
Write-Host 'DONE. Next: dotnet build src\Mod.csproj -c Debug'
