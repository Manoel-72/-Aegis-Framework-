param(
    [string]$Version = "0.9.9",
    [string]$Configuration = "Debug",
    [switch]$SkipVerify
)

$ErrorActionPreference = "Stop"

function Step($message) {
    Write-Host ""
    Write-Host "[package] $message" -ForegroundColor Cyan
}

function Fail($message) {
    Write-Host "[package] FAIL: $message" -ForegroundColor Red
    exit 1
}

$root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
Set-Location $root

$packageName = "Aegis-Framework-v$Version"
$distRoot = Join-Path $root "dist"
$stageParent = Join-Path $distRoot "framework-package-$Version"
$stage = Join-Path $stageParent $packageName
$zip = Join-Path $distRoot "$packageName.zip"

$distFull = [IO.Path]::GetFullPath($distRoot)
$stageParentFull = [IO.Path]::GetFullPath($stageParent)
$zipFull = [IO.Path]::GetFullPath($zip)

if (-not $stageParentFull.StartsWith($distFull, [StringComparison]::OrdinalIgnoreCase)) {
    Fail "Invalid staging path: $stageParentFull"
}

if (-not $zipFull.StartsWith($distFull, [StringComparison]::OrdinalIgnoreCase)) {
    Fail "Invalid zip path: $zipFull"
}

Step "Preparing dist folder"
New-Item -ItemType Directory -Force -Path $distRoot | Out-Null

if (Test-Path -LiteralPath $stageParent) {
    Remove-Item -LiteralPath $stageParent -Recurse -Force
}

if (Test-Path -LiteralPath $zip) {
    Remove-Item -LiteralPath $zip -Force
}

New-Item -ItemType Directory -Force -Path $stage | Out-Null

$excludeDirs = @(".git", ".vs", ".vscode", "bin", "obj", "dist", "archive", "jogos", "saves")
$excludeExt = @(".zip", ".log", ".tmp", ".bak", ".old", ".orig", ".user", ".suo", ".pdf", ".docx")
$copied = 0

Step "Copying clean package files"
Get-ChildItem -LiteralPath $root -Force -Recurse -File | ForEach-Object {
    $full = $_.FullName
    if (-not $full.StartsWith($root, [StringComparison]::OrdinalIgnoreCase)) { return }

    $rel = $full.Substring($root.Length).TrimStart("\", "/")
    if ([string]::IsNullOrWhiteSpace($rel)) { return }

    $segments = $rel -split "[\\/]"
    foreach ($segment in $segments) {
        if ($excludeDirs -contains $segment) { return }
    }

    if ($excludeExt -contains $_.Extension.ToLowerInvariant()) { return }

    $dest = Join-Path $stage $rel
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $dest) | Out-Null
    Copy-Item -LiteralPath $full -Destination $dest -Force
    $script:copied++
}

Step "Compressing $packageName.zip"
Compress-Archive -LiteralPath $stage -DestinationPath $zip -Force

Step "Cleaning staging folder"
Remove-Item -LiteralPath $stageParent -Recurse -Force

$item = Get-Item -LiteralPath $zip
Write-Host ("[package] Created: {0} ({1} MB, {2} files copied)" -f $item.FullName, ([math]::Round($item.Length / 1MB, 2)), $copied)

if (-not $SkipVerify) {
    Step "Running release verification"
    & (Join-Path $PSScriptRoot "verify.ps1") -Configuration $Configuration -ReleaseZip $zip
}

Write-Host ""
Write-Host "[package] OK" -ForegroundColor Green
