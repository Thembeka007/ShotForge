<#
.SYNOPSIS
    Builds a self-contained Windows installer for ShotForge using Velopack.

.DESCRIPTION
    Runs the two-step Velopack packaging process:
      1. dotnet publish  - produces a self-contained build (no .NET runtime
         required on the end user's machine)
      2. vpk pack        - wraps that build into an actual installer .exe

.PARAMETER Version
    The version number for this release, e.g. "1.0.0". Must be a plain
    semantic version (no "v" prefix). Increment this on every release -
    Velopack uses it to generate delta updates between versions.

.EXAMPLE
    .\build-installer.ps1 -Version 1.0.0
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$Version,

    [string]$AppId = "ShotForge"
)

$ErrorActionPreference = "Stop"

# The Velopack library (in ShotForge.App.csproj) and the vpk tool MUST be the
# same version. 1.2.0 is confirmed to exist for both on nuget.org.
$VelopackVersion = "1.2.0"

Write-Host "=== ShotForge Installer Build ===" -ForegroundColor Cyan
Write-Host "App ID:  $AppId"
Write-Host "Version: $Version"
Write-Host ""

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$csproj = Join-Path $scriptDir "src\ShotForge.App\ShotForge.App.csproj"
$publishDir = Join-Path $scriptDir "publish"

if (-not (Test-Path $csproj)) {
    throw "Could not find $csproj - run this script from the repo root (where ShotForge.sln lives)."
}

# Clean any previous publish output so stale files never end up in a
# release by accident.
if (Test-Path $publishDir) {
    Write-Host "Cleaning previous publish output..." -ForegroundColor DarkGray
    Remove-Item $publishDir -Recurse -Force
}

# Safety net: the project's Velopack package pin must match the tool version.
$csprojText = Get-Content $csproj -Raw
$pinPattern = 'Include="Velopack"\s+Version="' + [regex]::Escape($VelopackVersion) + '"'
if ($csprojText -notmatch $pinPattern) {
    throw "ShotForge.App.csproj does not reference Velopack $VelopackVersion. The library and vpk versions must match - update one to match the other."
}

Write-Host "Step 1/3: Publishing self-contained build (this can take a minute)..." -ForegroundColor Yellow
dotnet publish $csproj -r win-x64 --self-contained -o $publishDir -p:Version=$Version
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed - see output above." }

Write-Host ""
Write-Host "Step 2/3: Checking the vpk tool (needs version $VelopackVersion)..." -ForegroundColor Yellow

$vpkLine = dotnet tool list -g | Select-String "^vpk\s"
if (-not $vpkLine) {
    Write-Host "vpk not installed - downloading it now. This can take a few minutes on a slow" -ForegroundColor DarkGray
    Write-Host "connection and may print nothing until it finishes. Please wait..." -ForegroundColor DarkGray
    dotnet tool install -g vpk --version $VelopackVersion
    if ($LASTEXITCODE -ne 0) { throw "Failed to install vpk $VelopackVersion - see output above." }
} elseif ($vpkLine -notmatch [regex]::Escape($VelopackVersion)) {
    Write-Host "A different vpk version is installed - switching to $VelopackVersion..." -ForegroundColor DarkGray
    dotnet tool update -g vpk --version $VelopackVersion
    if ($LASTEXITCODE -ne 0) { throw "Failed to switch vpk to $VelopackVersion - see output above." }
} else {
    Write-Host "vpk $VelopackVersion already installed." -ForegroundColor DarkGray
}

Write-Host ""
Write-Host "Step 3/3: Packaging the installer..." -ForegroundColor Yellow
# Optional installer icon: if you add a file at assets\ShotForge.ico it is
# picked up automatically (vpk's --icon option). No file = default icon.
$iconPath = Join-Path $scriptDir "assets\ShotForge.ico"
$extraArgs = @()
if (Test-Path $iconPath) {
    Write-Host "Using installer icon: $iconPath" -ForegroundColor DarkGray
    $extraArgs += @("--icon", $iconPath)
}

vpk pack --packId $AppId --packVersion $Version --packDir $publishDir --mainExe "ShotForge.App.exe" --packTitle "ShotForge" @extraArgs
if ($LASTEXITCODE -ne 0) { throw "vpk pack failed - see output above." }

Write-Host ""
Write-Host "=== Done ===" -ForegroundColor Green
Write-Host "Your installer is in the 'Releases' folder vpk just created (inside this repo's root)." -ForegroundColor Green
Write-Host "Look for ShotForge-win-Setup.exe in there - that's the single file to share or upload." -ForegroundColor Green
Write-Host ""
Write-Host "To release an UPDATE later: bump -Version and run this script again." -ForegroundColor DarkGray
Write-Host "Keep the whole 'Releases' folder around (including old versions) -" -ForegroundColor DarkGray
Write-Host "Velopack needs the previous version's files present to generate delta updates." -ForegroundColor DarkGray
