<#
.SYNOPSIS
    Builds a new DevToolboxSetup.exe installer end to end: publishes the self-contained
    single-file exe, optionally bumps the installer's version number, then compiles the
    installer with Inno Setup.

.DESCRIPTION
    Automates the manual steps previously listed in README.md's "Building an installer"
    section, so cutting a new release is one command instead of three.

.PARAMETER Version
    The version to stamp on this release (e.g. "1.1.0"), written into DevToolbox.iss's
    MyAppVersion define. Omit to reuse whatever version is already in the .iss file - the
    script prints it either way, so a forgotten bump is visible, not silent.

.EXAMPLE
    .\build-release.ps1 -Version 1.1.0

.EXAMPLE
    .\build-release.ps1
    # Reuses the current MyAppVersion - useful for rebuilding the same version after a
    # last-minute source fix, without touching the version number.
#>
param(
    [string]$Version
)

$ErrorActionPreference = "Stop"

$installerDir = $PSScriptRoot
$repoRoot = Split-Path $installerDir -Parent
$csproj = Join-Path $repoRoot "DevToolbox.csproj"
$issPath = Join-Path $installerDir "DevToolbox.iss"

# Same two candidate locations README.md documents for where winget/the Inno Setup installer
# puts ISCC.exe - checked in this order rather than relying on PATH, since the installer
# doesn't add itself to PATH by default.
$isccCandidates = @(
    "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe",
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
)
$iscc = $isccCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $iscc) {
    throw "Inno Setup's ISCC.exe wasn't found in either usual install location. Install it first: winget install JRSoftware.InnoSetup"
}

if ($Version) {
    Write-Host "Setting installer version to $Version..." -ForegroundColor Cyan
    $issContent = Get-Content $issPath -Raw
    $updated = $issContent -replace '#define MyAppVersion "[^"]*"', "#define MyAppVersion `"$Version`""
    if ($updated -eq $issContent) {
        throw "Couldn't find a MyAppVersion line in $issPath to update - check the file wasn't restructured."
    }
    Set-Content $issPath -Value $updated -NoNewline
}

$currentVersion = [regex]::Match((Get-Content $issPath -Raw), '#define MyAppVersion "([^"]*)"').Groups[1].Value
Write-Host "Building DevToolboxSetup.exe version $currentVersion" -ForegroundColor Cyan

Write-Host "`nStep 1/2: Publishing self-contained single-file exe (Release)..." -ForegroundColor Cyan
& dotnet publish $csproj -c Release -p:PublishProfile=SingleFile
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed (exit code $LASTEXITCODE)." }

Write-Host "`nStep 2/2: Compiling installer with Inno Setup..." -ForegroundColor Cyan
& $iscc $issPath
if ($LASTEXITCODE -ne 0) { throw "ISCC.exe failed (exit code $LASTEXITCODE)." }

$outputExe = Join-Path $installerDir "Output\DevToolboxSetup.exe"
Write-Host "`nDone - version $currentVersion built at:" -ForegroundColor Green
Write-Host $outputExe -ForegroundColor Green
