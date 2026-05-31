param(
    [string]$Configuration = "Debug",
    [string]$ReleaseZip = "dist\Aegis-Framework-v0.9.9.zip",
    [switch]$SkipZip
)

$ErrorActionPreference = "Stop"

function Step($message) {
    Write-Host ""
    Write-Host "[verify] $message" -ForegroundColor Cyan
}

function Fail($message) {
    Write-Host "[verify] FAIL: $message" -ForegroundColor Red
    exit 1
}

function Require-File($path, $message) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        Fail $message
    }
}

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
Set-Location $root

Step "Repository root: $root"
Require-File "Aegis.sln" "Aegis.sln not found. Run this script from the repository package."
Require-File "tests\Aegis.Tests\Aegis.Tests.csproj" "Aegis.Tests project not found."

Step "Building solution"
dotnet build Aegis.sln -c $Configuration --no-restore

Step "Running automated tests"
dotnet run --project tests\Aegis.Tests\Aegis.Tests.csproj -c $Configuration --no-restore

if (-not $SkipZip) {
    Step "Validating release zip"
    Require-File $ReleaseZip "Release zip not found: $ReleaseZip"

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = [System.IO.Compression.ZipFile]::OpenRead((Resolve-Path $ReleaseZip))
    try {
        $names = $archive.Entries | ForEach-Object { $_.FullName -replace "\\", "/" }
        $required = @(
            "Aegis-Framework-v0.9.9/aegis.cmd",
            "Aegis-Framework-v0.9.9/VERSION",
            "Aegis-Framework-v0.9.9/INSTALL_0.9.9.md",
            "Aegis-Framework-v0.9.9/RELEASE_NOTES_0.9.9.md",
            "Aegis-Framework-v0.9.9/src/Aegis/Aegis.csproj",
            "Aegis-Framework-v0.9.9/src/Aegis.CLI/Aegis.CLI.csproj",
            "Aegis-Framework-v0.9.9/src/Aegis/Scripting/LuaRuntime.CoreApi.cs",
            "Aegis-Framework-v0.9.9/src/Aegis/Scripting/LuaRuntime.DisplayApi.cs",
            "Aegis-Framework-v0.9.9/src/Aegis/Scripting/LuaRuntime.GameplayApi.cs",
            "Aegis-Framework-v0.9.9/tests/Aegis.Tests/Aegis.Tests.csproj",
            "Aegis-Framework-v0.9.9/scripts/verify.ps1",
            "Aegis-Framework-v0.9.9/scripts/package-release.ps1",
            "Aegis-Framework-v0.9.9/templates/platformer/main.lua",
            "Aegis-Framework-v0.9.9/examples/demo-platformer/main.lua",
            "Aegis-Framework-v0.9.9/docs/MVP_API.md",
            "Aegis-Framework-v0.9.9/docs/RELEASE_CHECKLIST_0.9.9.md"
        )

        $missing = $required | Where-Object { $names -notcontains $_ }
        if ($missing) {
            Fail ("Release zip is missing required files: " + ($missing -join "; "))
        }

        $forbidden = $names | Where-Object {
            $_ -match '(^|/)(\.git|\.vs|\.vscode|bin|obj|dist|archive|jogos|saves)(/|$)' -or
            $_ -match '\.(zip|log|tmp|bak|old|orig|user|suo|pdf|docx)$'
        }
        if ($forbidden) {
            Fail ("Release zip contains forbidden entries: " + (($forbidden | Select-Object -First 20) -join "; "))
        }

        $zipItem = Get-Item -LiteralPath $ReleaseZip
        Write-Host ("[verify] Release zip OK: {0} MB, {1} entries" -f ([math]::Round($zipItem.Length / 1MB, 2)), $names.Count)
    }
    finally {
        $archive.Dispose()
    }
}

Write-Host ""
Write-Host "[verify] OK" -ForegroundColor Green
