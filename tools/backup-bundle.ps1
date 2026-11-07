#requires -Version 5.1
<#
    backup-bundle.ps1 - pack the whole repo (all branches + tags + LFS pointers)
    into a single .bundle file you can upload to the cloud drive as an offline mirror.

    A bare dump is the ONLY safe way to use a file-sync drive as "remote":
    the drive never holds a live .git directory, so it can never be corrupted
    by two people writing at once.

    Usage:
      powershell -ExecutionPolicy Bypass -File tools\backup-bundle.ps1
      powershell -ExecutionPolicy Bypass -File tools\backup-bundle.ps1 -OutDir "D:\Backup"

    Restore:
      git clone <path-to>.bundle restored-repo
#>
param(
    [string]$OutDir
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot

if (-not (Test-Path -LiteralPath (Join-Path $repoRoot '.git'))) {
    Write-Host 'FAIL: not a git repository yet. Run: git init  (then commit at least once)' -ForegroundColor Red
    exit 1
}

$outDir = if ($OutDir) { $OutDir } else { Join-Path $repoRoot 'backup' }
if (-not (Test-Path -LiteralPath $outDir)) {
    New-Item -ItemType Directory -Path $outDir -Force | Out-Null
}

$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$name  = "crownsigemod-$stamp.bundle"
$file  = Join-Path $outDir $name

Write-Host "packing -> $file"
& git -C $repoRoot bundle create $file --all
if ($LASTEXITCODE -ne 0) { Write-Host 'FAIL: git bundle failed' -ForegroundColor Red; exit 1 }

$hash = (Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash
Set-Content -LiteralPath "$file.sha256" -Value "$hash  $name" -Encoding utf8

$mb = [math]::Round((Get-Item -LiteralPath $file).Length / 1MB, 2)
Write-Host "OK: $name ($mb MB)" -ForegroundColor Green
Write-Host "sha256: $hash"
Write-Host ''
Write-Host 'Next: upload BOTH files (.bundle + .sha256) to the cloud drive.'
Write-Host 'Restore with: git clone <file>.bundle <target-dir>'
