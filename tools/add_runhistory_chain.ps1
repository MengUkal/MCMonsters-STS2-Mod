$ErrorActionPreference = 'Stop'
$NL = [char]10
$f = 'D:\塔2MOD\MCMonsters\tools\ArtGen\ModPckAssembler.cs'
$c = [System.IO.File]::ReadAllText($f)
if ($c.Contains('run_history')) { Write-Host '已存在'; exit 0 }
# 在力量图标块之后追加 run_history 图标链生成
$anchor = '        // ---- 自定义力量图标：images/powers/*.png 的完整 import 链 ----'
$block = @'
        // ---- 跑图记录图标：images/ui/run_history/*.png 的完整 import 链 ----
        // 游戏在进入新幕/展示跑图记录时会加载 res://images/ui/run_history/<遭遇ID>.png(+_outline)，
        // 缺失会导致同步回退加载失败循环（严重时会卡死游戏）
        string runHistoryDir = Path.Combine(contentDir, "images/ui/run_history");
        if (Directory.Exists(runHistoryDir))
        {
            foreach (var rhFile in Directory.EnumerateFiles(runHistoryDir, "*.png"))
            {
                string rhName = Path.GetFileNameWithoutExtension(rhFile);
                string rhHash = Md5("images/ui/run_history/" + rhName + ".png:mc");
                byte[] rhCtex = CtexBuilder.Build(rhFile);
                entries.Add(new($"res://.godot/imported/{rhName}.png-{rhHash}.ctex", rhCtex));
                entries.Add(new($"res://images/ui/run_history/{rhName}.png.import",
                    GodotImporterStub.TextureStub($"res://images/ui/run_history/{rhName}.png", rhName + ".png", rhHash, out _)));
                Console.WriteLine($"  缓存: images/ui/run_history/{rhName}.png → ctex ({rhCtex.Length} B)");
            }
        }

'@
$c = $c.Replace($anchor, $block + $anchor)
[System.IO.File]::WriteAllText($f, $c)
Write-Host 'run_history import链生成已加入'
$root = 'D:\塔2MOD\MCMonsters'
& "C:\Program Files\dotnet\dotnet.exe" build "$root\tools\ArtGen\ArtGen.csproj" -c Release --nologo -v minimal 2>&1 | Select-String -Pattern 'error CS' | Select-Object -First 3 | ForEach-Object { $_.Line }
Write-Host ("build: " + $LASTEXITCODE)