$ErrorActionPreference = 'Stop'
$prog = 'D:\塔2MOD\MCMonsters\tools\ArtGen\Program.cs'
$c = [System.IO.File]::ReadAllText($prog)
if ($c.Contains('图集页名全局改写')) { Write-Host 'already'; exit 0 }
$marker = '// ============================ 4. 组装 mod pck ============================'
$block = @'
// ===== 图集页名全局改写：相对路径 -> res:// 绝对路径（免去游戏根目录裸图）=====
try
{
    var monstersRoot2 = @"D:\塔2MOD\MCMonsters\pck_content\animations\monsters";
    int rewrote = 0;
    foreach (var dir2 in Directory.GetDirectories(monstersRoot2))
    {
        string slug2 = Path.GetFileName(dir2);
        foreach (var atlasFile2 in Directory.GetFiles(dir2, "*.atlas"))
        {
            var lines2 = File.ReadAllLines(atlasFile2);
            if (lines2.Length > 0)
            {
                string page2 = lines2[0].Trim();
                if (!page2.StartsWith("res://") && !page2.StartsWith("/"))
                {
                    lines2[0] = "res://animations/monsters/" + slug2 + "/" + page2;
                    File.WriteAllLines(atlasFile2, lines2);
                    rewrote++;
                }
            }
        }
    }
    Console.WriteLine($"OK 图集页名改写: {rewrote} 个 -> res:// 路径");
}
catch (Exception ex2) { Console.WriteLine("图集页名改写失败: " + ex2.Message); }

'@
$c = $c.Replace($marker, $block + $marker)
[System.IO.File]::WriteAllText($prog, $c)
Write-Host '图集页名改写已加入生成器'
$bak = 'D:\塔2MOD\MCMonsters\pck_content\animations\monsters\skeleton\skeleton.atlas.bak'
if (Test-Path $bak) { Move-Item $bak 'D:\塔2MOD\MCMonsters\pck_content\animations\monsters\skeleton\skeleton.atlas' -Force }
Write-Host '骷髅atlas已恢复'
$root = 'D:\塔2MOD\MCMonsters'
& "C:\Program Files\dotnet\dotnet.exe" build "$root\tools\ArtGen\ArtGen.csproj" -c Release --nologo -v minimal 2>&1 | Select-String -Pattern 'error CS' | Select-Object -First 3 | ForEach-Object { $_.Line }
Write-Host ("artgen build: " + $LASTEXITCODE)
# 运行完整生成+打包
Get-Process SlayTheSpire2 -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 2
& "C:\Program Files\dotnet\dotnet.exe" run --project "$root\tools\ArtGen\ArtGen.csproj" -c Release --no-build -- packonly 2>&1 | Select-String -Pattern '图集页名|打包完成' | ForEach-Object { $_.Line }
# 验证atlas页名
Write-Host '=== 骷髅atlas前两行 ==='
Get-Content "$root\pck_content\animations\monsters\skeleton\skeleton.atlas" -TotalCount 2
Write-Host '=== 僵尸atlas前两行 ==='
Get-Content "$root\pck_content\animations\monsters\zombie\zombie.atlas" -TotalCount 2