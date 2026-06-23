# Gamehelper Core - maintenance hub (build, sync, publish, git).
# Usage:
#   maintain.cmd              GUI (Standard)
#   maintain.cmd -Console     Textmenue
#   maintain.cmd -Gui         GUI explizit
#   maintain.cmd -Action Build -Configuration Release
param(
    [ValidateSet(
        "Menu", "Help", "Status", "Build", "Run",
        "SyncGordinCore", "SyncGordinPlugins", "SyncGordinAll",
        "SyncPlugins", "SyncPluginsMordWraith", "SyncPluginsUpstream",
        "PushMordWraithPlugins", "SigningKey", "SetupGithubConfig",
        "Publish", "PushSource", "VerifyPublish", "BuildDownloader",
        "OpenPublishFolder", "OpenProjectFolder", "GhAuthLogin"
    )]
    [string]$Action = "Menu",
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [string]$Version,
    [switch]$Gui,
    [switch]$Console,
    [switch]$ForceReclonePlugins
)

$ErrorActionPreference = "Stop"
$Root = $PSScriptRoot
$Scripts = Join-Path $Root "scripts"

function Write-Title([string]$Text) {
    $line = (" " * [Math]::Max(0, 78 - $Text.Length)) + $Text
    Write-Host ""
    Write-Host ("  +{0}+" -f ("-" * 78)) -ForegroundColor DarkCyan
    Write-Host ("  |{0}|" -f $line.Substring([Math]::Max(0, $line.Length - 78))) -ForegroundColor Cyan
    Write-Host ("  +{0}+" -f ("-" * 78)) -ForegroundColor DarkCyan
}

function Write-Section([string]$Text) {
    Write-Host ""
    Write-Host "  $Text" -ForegroundColor Yellow
    Write-Host ("  " + ("-" * ($Text.Length + 2))) -ForegroundColor DarkYellow
}

function Invoke-Script {
    param(
        [string]$Path,
        [hashtable]$Arguments = @{}
    )

    if (-not (Test-Path $Path)) {
        throw "Script not found: $Path"
    }

    $argList = @()
    foreach ($key in $Arguments.Keys) {
        $val = $Arguments[$key]
        if ($val -is [switch] -and $val) {
            $argList += "-$key"
        }
        elseif ($null -ne $val -and "$val" -ne "") {
            $argList += "-$key"
            $argList += $val
        }
    }

    Write-Host ""
    Write-Host "  > $Path $($argList -join ' ')" -ForegroundColor DarkGray
    & $Path @argList
    if ($LASTEXITCODE -and $LASTEXITCODE -ne 0) {
        throw "Script failed (exit $LASTEXITCODE): $Path"
    }
}

function Get-ProjectVersion {
    $csproj = Join-Path $Root "GameHelper\GameHelper.csproj"
    if (-not (Test-Path $csproj)) { return "?" }
    $xml = Get-Content $csproj -Raw
    if ($xml -match '<Version>([^<]+)</Version>') { return $Matches[1].Trim() }
    return "?"
}

function Get-StatusLinesDe {
    $ver = Get-ProjectVersion
    $repo = Get-GithubRepository
    $publishExe = Join-Path $Root "publish\GameHelper.exe"
    $signKey = Test-Path (Join-Path $Root "update-signing.key")
    $signPub = Test-Path (Join-Path $Root "update-signing.pub")
    $gh = [bool](Get-Command gh -ErrorAction SilentlyContinue)
    $ghAuth = $false
    if ($gh) {
        gh auth status 2>$null | Out-Null
        $ghAuth = $LASTEXITCODE -eq 0
    }

    $gitState = "kein Git-Repo"
    $gitDir = Join-Path $Root ".git"
    if (Test-Path $gitDir) {
        $hasBranch = -not [string]::IsNullOrWhiteSpace(
            (Get-ChildItem (Join-Path $gitDir "refs\heads") -ErrorAction SilentlyContinue | Select-Object -First 1))
        if (-not $hasBranch) {
            $gitState = "initialisiert (noch keine Commits)"
        }
        else {
            Push-Location $Root
            try {
                $branch = (git rev-parse --abbrev-ref HEAD 2>$null)
                $dirty = (git status --porcelain 2>$null)
                $gitState = if ($dirty) { "$branch (uncommittete Aenderungen)" } else { "$branch (sauber)" }
            }
            finally {
                Pop-Location
            }
        }
    }

    $repoDe = if ($repo -like "*(not configured*") { "nicht eingerichtet" } else { $repo }

    return @(
        "Version: $ver  |  Update-Repo: $repoDe",
        "publish: $(if (Test-Path $publishExe) { 'OK' } else { 'fehlt' })  |  Signatur: $(if ($signKey -and $signPub) { 'OK' } else { 'fehlt' })",
        "GitHub CLI: $(if ($ghAuth) { 'angemeldet' } elseif ($gh) { 'nicht angemeldet' } else { 'fehlt' })  |  Core-Git: $gitState"
    )
}

