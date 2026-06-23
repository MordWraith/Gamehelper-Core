# Pull upstream Gordin/GameHelper2 into this fork (maintainer workflow).
param(
    [string]$TargetRoot = (Split-Path $PSScriptRoot -Parent),
    [ValidateSet("CoreOnly", "Plugins", "AllGordinPlugins")]
    [string]$Mode = "CoreOnly",
    [string[]]$PluginNames = @("AutoHotKeyTrigger", "Radar", "HealthBars", "PreloadAlert", "LootValue")
)

$ErrorActionPreference = "Stop"

function Invoke-RobocopyMirror {
    param([string]$Source, [string]$Destination)

    & robocopy $Source $Destination /MIR /XD bin obj .git .vs publish configs /NFL /NDL /NJH /NJS /nc /ns /np | Out-Null
    if ($LASTEXITCODE -ge 8) {
        throw "robocopy failed ($LASTEXITCODE): $Source -> $Destination"
    }
}

$upstream = Join-Path $env:TEMP "gordin-gh2-sync"
if (Test-Path $upstream) { Remove-Item $upstream -Recurse -Force }
git clone --depth 1 https://github.com/Gordin/GameHelper2.git $upstream

$coreDirs = @("GameHelper", "GameOffsets")
if ($Mode -eq "CoreOnly" -or $Mode -eq "Plugins" -or $Mode -eq "AllGordinPlugins") {
    foreach ($dir in $coreDirs) {
        Invoke-RobocopyMirror -Source (Join-Path $upstream $dir) -Destination (Join-Path $TargetRoot $dir)
        Write-Host "Synced $dir from Gordin"
    }
}

if ($Mode -eq "AllGordinPlugins") {
    $PluginNames = @("AutoHotKeyTrigger", "Radar", "HealthBars", "PreloadAlert", "LootValue")
}

if ($Mode -eq "Plugins" -or $Mode -eq "AllGordinPlugins") {
    foreach ($name in $PluginNames) {
        $src = Join-Path $upstream "Plugins\$name"
        $dst = Join-Path $TargetRoot "Plugins\$name"
        if (-not (Test-Path $src)) {
            Write-Warning "Gordin plugin not found: $name"
            continue
        }

        Invoke-RobocopyMirror -Source $src -Destination $dst
        Write-Host "Synced plugin $name from Gordin"
    }
}

Write-Host ""
Write-Host "sync-gordin complete ($Mode)." -ForegroundColor Green
Write-Host "Review diff, rebuild, test plugins (especially yokkenUA / Stable forks)." -ForegroundColor Cyan
