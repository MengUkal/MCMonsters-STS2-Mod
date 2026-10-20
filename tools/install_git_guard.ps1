# Install a repo-level pre-commit hook that guards the git author identity.
# Purpose: block commits made with a wrong user.email (e.g. company account),
#          so contributors are always attributed to MengUkal.
# Usage (from repo root):
#   powershell -ExecutionPolicy Bypass -File tools\install_git_guard.ps1
# Bypass once if really needed: git commit --no-verify
$ErrorActionPreference = 'Stop'

$root = (git rev-parse --show-toplevel 2>$null)
if (-not $root) { throw "Run this script from inside the MCMonsters repository." }
$root = $root.Trim()
$hookDir = Join-Path $root '.git\hooks'
New-Item -ItemType Directory -Force -Path $hookDir | Out-Null

$hookBody = @'
#!/bin/sh
expected="169738389+MengUkal@users.noreply.github.com"
actual=$(git config user.email)
if [ "$actual" != "$expected" ]; then
  echo ""
  echo "[GitGuard] Commit blocked: git user.email = $actual"
  echo "[GitGuard] Expected: MengUkal <$expected>"
  echo "[GitGuard] Fix: git config user.email \"$expected\" && git config user.name \"MengUkal\""
  echo "[GitGuard] (Temporary bypass: git commit --no-verify)"
  echo ""
  exit 1
fi
'@

$hookPath = Join-Path $hookDir 'pre-commit'
[System.IO.File]::WriteAllText($hookPath, ($hookBody -replace "`r`n", "`n"), [System.Text.UTF8Encoding]::new($false))
Write-Host "Hook installed: $hookPath"
Write-Host "Current user.email: $((git config user.email))"