function Get-ReleaseChecklistDe {
    $cfg = Test-Path (Join-Path $Root "github.config.json")
    $sign = (Test-Path (Join-Path $Root "update-signing.key")) -and (Test-Path (Join-Path $Root "update-signing.pub"))
    $pub = Test-Path (Join-Path $Root "publish\GameHelper.exe")
    $gh = $false
    if (Get-Command gh -ErrorAction SilentlyContinue) {
        gh auth status 2>$null | Out-Null
        $gh = $LASTEXITCODE -eq 0
    }
    $gitDir = Join-Path $Root ".git"
    $hasCommits = $false
    if (Test-Path $gitDir) {
        $hasCommits = -not [string]::IsNullOrWhiteSpace(
            (Get-ChildItem (Join-Path $gitDir "refs\heads") -ErrorAction SilentlyContinue | Select-Object -First 1))
    }

    return @(
        @{ Ok = $cfg; Text = "github.config.json -> MordWraith/Gamehelper-Core" },
        @{ Ok = $sign; Text = "Signatur-Schluessel (eigen fuer Core, nicht Stable!)" },
        @{ Ok = $gh; Text = "gh auth login erledigt" },
        @{ Ok = $pub; Text = "Release-Build in publish\" },
        @{ Ok = $hasCommits; Text = "Core-Quellcode auf GitHub (mind. 1 Commit)" }
    )
}

function Get-GithubRepository {
    $cfgPath = Join-Path $Root "github.config.json"
    if (Test-Path $cfgPath) {
        $cfg = Get-Content $cfgPath -Raw | ConvertFrom-Json
        if ($cfg.repository) { return [string]$cfg.repository }
    }
    return "(not configured - use Setup github.config)"
}

function Get-StatusLines {
    $ver = Get-ProjectVersion
    $repo = Get-GithubRepository
    $publishExe = Join-Path $Root "publish\GameHelper.exe"
    $signKey = Test-Path (Join-Path $Root "update-signing.key")
    $signPub = Test-Path (Join-Path $Root "update-signing.pub")
    $gh = [bool](Get-Command gh -ErrorAction SilentlyContinue)
    $ghAuth = $false
    if ($gh) {
        gh auth status 2>$null | Out-Null
        $ghAuth = $LASTEXITCODE -eq 0
    }

    $gitState = "no repo"
    $gitDir = Join-Path $Root ".git"
    if (Test-Path $gitDir) {
        $hasBranch = -not [string]::IsNullOrWhiteSpace(
            (Get-ChildItem (Join-Path $gitDir "refs\heads") -ErrorAction SilentlyContinue | Select-Object -First 1))
        if (-not $hasBranch) {
            $gitState = "initialized (no commits yet)"
        }
        else {
            Push-Location $Root
            try {
                $branch = (git rev-parse --abbrev-ref HEAD 2>$null)
                $dirty = (git status --porcelain 2>$null)
                $gitState = if ($dirty) { "$branch (uncommitted changes)" } else { "$branch (clean)" }
            }
            finally {
                Pop-Location
            }
        }
    }

    $publishState = if (Test-Path $publishExe) { "built" } else { "missing - run Build" }
    $signState = if ($signKey -and $signPub) { "present" } else { "missing - run Signing key" }
    if ($ghAuth) { $ghState = "authenticated" }
    elseif ($gh) { $ghState = "installed, not logged in" }
    else { $ghState = "not installed" }

    return @(
        "Version (csproj):     $ver",
        "Update repository:    $repo",
        "publish\GameHelper:   $publishState",
        "Signing key:          $signState",
        "GitHub CLI:           $ghState",
        "Core git:             $gitState"
    )
}

function Show-Status {
    Write-Title "Gamehelper Core - Status"
    foreach ($line in (Get-StatusLines)) {
        Write-Host "  $line"
    }
}

function Show-WorkflowHelp {
    Write-Title "When to do what"
    Write-Section "Every day (local development)"
    Write-Host @"
  1. Sync plugin repos (git pull) - after you or others pushed plugin changes on GitHub.
  2. Build (Release) - after ANY code change (core, launcher, plugins).
  3. Run publish\GameHelper.exe - smoke-test launcher + overlay in game.
"@
    Write-Section "After Gordin upstream updates (GameHelper2)"
    Write-Host @"
  * Sync Gordin - Core only: GameHelper + GameOffsets from github.com/Gordin/GameHelper2
  * Sync Gordin - Plugins: AutoHotKeyTrigger, Radar, HealthBars, PreloadAlert, LootValue
  * Then: Build + test. Check yokkenUA and MordWraith forks still compile and work.
  * Do NOT run full bootstrap unless you intentionally reset the tree.
"@
    Write-Section "After editing a MordWraith plugin"
    Write-Host @"
  * Change files under Plugins\<Name>\ (each has its own .git remote).
  * Push MordWraith plugin repos - commits + pushes only folders with local changes.
  * Then Build so publish\ picks up new DLLs.
"@
    Write-Section "First release on a new machine (one-time)"
    Write-Host @"
  1. Setup github.config.json -> repository: MordWraith/Gamehelper-Core
  2. Generate update signing key (new key for Core - do not reuse Stable key)
  3. gh auth login
  4. Push source to GitHub (creates/commits core tree, excludes publish/ and secrets)
  5. Publish release - build + signed ZIP + manifest on GitHub Releases
  6. Verify publish - checks release tag, assets, and source branch
"@
    Write-Section "Normal version release (e.g. 1.0.1)"
    Write-Host @"
  1. Bump version in GameHelper\GameHelper.csproj (or pass -Version to Publish)
  2. Build + test locally
  3. Push source to GitHub
  4. Publish release with matching version
  5. Verify publish
  Users get the update via the launcher (signed manifest ZIP).
"@
    Write-Section "What NOT to mix up"
    Write-Host @"
  * Gamehelper-Core = THIS distribution (signed ZIP updates)
  * MordWraith/Gamehelper = old Stable repo - do not publish Core there
  * Plugin repos = separate GitHub repos; core repo does not replace them
  * bootstrap.ps1 = full tree reset from Gordin + Stable downloader - rarely needed
"@
}

function Invoke-MaintainAction {
    param([string]$Name)

    switch ($Name) {
        "Status" { Show-Status; return }
        "Help" { Show-WorkflowHelp; return }
        "Build" {
            Invoke-Script (Join-Path $Scripts "build.ps1") @{ Configuration = $Configuration }
            Write-Host ""
            Write-Host "  Build OK -> $Root\publish\GameHelper.exe" -ForegroundColor Green
            return
        }
        "Run" {
            $exe = Join-Path $Root "publish\GameHelper.exe"
            if (-not (Test-Path $exe)) { throw "Not built yet. Run Build first." }
            Start-Process $exe -WorkingDirectory (Join-Path $Root "publish")
            return
        }
        "SyncGordinCore" {
            Invoke-Script (Join-Path $Scripts "sync-gordin.ps1") @{ Mode = "CoreOnly" }
            return
        }
        "SyncGordinPlugins" {
            Invoke-Script (Join-Path $Scripts "sync-gordin.ps1") @{ Mode = "Plugins" }
            return
        }
        "SyncGordinAll" {
            Invoke-Script (Join-Path $Scripts "sync-gordin.ps1") @{ Mode = "AllGordinPlugins" }
            return
        }
        "SyncPlugins" {
            $args = @{ Set = "All" }
            if ($ForceReclonePlugins) { $args.ForceReclone = $true }
            Invoke-Script (Join-Path $Scripts "sync-plugin-repos.ps1") $args
            return
        }
        "SyncPluginsMordWraith" {
            $args = @{ Set = "MordWraith" }
            if ($ForceReclonePlugins) { $args.ForceReclone = $true }
            Invoke-Script (Join-Path $Scripts "sync-plugin-repos.ps1") $args
            return
        }
        "SyncPluginsUpstream" {
            $args = @{ Set = "Upstream" }
            if ($ForceReclonePlugins) { $args.ForceReclone = $true }
            Invoke-Script (Join-Path $Scripts "sync-plugin-repos.ps1") $args
            return
        }
        "PushMordWraithPlugins" {
            $sourcesPath = Join-Path $Scripts "plugins-sources.json"
            $sources = Get-Content $sourcesPath -Raw | ConvertFrom-Json
            $identity = $null
            if (Get-Command gh -ErrorAction SilentlyContinue) {
                $user = gh api user 2>$null | ConvertFrom-Json
                if ($user) {
                    $email = $user.email
                    if ([string]::IsNullOrWhiteSpace($email)) {
                        $email = "{0}+{1}@users.noreply.github.com" -f $user.id, $user.login
                    }
                    $identity = @{ Name = $user.login; Email = $email }
                }
            }
            if (-not $identity) {
                $identity = @{ Name = "MordWraith"; Email = "mordwraith@users.noreply.github.com" }
            }

            $env:GIT_AUTHOR_NAME = $identity.Name
            $env:GIT_AUTHOR_EMAIL = $identity.Email
            $env:GIT_COMMITTER_NAME = $identity.Name
            $env:GIT_COMMITTER_EMAIL = $identity.Email

            $pushed = 0
            foreach ($prop in $sources.mordWraithForks.PSObject.Properties) {
                $folder = $prop.Name
                $dir = Join-Path $Root "Plugins\$folder"
                if (-not (Test-Path (Join-Path $dir ".git"))) {
                    Write-Host "  skip $folder (no .git)" -ForegroundColor DarkGray
                    continue
                }

                Push-Location $dir
                $status = git status --porcelain 2>$null
                if (-not $status) {
                    Write-Host "  skip $folder (clean)" -ForegroundColor DarkGray
                    Pop-Location
                    continue
                }

                Write-Host "  push $folder ..." -ForegroundColor Cyan
                git add -A
                $commitScript = Join-Path $Scripts "Invoke-GitCommitClean.ps1"
                & $commitScript -Message "Update from Gamehelper Core workspace." -WorkingDirectory $dir `
                    -AuthorName $identity.Name -AuthorEmail $identity.Email
                if ($LASTEXITCODE -ne 0) {
                    Pop-Location
                    throw "git commit failed: $folder"
                }
                git push
                if ($LASTEXITCODE -ne 0) {
                    Pop-Location
                    throw "git push failed: $folder"
                }
                $pushed++
                Pop-Location
            }

            Write-Host ""
            Write-Host "  Pushed $pushed plugin repo(s)." -ForegroundColor Green
            return
        }
        "SigningKey" {
            Invoke-Script (Join-Path $Scripts "ensure-update-signing-key.ps1") @{}
            Write-Host "  Rebuild and publish after generating a NEW key." -ForegroundColor Yellow
            return
        }
        "SetupGithubConfig" {
            $example = Join-Path $Root "github.config.example.json"
            $target = Join-Path $Root "github.config.json"
            if (Test-Path $target) {
                Write-Host "  Already exists: github.config.json" -ForegroundColor Yellow
                Get-Content $target | Write-Host
                return
            }
            if (-not (Test-Path $example)) {
                throw "Missing github.config.example.json"
            }
            Copy-Item $example $target
            Write-Host "  Created github.config.json from example." -ForegroundColor Green
            Get-Content $target | Write-Host
            return
        }
        "Publish" {
            if ([string]::IsNullOrWhiteSpace($Version) -or $Version -eq "?") {
                $Version = Get-ProjectVersion
            }
            if ([string]::IsNullOrWhiteSpace($Version) -or $Version -eq "?") {
                $Version = Read-Host "  Release version (e.g. 1.0.0)"
            }
            if ([string]::IsNullOrWhiteSpace($Version)) { throw "Version required." }
            Write-Host "  Publishing version $Version" -ForegroundColor Cyan
            Invoke-Script (Join-Path $Scripts "publish.ps1") @{
                Version = $Version
                Configuration = $Configuration
            }
            return
        }
        "PushSource" {
            $args = @{}
            if (-not [string]::IsNullOrWhiteSpace($Version)) { $args.Version = $Version }
            Invoke-Script (Join-Path $Scripts "push-github-source.ps1") $args
            return
        }
        "VerifyPublish" {
            if ([string]::IsNullOrWhiteSpace($Version)) {
                $Version = Get-ProjectVersion
            }
            Invoke-Script (Join-Path $Scripts "verify-github-publish.ps1") @{
                ExpectedVersion = $Version
            }
            return
        }
        "BuildDownloader" {
            Invoke-Script (Join-Path $Scripts "build-downloader.ps1") @{}
            return
        }
        "OpenPublishFolder" {
            $dir = Join-Path $Root "publish"
            if (-not (Test-Path $dir)) { throw "publish\ existiert noch nicht. Zuerst bauen." }
            Start-Process explorer.exe $dir
            return
        }
        "OpenProjectFolder" {
            Start-Process explorer.exe $Root
            return
        }
        "GhAuthLogin" {
            if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
                throw "GitHub CLI fehlt. Install: winget install GitHub.cli"
            }
            Start-Process gh -ArgumentList "auth", "login" -Wait:$false
            Write-Host "  gh auth login in neuem Fenster gestartet."
            return
        }
        default { throw "Unknown action: $Name" }
    }
}

function Show-Menu {
    while ($true) {
        Clear-Host
        Write-Title "Gamehelper Core - Maintenance Hub"
        foreach ($line in (Get-StatusLines)) {
            Write-Host "  $line" -ForegroundColor DarkGray
        }

        Write-Section "Development"
        Write-Host '  [1] Build (Release)          compile solution -> publish\'
        Write-Host '  [2] Build (Debug)'
        Write-Host '  [3] Run GameHelper           start publish\GameHelper.exe'
        Write-Host "  [4] Sync plugin repos (all)  git pull MordWraith + yokkenUA clones"

        Write-Section "Gordin upstream (GameHelper2)"
        Write-Host "  [5] Sync Gordin - core only  GameHelper + GameOffsets"
        Write-Host "  [6] Sync Gordin - plugins    AHK, Radar, HealthBars, Preload, LootValue"
        Write-Host "  [7] Sync Gordin - all        core + all Gordin plugins"

        Write-Section "MordWraith plugins"
        Write-Host "  [8] Sync MordWraith plugins only"
        Write-Host "  [9] Push changed MordWraith plugin repos to GitHub"

        Write-Section "Release & GitHub"
        Write-Host " [10] Setup github.config.json  (one-time, points to Gamehelper-Core)"
        Write-Host " [11] Generate signing key      (one-time per update channel)"
        Write-Host " [12] Publish GitHub release    build + signed ZIP + manifest"
        Write-Host " [13] Push core source to GitHub"
        Write-Host " [14] Verify last publish"
        Write-Host " [15] Build downloader"

        Write-Section "Info"
        Write-Host " [H] Workflow guide - when to do what"
        Write-Host " [S] Refresh status"
        Write-Host " [Q] Quit"

        Write-Host ""
        $choice = Read-Host "  Choice"
        try {
            switch ($choice.ToUpperInvariant()) {
                "1" { $script:Configuration = "Release"; Invoke-MaintainAction "Build" }
                "2" { $script:Configuration = "Debug"; Invoke-MaintainAction "Build" }
                "3" { Invoke-MaintainAction "Run" }
                "4" { Invoke-MaintainAction "SyncPlugins" }
                "5" { Invoke-MaintainAction "SyncGordinCore" }
                "6" { Invoke-MaintainAction "SyncGordinPlugins" }
                "7" { Invoke-MaintainAction "SyncGordinAll" }
                "8" { Invoke-MaintainAction "SyncPluginsMordWraith" }
                "9" { Invoke-MaintainAction "PushMordWraithPlugins" }
                "10" { Invoke-MaintainAction "SetupGithubConfig" }
                "11" { Invoke-MaintainAction "SigningKey" }
                "12" {
                    $v = Read-Host "  Version (empty = use csproj)"
                    if ($v) { $script:Version = $v }
                    Invoke-MaintainAction "Publish"
                }
                "13" { Invoke-MaintainAction "PushSource" }
                "14" {
                    $v = Read-Host "  Version to verify (empty = csproj)"
                    if ($v) { $script:Version = $v }
                    Invoke-MaintainAction "VerifyPublish"
                }
                "15" { Invoke-MaintainAction "BuildDownloader" }
                "H" { Clear-Host; Show-WorkflowHelp }
                "S" { continue }
                "Q" { return }
                default { Write-Host "  Unknown choice." -ForegroundColor Red }
            }
        }
        catch {
            Write-Host ""
            Write-Host "  ERROR: $($_.Exception.Message)" -ForegroundColor Red
        }

        if ($choice.ToUpperInvariant() -ne "S" -and $choice.ToUpperInvariant() -ne "Q") {
            Write-Host ""
            Read-Host "  Press Enter to continue"
        }
    }
}

$script:MaintainGui = $null

function Update-MaintainGuiStatus {
    if (-not $script:MaintainGui) { return }
    $script:MaintainGui.Status.Text = (Get-StatusLinesDe) -join [Environment]::NewLine
}

function Update-MaintainGuiChecklist {
    $lb = $script:MaintainGui.ChecklistListBox
    if (-not $lb) { return }
    $lb.BeginUpdate()
    $lb.Items.Clear()
    foreach ($item in Get-ReleaseChecklistDe) {
        $prefix = if ($item.Ok) { "OK    " } else { "OFFEN " }
        [void]$lb.Items.Add("$prefix$($item.Text)")
    }
    $lb.EndUpdate()
}

function Invoke-MaintainGuiAction {
    param(
        [string]$ActionName,
        [string]$ExtraArgs = ""
    )

    $g = $script:MaintainGui
    if (-not $g) { return }

    $g.Form.UseWaitCursor = $true
    $g.Tabs.Enabled = $false
    $g.Log.Clear()
    $g.Log.AppendText("Starte: $ActionName ...`r`n")
    $g.Form.Refresh()

    try {
        if ($ActionName -eq "Status") {
            Update-MaintainGuiStatus
            Update-MaintainGuiChecklist
            $g.Log.AppendText("Status aktualisiert.`r`n")
            return
        }

        $maintainPath = Join-Path $g.Root "maintain.ps1"
        $argLine = "-NoProfile -ExecutionPolicy Bypass -File `"$maintainPath`" -Console -Action $ActionName"
        if ($ExtraArgs) { $argLine += " $ExtraArgs" }
        $psi = New-Object System.Diagnostics.ProcessStartInfo
        $psi.FileName = "powershell.exe"
        $psi.Arguments = $argLine
        $psi.WorkingDirectory = $g.Root
        $psi.UseShellExecute = $false
        $psi.RedirectStandardOutput = $true
        $psi.RedirectStandardError = $true
        $psi.CreateNoWindow = $true
        $p = [System.Diagnostics.Process]::Start($psi)
        $stdout = $p.StandardOutput.ReadToEnd()
        $stderr = $p.StandardError.ReadToEnd()
        $p.WaitForExit()

        if ($stdout) { $g.Log.AppendText($stdout.TrimEnd() + "`r`n") }
        if ($stderr) { $g.Log.AppendText($stderr.TrimEnd() + "`r`n") }

        if ($p.ExitCode -ne 0) {
            $g.Log.AppendText("Beendet mit Fehlercode $($p.ExitCode).`r`n")
            [System.Windows.Forms.MessageBox]::Show(
                "Aktion fehlgeschlagen. Details siehe Ausgabe unten.",
                "Fehler",
                [System.Windows.Forms.MessageBoxButtons]::OK,
                [System.Windows.Forms.MessageBoxIcon]::Warning) | Out-Null
        }
        else {
            $g.Log.AppendText("Fertig.`r`n")
        }
    }
    catch {
        $g.Log.AppendText("FEHLER: $($_.Exception.Message)`r`n")
        [System.Windows.Forms.MessageBox]::Show($_.Exception.Message, "Fehler") | Out-Null
    }
    finally {
        $g.Form.UseWaitCursor = $false
        $g.Tabs.Enabled = $true
        Update-MaintainGuiStatus
        Update-MaintainGuiChecklist
    }
}

function Show-Gui {
    Add-Type -AssemblyName System.Windows.Forms
    Add-Type -AssemblyName System.Drawing

    try {
        if (-not ("Win32Console" -as [type])) {
            Add-Type @'
using System;
using System.Runtime.InteropServices;
public class Win32Console {
    [DllImport("kernel32.dll")] public static extern IntPtr GetConsoleWindow();
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
}
'@
        }
        $consoleHwnd = [Win32Console]::GetConsoleWindow()
        if ($consoleHwnd -ne [IntPtr]::Zero) {
            [void][Win32Console]::ShowWindow($consoleHwnd, 0)
        }
    }
    catch { }

    $uiBg = [System.Drawing.Color]::FromArgb(26, 28, 34)
    $uiPanel = [System.Drawing.Color]::FromArgb(32, 35, 44)
    $uiText = [System.Drawing.Color]::FromArgb(235, 237, 245)
    $uiMuted = [System.Drawing.Color]::FromArgb(170, 175, 190)
    $uiAccent = [System.Drawing.Color]::FromArgb(92, 140, 240)
    $uiBtn = [System.Drawing.Color]::FromArgb(55, 58, 72)
    $uiOk = [System.Drawing.Color]::FromArgb(100, 200, 120)
    $uiWarn = [System.Drawing.Color]::FromArgb(255, 200, 80)

    $form = New-Object System.Windows.Forms.Form
    $form.Text = "Gamehelper Core - Wartung & Releases"
    $form.Size = New-Object System.Drawing.Size(980, 820)
    $form.MinimumSize = New-Object System.Drawing.Size(900, 720)
    $form.StartPosition = "CenterScreen"
    $form.BackColor = $uiBg
    $form.ForeColor = $uiText
    $form.Font = New-Object System.Drawing.Font("Segoe UI", 9.5)
    $form.Padding = New-Object System.Windows.Forms.Padding(12, 10, 12, 6)

    $topPanel = New-Object System.Windows.Forms.Panel
    $topPanel.Dock = "Top"
    $topPanel.Height = 128
    $topPanel.BackColor = $uiBg
    # Dock order: add Top panels later = closer to form top (header above action bar).

    $header = New-Object System.Windows.Forms.Label
    $header.Text = "Dein Wartungs-Assistent fuer Gamehelper Core (ohne Coding - nur Build, Sync, GitHub, Releases)"
    $header.Dock = "Top"
    $header.Height = 34
    $header.Padding = New-Object System.Windows.Forms.Padding(2, 0, 2, 4)
    $header.ForeColor = $uiMuted
    $header.Font = New-Object System.Drawing.Font("Segoe UI", 9.5)
    $topPanel.Controls.Add($header)

    $statusFrame = New-Object System.Windows.Forms.Panel
    $statusFrame.Dock = "Top"
    $statusFrame.Height = 82
    $statusFrame.BackColor = [System.Drawing.Color]::FromArgb(18, 20, 26)
    $statusFrame.BorderStyle = "FixedSingle"
    $topPanel.Controls.Add($statusFrame)

    $status = New-Object System.Windows.Forms.Label
    $status.Location = New-Object System.Drawing.Point -ArgumentList 10, 8
    $status.Size = New-Object System.Drawing.Size -ArgumentList 920, 66
    $status.Anchor = "Top, Left, Right"
    $status.ForeColor = $uiText
    $status.Font = New-Object System.Drawing.Font("Consolas", 9)
    $status.TextAlign = [System.Drawing.ContentAlignment]::TopLeft
    $statusFrame.Controls.Add($status)
    $statusFrame.Add_Resize({
        $innerW = [Math]::Max(100, $statusFrame.ClientSize.Width - 20)
        $status.Width = $innerW
    })

    $actionBar = New-Object System.Windows.Forms.Panel
    $actionBar.Dock = "Top"
    $actionBar.Height = 52
    $actionBar.BackColor = [System.Drawing.Color]::FromArgb(40, 44, 56)
    $actionBar.Padding = New-Object System.Windows.Forms.Padding(8, 8, 8, 6)
    # Added to form after topPanel (see dock order below).

    $actionHint = New-Object System.Windows.Forms.Label
    $actionHint.Text = "Hauptaktionen:"
    $actionHint.Location = New-Object System.Drawing.Point -ArgumentList 10, 14
    $actionHint.AutoSize = $true
    $actionHint.ForeColor = $uiMuted
    $actionHint.Font = New-Object System.Drawing.Font("Segoe UI", 9, [System.Drawing.FontStyle]::Bold)
    $actionBar.Controls.Add($actionHint)

    $actionX = 118
    foreach ($ad in @(
            @{ T = "Build"; A = "Build"; E = "" },
            @{ T = "Plugins pullen"; A = "SyncPlugins"; E = "" },
            @{ T = "Plugins pushen"; A = "PushMordWraithPlugins"; E = "" },
            @{ T = "Source pushen"; A = "PushSource"; E = "" },
            @{ T = "Release publishen"; A = "Publish"; E = ""; Accent = $true },
            @{ T = "Release pruefen"; A = "VerifyPublish"; E = "VERSION" }
        )) {
        $ab = New-Object System.Windows.Forms.Button
        $ab.Text = $ad.T
        $ab.Location = New-Object System.Drawing.Point -ArgumentList $actionX, 10
        $ab.Size = New-Object System.Drawing.Size -ArgumentList 132, 30
        $ab.FlatStyle = "Flat"
        if ($ad.Accent) {
            $ab.BackColor = $uiAccent
            $ab.ForeColor = [System.Drawing.Color]::White
        }
        else {
            $ab.BackColor = $uiBtn
            $ab.ForeColor = $uiText
        }
        $ab.FlatAppearance.BorderSize = 0
        $actName = $ad.A
        $actExtra = $ad.E
        $ab.Add_Click({
            $extra = $actExtra
            if ($extra -eq "VERSION") {
                $extra = "-Version $(Get-ProjectVersion)"
            }
            Invoke-MaintainGuiAction -ActionName $actName -ExtraArgs $extra
        }.GetNewClosure())
        $actionBar.Controls.Add($ab)
        $actionX += 140
    }

    $actionBar.Add_Resize({
        $wrapX = 118
        $wrapY = 10
        $maxW = [Math]::Max(200, $actionBar.ClientSize.Width - 16)
        foreach ($child in $actionBar.Controls) {
            if ($child -eq $actionHint) { continue }
            if (($wrapX + $child.Width) -gt $maxW) {
                $wrapX = 118
                $wrapY += 36
            }
            $child.Location = New-Object System.Drawing.Point -ArgumentList $wrapX, $wrapY
            $wrapX += $child.Width + 8
        }
        $needed = $wrapY + 40
        if ($actionBar.Height -lt $needed) {
            $actionBar.Height = $needed
        }
    })

    $logLabel = New-Object System.Windows.Forms.Label
    $logLabel.Text = "Ausgabe (letzte Aktion):"
    $logLabel.Dock = "Bottom"
    $logLabel.Height = 22
    $logLabel.Padding = New-Object System.Windows.Forms.Padding(12, 4, 0, 0)
    $logLabel.ForeColor = $uiMuted

    $log = New-Object System.Windows.Forms.TextBox
    $log.Multiline = $true
    $log.ReadOnly = $true
    $log.ScrollBars = "Vertical"
    $log.Dock = "Bottom"
    $log.Height = 140
    $log.BackColor = [System.Drawing.Color]::FromArgb(14, 16, 22)
    $log.ForeColor = [System.Drawing.Color]::FromArgb(180, 220, 180)
    $log.Font = New-Object System.Drawing.Font("Consolas", 9)

    $tabs = New-Object System.Windows.Forms.TabControl
    $tabs.Dock = "Fill"
    $tabs.Padding = New-Object System.Drawing.Point(16, 10)
    $tabs.Font = New-Object System.Drawing.Font("Segoe UI", 9.5, [System.Drawing.FontStyle]::Bold)
    $tabs.SizeMode = "Fixed"
    $tabs.ItemSize = New-Object System.Drawing.Size -ArgumentList 128, 30
    $tabs.BackColor = [System.Drawing.Color]::FromArgb(34, 37, 48)

    # Dock layout: add Fill/Bottom first, then Top panels (last Top = outermost / top of window).
    $form.Controls.Add($tabs)
    $form.Controls.Add($log)
    $form.Controls.Add($logLabel)
    $form.Controls.Add($actionBar)
    $form.Controls.Add($topPanel)

    $script:MaintainGui = @{
        Root = $Root
        Form = $form
        Status = $status
        Log = $log
        Tabs = $tabs
        ChecklistListBox = $null
        VersionBox = $null
    }

    $versionBox = $null

    function New-GuiScrollTab([string]$Title) {
        $tab = New-Object System.Windows.Forms.TabPage
        $tab.Text = $Title
        $tab.BackColor = $uiBg
        $tab.Padding = New-Object System.Windows.Forms.Padding(10, 12, 10, 10)

        $scroll = New-Object System.Windows.Forms.Panel
        $scroll.AutoScroll = $true
        $scroll.Dock = "Fill"
        $scroll.BackColor = $uiBg
        $scroll.Padding = New-Object System.Windows.Forms.Padding(4, 8, 4, 8)
        $tab.Controls.Add($scroll)
        $tabs.TabPages.Add($tab) | Out-Null

        return @{
            Tab = $tab
            Panel = $scroll
            Y = 12
        }
    }

    function Add-GuiStep {
        param(
            [hashtable]$Ctx,
            [string]$Step,
            [string]$Title,
            [string]$Description,
            [string[]]$Buttons
        )

        $y = $Ctx.Y
        $panel = $Ctx.Panel
        $w = [int][Math]::Max(400, ([int]$panel.ClientSize.Width - 40))

        $box = New-Object System.Windows.Forms.Panel
        $box.Location = New-Object System.Drawing.Point -ArgumentList 8, $y
        $box.Size = New-Object System.Drawing.Size -ArgumentList $w, 1
        $box.Anchor = "Top, Left, Right"
        $box.BackColor = $uiPanel
        $box.Padding = New-Object System.Windows.Forms.Padding(12, 10, 12, 10)
        $panel.Controls.Add($box)

        $stepLbl = New-Object System.Windows.Forms.Label
        $stepLbl.Text = "Schritt $Step"
        $stepLbl.Location = New-Object System.Drawing.Point -ArgumentList 12, 10
        $stepLbl.AutoSize = $true
        $stepLbl.Font = New-Object System.Drawing.Font("Segoe UI", 9.5, [System.Drawing.FontStyle]::Bold)
        $stepLbl.ForeColor = $uiAccent
        $box.Controls.Add($stepLbl)

        $titleLbl = New-Object System.Windows.Forms.Label
        $titleLbl.Text = $Title
        $titleLbl.Location = New-Object System.Drawing.Point -ArgumentList 90, 10
        $titleLbl.Size = New-Object System.Drawing.Size -ArgumentList ($w - 110), 20
        $titleLbl.Font = New-Object System.Drawing.Font("Segoe UI", 10, [System.Drawing.FontStyle]::Bold)
        $titleLbl.ForeColor = $uiText
        $box.Controls.Add($titleLbl)

        $descLbl = New-Object System.Windows.Forms.Label
        $descLbl.Text = $Description
        $descLbl.Location = New-Object System.Drawing.Point -ArgumentList 12, 36
        $descLbl.Size = New-Object System.Drawing.Size -ArgumentList ($w - 24), 48
        $descLbl.ForeColor = $uiMuted
        $box.Controls.Add($descLbl)

        $btnY = 92
        $btnX = 12
        foreach ($btn in $Buttons) {
            $parts = $btn -split '\|', 3
            $caption = $parts[0]
            $action = $parts[1]
            $extra = if ($parts.Count -ge 3) { $parts[2] } else { "" }
            $b = New-Object System.Windows.Forms.Button
            $b.Text = $caption
            $b.Location = New-Object System.Drawing.Point -ArgumentList $btnX, $btnY
            $b.Size = New-Object System.Drawing.Size -ArgumentList 200, 34
            $b.FlatStyle = "Flat"
            $b.BackColor = $uiBtn
            $b.ForeColor = $uiText
            $b.FlatAppearance.BorderSize = 0
            $cap = $action
            $ext = $extra
            $b.Add_Click({ Invoke-MaintainGuiAction -ActionName $cap -ExtraArgs $ext }.GetNewClosure())
            $box.Controls.Add($b)
            $btnX += 210
        }

        $box.Height = 140
        $Ctx.Y = $y + 148
    }

    function Add-GuiIntro {
        param(
            [hashtable]$Ctx,
            [string]$Text
        )
        $lbl = New-Object System.Windows.Forms.Label
        $lbl.Text = $Text
        $lbl.Location = New-Object System.Drawing.Point -ArgumentList 12, ([int]$Ctx.Y)
        $lbl.Size = New-Object System.Drawing.Size -ArgumentList 920, 50
        $lbl.ForeColor = $uiMuted
        $lbl.AutoSize = $false
        $Ctx.Panel.Controls.Add($lbl)
        $Ctx.Y = [int]$Ctx.Y + 58
    }

    # --- Tab: Uebersicht ---
    $ov = New-GuiScrollTab "Uebersicht"
    Add-GuiIntro $ov @"
Willkommen! Dieses Tool fuehrt dich durch alles, was du fuer Wartung und Releases brauchst.
Unten siehst du den Projekt-Status. OK = bereit. OFFEN = noch erledigen.
"@

    $chkPanel = New-Object System.Windows.Forms.Panel
    $chkPanel.Location = New-Object System.Drawing.Point -ArgumentList 8, ([int]$ov.Y)
    $chkPanel.Size = New-Object System.Drawing.Size -ArgumentList 900, 158
    $chkPanel.BackColor = $uiPanel
    $chkPanel.BorderStyle = "FixedSingle"
    $ov.Panel.Controls.Add($chkPanel)

    $chkTitle = New-Object System.Windows.Forms.Label
    $chkTitle.Text = "Checkliste erstes Release (MordWraith/Gamehelper-Core)"
    $chkTitle.Location = New-Object System.Drawing.Point -ArgumentList 10, 8
    $chkTitle.AutoSize = $true
    $chkTitle.ForeColor = $uiWarn
    $chkTitle.Font = New-Object System.Drawing.Font("Segoe UI", 9.5, [System.Drawing.FontStyle]::Bold)
    $chkPanel.Controls.Add($chkTitle)

    $checklistListBox = New-Object System.Windows.Forms.ListBox
    $checklistListBox.Location = New-Object System.Drawing.Point -ArgumentList 10, 30
    $checklistListBox.Size = New-Object System.Drawing.Size -ArgumentList 878, 118
    $checklistListBox.Anchor = "Top, Left, Right, Bottom"
    $checklistListBox.BackColor = [System.Drawing.Color]::FromArgb(18, 20, 26)
    $checklistListBox.ForeColor = $uiText
    $checklistListBox.BorderStyle = "None"
    $checklistListBox.Font = New-Object System.Drawing.Font("Consolas", 9.5)
    $checklistListBox.IntegralHeight = $false
    $chkPanel.Controls.Add($checklistListBox)
    $script:MaintainGui.ChecklistListBox = $checklistListBox
    Update-MaintainGuiChecklist
    $ov.Y = [int]$ov.Y + 170

    $tabHint = New-Object System.Windows.Forms.Label
    $tabHint.Text = "Alle Schritte im Detail: Tabs oben -> Taeglich | Gordin & Plugins | Erstes Release | Release | Hilfe"
    $tabHint.Location = New-Object System.Drawing.Point -ArgumentList 12, ([int]$ov.Y)
    $tabHint.Size = New-Object System.Drawing.Size -ArgumentList 920, 36
    $tabHint.ForeColor = $uiAccent
    $tabHint.Font = New-Object System.Drawing.Font("Segoe UI", 9, [System.Drawing.FontStyle]::Bold)
    $ov.Panel.Controls.Add($tabHint)
    $ov.Y = [int]$ov.Y + 40

    $quickLbl = New-Object System.Windows.Forms.Label
    $quickLbl.Text = "Schnellzugriff:"
    $quickLbl.Location = New-Object System.Drawing.Point -ArgumentList 12, ([int]$ov.Y)
    $quickLbl.AutoSize = $true
    $quickLbl.ForeColor = $uiText
    $ov.Panel.Controls.Add($quickLbl)
    $ov.Y = [int]$ov.Y + 32

    foreach ($qb in @(
        @{ T = "Status aktualisieren"; A = "Status" },
        @{ T = "Build (Release)"; A = "Build" },
        @{ T = "Source pushen"; A = "PushSource" },
        @{ T = "Release publishen"; A = "Publish" },
        @{ T = "Projektordner oeffnen"; A = "OpenProjectFolder" },
        @{ T = "publish\ oeffnen"; A = "OpenPublishFolder" },
        @{ T = "GameHelper starten"; A = "Run" }
    )) {
        $btn = New-Object System.Windows.Forms.Button
        $btn.Text = $qb.T
        $btn.Location = New-Object System.Drawing.Point -ArgumentList 12, ([int]$ov.Y)
        $btn.Size = New-Object System.Drawing.Size -ArgumentList 200, 32
        $btn.FlatStyle = "Flat"
        if ($qb.A -in @("Publish", "PushSource", "Build")) {
            $btn.BackColor = $uiAccent
            $btn.ForeColor = [System.Drawing.Color]::White
        }
        else {
            $btn.BackColor = $uiBtn
            $btn.ForeColor = $uiText
        }
        $btn.FlatAppearance.BorderSize = 0
        $act = $qb.A
        $btn.Add_Click({ Invoke-MaintainGuiAction -ActionName $act }.GetNewClosure())
        $ov.Panel.Controls.Add($btn)
        $ov.Y = [int]$ov.Y + 42
    }

    $ov.Panel.AutoScrollMinSize = New-Object System.Drawing.Size -ArgumentList 0, ([int]$ov.Y + 16)
    $daily = New-GuiScrollTab "Taeglich"
    Add-GuiIntro $daily "Nach jeder Aenderung am Code (Core, Launcher, Plugins) oder nach Plugin-Updates von GitHub."
    Add-GuiStep $daily "1" "Plugin-Repos aktualisieren (git pull)" `
        "Holt die neuesten Versionen deiner MordWraith-Forks und yokkenUA-Plugins. Mach das, wenn du auf GitHub etwas gepusht hast oder Upstream-Updates willst." `
        @("Alle Plugins pullen|SyncPlugins")
    Add-GuiStep $daily "2" "Release-Build erstellen" `
        "Kompiliert die komplette Solution und kopiert alles nach publish\. Das ist dein testbarer Stand." `
        @("Build (Release)|Build")
    Add-GuiStep $daily "3" "Lokal testen" `
        "Startet publish\GameHelper.exe - Launcher, Update-Dialog und Overlay im Spiel pruefen." `
        @("GameHelper starten|Run")
    Add-GuiStep $daily "optional" "Debug-Build" `
        "Nur wenn du gezielt mit Debug-Konfiguration testen willst (langsamer, mehr Logs)." `
        @("Build (Debug)|Build|-Configuration Debug")

    # --- Tab: Gordin & Plugins ---
    $sync = New-GuiScrollTab "Gordin & Plugins"
    Add-GuiIntro $sync "Gordin (GameHelper2) liefert Core und 5 Plugins. Deine Forks (MordWraith, yokkenUA) bleiben separate Git-Repos."
    Add-GuiStep $sync "1" "Gordin Core holen" `
        "Ueberschreibt GameHelper\ und GameOffsets\ mit dem aktuellen Stand von github.com/Gordin/GameHelper2. Danach immer neu bauen und testen!" `
        @("Gordin Core sync|SyncGordinCore")
    Add-GuiStep $sync "2" "Gordin Plugins holen" `
        "AutoHotKeyTrigger, Radar, HealthBars, PreloadAlert, LootValue von Gordin. Achtung: ueberschreibt lokale Aenderungen in diesen Ordnern." `
        @("Gordin Plugins sync|SyncGordinPlugins")
    Add-GuiStep $sync "3" "Alles von Gordin" `
        "Core + alle 5 Gordin-Plugins auf einmal. Nur wenn du bewusst alles aktualisieren willst." `
        @("Gordin komplett|SyncGordinAll")
    Add-GuiStep $sync "4" "Nur MordWraith-Plugins pullen" `
        "AuraTracker, Autopot, PlayerBuffBar, RitualHelper, SimpleBars, Hiveblood, AmanamuVoidAlert." `
        @("MordWraith pull|SyncPluginsMordWraith")
    Add-GuiStep $sync "5" "Nur yokkenUA-Plugins pullen" `
        "Atlas, LootTracker, RunecraftHelper, SekhemaHelper." `
        @("yokkenUA pull|SyncPluginsUpstream")
    Add-GuiStep $sync "6" "Eigene Plugin-Aenderungen pushen" `
        "Wenn du in Plugins\<Name>\ etwas geaendert hast: committet und pusht nur Repos mit lokalen Aenderungen zu GitHub." `
        @("MordWraith Plugins pushen|PushMordWraithPlugins")

    # --- Tab: Erstes Release ---
    $first = New-GuiScrollTab "Erstes Release"
    Add-GuiIntro $first "Einmalige Einrichtung fuer MordWraith/Gamehelper-Core. Reihenfolge einhalten! Nicht mit Stable (MordWraith/Gamehelper) verwechseln."
    Add-GuiStep $first "1" "github.config.json anlegen" `
        "Legt die Datei an mit Ziel-Repo MordWraith/Gamehelper-Core. Ohne das landen Releases am falschen Ort." `
        @("Config anlegen|SetupGithubConfig")
    Add-GuiStep $first "2" "Signatur-Schluessel erzeugen" `
        "Neuer Schluessel NUR fuer Core - niemals den Stable-Schluessel wiederverwenden! Danach neu bauen." `
        @("Schluessel erzeugen|SigningKey")
    Add-GuiStep $first "3" "Bei GitHub anmelden" `
        "Oeffnet gh auth login in einem neuen Fenster. Brauchst du fuer Publish und Source-Push." `
        @("gh auth login|GhAuthLogin")
    Add-GuiStep $first "4" "Core-Quellcode auf GitHub" `
        "Erster Push des Core-Trees (ohne publish\, Secrets, Plugin-.git). Repo muss existieren: gh repo create MordWraith/Gamehelper-Core" `
        @("Source pushen|PushSource")
    Add-GuiStep $first "5" "Lokal bauen & testen" `
        "Sicherstellen, dass publish\ funktioniert, bevor du oeffentlich releasest." `
        @("Build|Build", "Testen|Run")

    # --- Tab: Version veroeffentlichen ---
    $rel = New-GuiScrollTab "Release"
    Add-GuiIntro $rel "Fuer jede neue Version (z.B. 1.0.1): Version in GameHelper.csproj erhoehen, testen, Source pushen, Release publishen, verifizieren."

    $verPanel = New-Object System.Windows.Forms.Panel
    $verPanel.Location = New-Object System.Drawing.Point(8, $rel.Y)
    $verPanel.Size = New-Object System.Drawing.Size -ArgumentList 900, 40
    $verPanel.BackColor = $uiPanel
    $rel.Panel.Controls.Add($verPanel)
    $verLbl = New-Object System.Windows.Forms.Label
    $verLbl.Text = "Release-Version (leer = aus csproj):"
    $verLbl.Location = New-Object System.Drawing.Point(12, 10)
    $verLbl.AutoSize = $true
    $verPanel.Controls.Add($verLbl)
    $versionBox = New-Object System.Windows.Forms.TextBox
    $versionBox.Location = New-Object System.Drawing.Point(240, 8)
    $versionBox.Width = 120
    $versionBox.Text = Get-ProjectVersion
    $verPanel.Controls.Add($versionBox)
    $script:MaintainGui.VersionBox = $versionBox
    $rel.Y += 52

    Add-GuiStep $rel "1" "Version setzen" `
        "Version in GameHelper\GameHelper.csproj anpassen ODER oben eintragen. Muss zu Tag vX.Y.Z passen." `
        @("Aktuelle Version lesen|Status")
    Add-GuiStep $rel "2" "Build & Test" `
        "Release-Build und kurzer Test im Spiel." `
        @("Build|Build", "Testen|Run")
    Add-GuiStep $rel "3" "Source pushen" `
        "Aktuellen Core-Stand nach Gamehelper-Core pushen." `
        @("Source pushen|PushSource")

    # Custom publish/verify buttons with version
    $yRel = $rel.Y
    $pubPanel = New-Object System.Windows.Forms.Panel
    $pubPanel.Location = New-Object System.Drawing.Point(8, $yRel)
    $pubPanel.Size = New-Object System.Drawing.Size -ArgumentList 900, 140
    $pubPanel.BackColor = $uiPanel
    $rel.Panel.Controls.Add($pubPanel)

    $s4 = New-Object System.Windows.Forms.Label
    $s4.Text = "Schritt 4"
    $s4.Location = New-Object System.Drawing.Point(12, 10)
    $s4.ForeColor = $uiAccent
    $s4.Font = New-Object System.Drawing.Font("Segoe UI", 9.5, [System.Drawing.FontStyle]::Bold)
    $pubPanel.Controls.Add($s4)

    $t4 = New-Object System.Windows.Forms.Label
    $t4.Text = "GitHub Release publishen"
    $t4.Location = New-Object System.Drawing.Point(90, 10)
    $t4.Font = New-Object System.Drawing.Font("Segoe UI", 10, [System.Drawing.FontStyle]::Bold)
    $pubPanel.Controls.Add($t4)

    $d4 = New-Object System.Windows.Forms.Label
    $d4.Text = "Baut (falls noetig), erstellt signiertes ZIP + manifest.json auf GitHub Releases. Nutzer bekommen Update ueber den Launcher."
    $d4.Location = New-Object System.Drawing.Point(12, 36)
    $d4.Size = New-Object System.Drawing.Size -ArgumentList 860, 40
    $d4.ForeColor = $uiMuted
    $pubPanel.Controls.Add($d4)

    $pubBtn = New-Object System.Windows.Forms.Button
    $pubBtn.Text = "Release publishen"
    $pubBtn.Location = New-Object System.Drawing.Point(12, 88)
    $pubBtn.Size = New-Object System.Drawing.Size -ArgumentList 200, 34
    $pubBtn.FlatStyle = "Flat"
    $pubBtn.BackColor = $uiAccent
    $pubBtn.ForeColor = [System.Drawing.Color]::White
    $pubBtn.FlatAppearance.BorderSize = 0
    $pubBtn.Add_Click({
        $v = $versionBox.Text.Trim()
        $extra = if ($v) { " -Version $v" } else { "" }
        Invoke-MaintainGuiAction -ActionName "Publish" -ExtraArgs $extra
    })
    $pubPanel.Controls.Add($pubBtn)

    $verBtn = New-Object System.Windows.Forms.Button
    $verBtn.Text = "Release pruefen"
    $verBtn.Location = New-Object System.Drawing.Point(222, 88)
    $verBtn.Size = New-Object System.Drawing.Size -ArgumentList 200, 34
    $verBtn.FlatStyle = "Flat"
    $verBtn.BackColor = $uiBtn
    $verBtn.ForeColor = $uiText
    $verBtn.FlatAppearance.BorderSize = 0
    $verBtn.Add_Click({
        $v = $versionBox.Text.Trim()
        if (-not $v) { $v = Get-ProjectVersion }
        Invoke-MaintainGuiAction -ActionName "VerifyPublish" -ExtraArgs "-Version $v"
    })
    $pubPanel.Controls.Add($verBtn)

    $rel.Y = $yRel + 152
    Add-GuiStep $rel "5" "Downloader bauen (optional)" `
        "Erstellt GameHelperDownloader.exe fuer Erstinstallation per ZIP. Nur noetig wenn du den Downloader neu ausliefern willst." `
        @("Downloader bauen|BuildDownloader")

    # --- Tab: Hilfe ---
    $help = New-GuiScrollTab "Hilfe"
    $helpText = @"
WANN WAS TUN - Kurzreferenz

TAEGLICH (nach Code-Aenderungen):
  1. Plugins pullen (falls noetig)
  2. Build (Release)
  3. publish\GameHelper.exe testen

NACH GORDIN-UPDATE:
  Gordin Core/Plugins sync -> Build -> alle Fork-Plugins testen
  bootstrap.ps1 NUR bei komplettem Reset!

EIGENES PLUGIN GEANDERT:
  Aenderung in Plugins\<Name>\ -> MordWraith Plugins pushen -> Build

ERSTES RELEASE:
  github.config -> Signatur-Schluessel -> gh login -> Source push -> Build/Test -> Publish -> Verify

NEUE VERSION (z.B. 1.0.1):
  Version in csproj -> Build/Test -> Source push -> Publish -> Verify

WICHTIG - NICHT VERWECHSELN:
  * Gamehelper-Core = diese Distribution (signierte ZIP-Updates)
  * MordWraith/Gamehelper = altes Stable-Repo
  * Plugin-Repos = eigene GitHub-Repos (separat vom Core)
  * App-Sprache = Englisch (nur dieses Wartungs-Tool ist Deutsch)

KONSOLEN-MENUE: maintain.cmd -Console
"@
    $helpBox = New-Object System.Windows.Forms.TextBox
    $helpBox.Multiline = $true
    $helpBox.ReadOnly = $true
    $helpBox.ScrollBars = "Vertical"
    $helpBox.Text = $helpText
    $helpBox.BackColor = [System.Drawing.Color]::FromArgb(18, 20, 26)
    $helpBox.ForeColor = $uiText
    $helpBox.BorderStyle = "None"
    $helpBox.Font = New-Object System.Drawing.Font("Consolas", 9.5)
    $helpBox.Dock = "Fill"
    $help.Panel.Controls.Add($helpBox)

    Update-MaintainGuiStatus
    Update-MaintainGuiChecklist
    [void]$form.ShowDialog()
}

try {
    if ($Gui -or (-not $Console -and $Action -eq "Menu")) {
        Show-Gui
    }
    elseif ($Action -eq "Menu") {
        Show-Menu
    }
    else {
        Invoke-MaintainAction $Action
    }
}
catch {
    Write-Host ""
    Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
