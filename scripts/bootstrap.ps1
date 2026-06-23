# One-time bootstrap for Gamehelper Core (re-runnable with -Force).
param(
    [string]$TargetRoot = "D:\Gamehelper Core",
    [string]$StableRoot = "D:\ZusatzProgramme\Gamehelper",
    [switch]$Force
)

$ErrorActionPreference = "Stop"

function Invoke-RobocopyMirror {
    param([string]$Source, [string]$Destination, [string[]]$ExcludeDirs = @("bin", "obj", ".git", ".vs", "publish"))

    $args = @($Source, $Destination, "/MIR", "/NFL", "/NDL", "/NJH", "/NJS", "/nc", "/ns", "/np")
    foreach ($dir in $ExcludeDirs) {
        $args += "/XD"
        $args += $dir
    }

    & robocopy @args | Out-Null
    if ($LASTEXITCODE -ge 8) {
        throw "robocopy failed ($LASTEXITCODE): $Source -> $Destination"
    }
}

function Install-PluginFromGit {
    param(
        [string]$Repo,
        [string]$Folder,
        [string]$PluginsRoot
    )

    $temp = Join-Path $env:TEMP "gh-core-bootstrap"
    New-Item -ItemType Directory -Force -Path $temp | Out-Null
    $cloneDir = Join-Path $temp $Folder
    if (Test-Path $cloneDir) { Remove-Item $cloneDir -Recurse -Force }

    git clone --depth 1 "https://github.com/$Repo.git" $cloneDir
    $dst = Join-Path $PluginsRoot $Folder
    if (Test-Path $dst) { Remove-Item $dst -Recurse -Force }

    if (Test-Path (Join-Path $cloneDir "Plugins\$Folder")) {
        Invoke-RobocopyMirror -Source (Join-Path $cloneDir "Plugins\$Folder") -Destination $dst
    }
    else {
        Invoke-RobocopyMirror -Source $cloneDir -Destination $dst
    }

    Write-Host "Installed $Folder from $Repo"
}

if ((Test-Path $TargetRoot) -and -not $Force) {
    Write-Host "Target exists: $TargetRoot (use -Force to re-bootstrap file copies)" -ForegroundColor Yellow
}

$gordinTemp = Join-Path $env:TEMP "gordin-gh2-bootstrap"
if (Test-Path $gordinTemp) { Remove-Item $gordinTemp -Recurse -Force }
git clone --depth 1 https://github.com/Gordin/GameHelper2.git $gordinTemp

New-Item -ItemType Directory -Force -Path $TargetRoot | Out-Null
Invoke-RobocopyMirror -Source $gordinTemp -Destination $TargetRoot
Write-Host "Gordin base copied"

# Launcher is maintained in Core (English-only); do not overwrite from Stable.
foreach ($dir in @("Shared", "Downloader")) {
    $src = Join-Path $StableRoot $dir
    if (Test-Path $src) {
        Invoke-RobocopyMirror -Source $src -Destination (Join-Path $TargetRoot $dir) -ExcludeDirs @("bin", "obj")
        Write-Host "Copied $dir from Stable"
    }
}

$pluginsRoot = Join-Path $TargetRoot "Plugins"
foreach ($remove in @("Atlas", "FarmTracker", "RuneforgeHelper", "MapKillCounter", "SamplePluginTemplate", "WorldDrawing")) {
    $path = Join-Path $pluginsRoot $remove
    if (Test-Path $path) { Remove-Item $path -Recurse -Force; Write-Host "Removed $remove" }
}

& (Join-Path $PSScriptRoot "sync-plugin-repos.ps1") -TargetRoot $TargetRoot -Set All

Write-Host ""
Write-Host "Bootstrap file copy complete." -ForegroundColor Green
Write-Host "Next: dotnet sln add (if needed), scripts\build.ps1, fix plugin API mismatches." -ForegroundColor Cyan
