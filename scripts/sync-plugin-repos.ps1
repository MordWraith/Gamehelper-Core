# Clone or pull external plugin repositories into Plugins\ (see plugins-sources.json).
param(
    [string]$TargetRoot = (Split-Path $PSScriptRoot -Parent),
    [ValidateSet("MordWraith", "Upstream", "All")]
    [string]$Set = "All",
    [switch]$ForceReclone
)

$ErrorActionPreference = "Stop"
$SourcesPath = Join-Path $PSScriptRoot "plugins-sources.json"
if (-not (Test-Path $SourcesPath)) {
    throw "Missing $SourcesPath"
}

$sources = Get-Content $SourcesPath -Raw | ConvertFrom-Json
$pluginsRoot = Join-Path $TargetRoot "Plugins"
New-Item -ItemType Directory -Force -Path $pluginsRoot | Out-Null

function Sync-PluginRepo {
    param(
        [string]$Folder,
        [string]$RepoUrl
    )

    $dst = Join-Path $pluginsRoot $Folder
    if ($ForceReclone -and (Test-Path $dst)) {
        Remove-Item $dst -Recurse -Force
    }

    if (Test-Path (Join-Path $dst ".git")) {
        Write-Host "Pull $Folder ..."
        Push-Location $dst
        try {
            & git pull --ff-only
            if ($LASTEXITCODE -ne 0) {
                throw "git pull failed for $Folder (exit $LASTEXITCODE)"
            }
        }
        finally {
            Pop-Location
        }
        return
    }

    if (Test-Path $dst) {
        Write-Host "Replace copied folder with git clone: $Folder"
        Remove-Item $dst -Recurse -Force
    }

    Write-Host "Clone $Folder from $RepoUrl ..."
    & git clone --depth 1 $RepoUrl $dst
    if ($LASTEXITCODE -ne 0) {
        throw "git clone failed for $Folder (exit $LASTEXITCODE)"
    }
}

$map = @{}
if ($Set -eq "MordWraith" -or $Set -eq "All") {
    foreach ($prop in $sources.mordWraithForks.PSObject.Properties) {
        $map[$prop.Name] = [string]$prop.Value
    }
}
if ($Set -eq "Upstream" -or $Set -eq "All") {
    foreach ($prop in $sources.upstream.PSObject.Properties) {
        $map[$prop.Name] = [string]$prop.Value
    }
}

foreach ($entry in $map.GetEnumerator() | Sort-Object Name) {
    Sync-PluginRepo -Folder $entry.Key -RepoUrl $entry.Value
}

Write-Host ""
Write-Host "sync-plugin-repos complete ($Set)." -ForegroundColor Green
Write-Host "Rebuild: scripts\build.ps1" -ForegroundColor Cyan
