$ErrorActionPreference = 'Continue'
$env:PATH = "C:\Program Files\Git\cmd;" + $env:PATH
$git = 'C:\Program Files\Git\cmd\git.exe'
$gh = 'C:\Program Files\GitHub CLI\gh.exe'
Set-Location 'D:\塔2MOD\MCMonsters'

# ===== 1. 更新 README =====
$readme = 'D:\塔2MOD\MCMonsters\README.md'
$c = [System.IO.File]::ReadAllText($readme)
if (-not $c.Contains('1.6.1')) {
    $note = @'

**1.6.1** — 修复**严重 BUG：进入第二幕时游戏卡死**。根因：游戏在幕切换/跑图记录界面会加载 `res://images/ui/run_history/<遭遇ID>.png`（+`_outline`），本模组此前没有提供这 34 个遭遇的图标，缺失导致 `NAssetLoader` 同步回退加载循环、严重时卡死。现已用 `tools/make_runhistory_icons.ps1` 为全部 34 个遭遇生成图标 + 白色轮廓（128×128），组装器补上 `.png.import → .ctex` 导入链。本机 v0.107.1 实测进入二幕/凋灵场景 0 报错。
'@
    $c = $c.Replace('**1.6.0** —', $note + "`r`n" + '**1.6.0** —')
    $c = $c.Replace('- 三幕 BOSS 的跑图记录图标（run history）显示为空白' + "`r`n", '')
    [System.IO.File]::WriteAllText($readme, $c)
    Write-Host 'README已更新'
} else { Write-Host 'README已有1.6.1说明' }

# ===== 2. 提交推送 =====
& $git add -A 2>&1 | Out-Null
& $git commit -m "v1.6.1: fix game lock on act 2 entry - generate run_history icons for all 34 encounters" 2>&1 | Select-Object -Last 1
& $git push 2>&1 | Select-Object -Last 2
Write-Host ('推送退出码: ' + $LASTEXITCODE)
Write-Host ''
Write-Host '=== 状态 ==='
& $git log --oneline -2
& $git status -sb | Select-Object -First 1
Write-Host '=== 贡献者 ==='
& $gh api "repos/MengUkal/MCMonsters-STS2-Mod/contributors" --jq '.[].login' 2>&1

