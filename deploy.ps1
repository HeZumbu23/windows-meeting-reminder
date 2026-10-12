<#
.SYNOPSIS
    Watches the current branch for new commits and redeploys automatically.
.DESCRIPTION
    Deploys immediately once on startup, then watches: every
    -IntervalSeconds (default 60), fetches from origin. Only when the
    remote branch actually has commits the local branch lacks does it pull,
    stop the running instance, rebuild (Release), and relaunch - an
    unchanged remote (or a local branch that is merely ahead) is a silent
    no-op. Press Ctrl+C to stop watching.
.PARAMETER IntervalSeconds
    How often to check for new commits, in seconds. Default: 60.
.PARAMETER Once
    Deploy immediately, once, without entering the watch loop.
#>

param(
    [int]$IntervalSeconds = 60,
    [switch]$Once
)

$ErrorActionPreference = "Stop"

$repoRoot = $PSScriptRoot
Set-Location $repoRoot

function Write-Step([string]$Text) {
    Write-Host ""
    Write-Host "==> $Text" -ForegroundColor Cyan
}

function Write-Command([string]$Text) {
    Write-Host "  `$ $Text" -ForegroundColor DarkGray
}

function Invoke-Deploy {
    # Pull first: if it fails (e.g. local edits to meetings.json collide with
    # incoming changes), the running instance is left untouched instead of
    # being stopped and never restarted.
    Write-Step "Pulling latest changes"
    Write-Command "git pull"
    git pull
    if ($LASTEXITCODE -ne 0) {
        throw "git pull failed (exit code $LASTEXITCODE)."
    }

    Write-Step "Stopping running instance (if any)"
    $proc = Get-Process -Name "MeetingReminder" -ErrorAction SilentlyContinue
    if ($proc) {
        $proc | Stop-Process -Force
        $proc | Wait-Process -Timeout 5 -ErrorAction SilentlyContinue
        # Brief buffer so Windows actually releases the .exe file before
        # dotnet publish tries to overwrite it.
        Start-Sleep -Milliseconds 500
        Write-Host "Instance stopped."
    }
    else {
        Write-Host "No running instance found."
    }

    Write-Step "Building Release"
    Write-Command "dotnet publish MeetingReminder -c Release"
    dotnet publish MeetingReminder -c Release
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed (exit code $LASTEXITCODE)."
    }

    $exePath = Join-Path $repoRoot "MeetingReminder\bin\Release\net8.0-windows\win-x64\publish\MeetingReminder.exe"
    if (-not (Test-Path $exePath)) {
        throw "Expected .exe was not found: $exePath"
    }

    Write-Step "Starting MeetingReminder"
    Write-Command "Start-Process -FilePath `"$exePath`""
    Start-Process -FilePath $exePath

    Write-Host ""
    Write-Host "Deployed." -ForegroundColor Green
}

if ($Once) {
    Invoke-Deploy
    return
}

# Auch beim Start einmal bauen/starten, nicht erst beim ersten erkannten
# Commit - sonst läuft nach dem Start erstmal keine (aktuelle) Version.
Invoke-Deploy

Write-Command "git rev-parse --abbrev-ref HEAD"
$branch = (git rev-parse --abbrev-ref HEAD).Trim()
Write-Host ""
Write-Host "Watching for new commits every $IntervalSeconds s. Press Ctrl+C to stop." -ForegroundColor Yellow

while ($true) {
    Write-Command "git fetch origin $branch --quiet"
    git fetch origin $branch --quiet
    if ($LASTEXITCODE -ne 0) {
        Write-Host "git fetch failed (exit code $LASTEXITCODE) - will retry next interval." -ForegroundColor Red
    }
    else {
        # Count commits the remote has that HEAD lacks. Comparing the two
        # hashes for inequality instead would also fire when the local branch
        # is merely *ahead* (unpushed commit) - which never resolves by
        # pulling and would redeploy (= restart the app) every interval.
        Write-Command "git rev-list --count HEAD..origin/$branch"
        $behind = [int](git rev-list --count "HEAD..origin/$branch")

        if ($behind -gt 0) {
            $localHash = (git rev-parse HEAD).Trim()
            $remoteHash = (git rev-parse "origin/$branch").Trim()
            Write-Host ""
            Write-Host "New commits detected ($behind): $($localHash.Substring(0,7)) -> $($remoteHash.Substring(0,7))" -ForegroundColor Yellow
            try {
                Invoke-Deploy
            }
            catch {
                Write-Host "Deploy failed: $_" -ForegroundColor Red
            }
        }
    }

    Start-Sleep -Seconds $IntervalSeconds
}
