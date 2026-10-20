$ErrorActionPreference = 'Stop'
$NL = [char]10
$p = 'D:\塔2MOD\MCMonsters\src\MCMonsters\Patches\ActEncounterPatches.cs'
$c = [System.IO.File]::ReadAllText($p)

if ($c.Contains('注入后遭遇池')) { Write-Host '已有日志'; exit 0 }

# 在方法开头加"每幕打印一次"的静态记录
$anchor = '    private static void InjectMcEncounters(ActModel __instance, ref IEnumerable<EncounterModel> __result)' + $NL + '    {'
$loggedField = '    private static readonly HashSet<int> _loggedActs = new();' + $NL + $NL
$logBlock = $NL + '        // 诊断：每幕首次生成池时打印注入结果（排查"遇不到怪物"问题用）' + $NL + '        try' + $NL + '        {' + $NL + '            if (_loggedActs.Add(__instance.Index))' + $NL + '            {' + $NL + '                var list = System.Linq.Enumerable.ToList(__result);' + $NL + '                MegaCrit.Sts2.Core.Logging.Log.Info("[MCMonsters] Act" + __instance.Index + " pools injected, total=" + (list.Count + 0) + ", MC=" + string.Join("|", System.Linq.Enumerable.Where(list, e => e.Id.Entry.StartsWith("ZOMBIE") || e.Id.Entry.StartsWith("SKELETON") || e.Id.Entry.StartsWith("SPIDER") || e.Id.Entry.StartsWith("CREEPER") || e.Id.Entry.StartsWith("CHICKEN") || e.Id.Entry.StartsWith("WARDEN") || e.Id.Entry.StartsWith("BLAZE") || e.Id.Entry.StartsWith("WITHER") || e.Id.Entry.StartsWith("PIGLIN") || e.Id.Entry.StartsWith("GHAST") || e.Id.Entry.StartsWith("ENDERMAN") || e.Id.Entry.StartsWith("SHULKER") || e.Id.Entry.StartsWith("WITCH") || e.Id.Entry.StartsWith("PILLAGER") || e.Id.Entry.StartsWith("GIANT") || e.Id.Entry.StartsWith("RAID"))));' + $NL + '            }' + $NL + '        }' + $NL + '        catch { }'
$c = $c.Replace($anchor, $loggedField + $anchor + $logBlock)
# 需要在类开头加静态字段（_loggedActs已在方法前插入，但它在方法外？——刚才插入位置是方法声明前，即类的成员区 ✓）
[System.IO.File]::WriteAllText($p, $c)
Write-Host '诊断日志已加（每幕一次）'
