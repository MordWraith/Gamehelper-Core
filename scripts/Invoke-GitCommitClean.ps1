# Creates a git commit without running commit hooks (avoids injected Co-authored-by trailers).
param(
    [Parameter(Mandatory = $true)]
    [string]$Message,
    [string]$WorkingDirectory = "",
    [string]$AuthorName = "",
    [string]$AuthorEmail = ""
)

$ErrorActionPreference = "Stop"

function Resolve-GitExe {
    $cmd = Get-Command git -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }

    foreach ($path in @(
            "C:\Program Files\Git\cmd\git.exe",
            "C:\Program Files\Git\bin\git.exe",
            "C:\Program Files (x86)\Git\cmd\git.exe"
        )) {
        if (Test-Path $path) { return $path }
    }

    throw "Git nicht gefunden."
}

function Get-DefaultIdentity {
    if (Get-Command gh -ErrorAction SilentlyContinue) {
        $user = gh api user 2>$null | ConvertFrom-Json
        if ($user) {
            $email = $user.email
            if ([string]::IsNullOrWhiteSpace($email)) {
                $email = "{0}+{1}@users.noreply.github.com" -f $user.id, $user.login
            }

            return @{ Name = [string]$user.login; Email = [string]$email }
        }
    }

    return @{ Name = "MordWraith"; Email = "mordwraith@users.noreply.github.com" }
}

$git = Resolve-GitExe
$identity = Get-DefaultIdentity
if (-not [string]::IsNullOrWhiteSpace($AuthorName)) { $identity.Name = $AuthorName }
if (-not [string]::IsNullOrWhiteSpace($AuthorEmail)) { $identity.Email = $AuthorEmail }

$prevLocation = $null
if (-not [string]::IsNullOrWhiteSpace($WorkingDirectory)) {
    if (-not (Test-Path $WorkingDirectory)) {
        throw "Working directory not found: $WorkingDirectory"
    }

    $prevLocation = Get-Location
    Set-Location $WorkingDirectory
}

try {
    $env:GIT_AUTHOR_NAME = $identity.Name
    $env:GIT_AUTHOR_EMAIL = $identity.Email
    $env:GIT_COMMITTER_NAME = $identity.Name
    $env:GIT_COMMITTER_EMAIL = $identity.Email

    $status = & $git status --porcelain
    if (-not $status) {
        Write-Host "Keine Aenderungen zum Committen." -ForegroundColor DarkGray
        return
    }

    $tree = (& $git write-tree).Trim()
    if ([string]::IsNullOrWhiteSpace($tree)) {
        throw "git write-tree lieferte keinen Tree."
    }

    $parent = (& $git rev-parse -q --verify HEAD 2>$null)
    $hasParent = ($LASTEXITCODE -eq 0) -and (-not [string]::IsNullOrWhiteSpace($parent))

    if ($hasParent) {
        $commit = (& $git commit-tree $tree -p $parent.Trim() -m $Message).Trim()
    }
    else {
        $commit = (& $git commit-tree $tree -m $Message).Trim()
    }

    if ([string]::IsNullOrWhiteSpace($commit)) {
        throw "git commit-tree fehlgeschlagen."
    }

    & $git reset --hard $commit | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "git reset --hard fehlgeschlagen (exit $LASTEXITCODE)."
    }

    $body = (& $git log -1 --format="%B").TrimEnd()
    if ($body -match '(?im)^Co-authored-by:') {
        throw "Commit enthaelt unerwarteten Co-authored-by-Trailer."
    }

    Write-Host "Commit: $commit ($($identity.Name))" -ForegroundColor DarkGray
}
finally {
    if ($null -ne $prevLocation) {
        Set-Location $prevLocation
    }
}
