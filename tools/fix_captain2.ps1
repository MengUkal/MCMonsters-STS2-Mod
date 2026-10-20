$ErrorActionPreference = 'Stop'
$f = 'D:\塔2MOD\MCMonsters\tools\ArtGen\Program.cs'
$c = [System.IO.File]::ReadAllText($f)
$c = $c.Replace('"pillager_captain"', '"raid_captain"')
$c = $c.Replace('pillager_captain.tscn', 'raid_captain.tscn')
[System.IO.File]::WriteAllText($f, $c)
Write-Host 'ArtGen slug changed to raid_captain'
$dir = 'D:\塔2MOD\MCMonsters\pck_content\animations\monsters\raid_captain'
New-Item -ItemType Directory -Path $dir -Force | Out-Null
$tres = @'
[gd_resource type="SpineSkeletonDataResource" load_steps=3 format=3]

[ext_resource type="SpineAtlasResource" path="res://animations/monsters/pillager/pillager.atlas" id="1_atlas"]
[ext_resource type="SpineSkeletonFileResource" path="res://animations/monsters/pillager/pillager.json" id="2_skel"]

[resource]
atlas_res = ExtResource("1_atlas")
skeleton_file_res = ExtResource("2_skel")
default_mix = 0.02
'@
Set-Content (Join-Path $dir 'raid_captain_skel_data.tres') $tres -Encoding UTF8
Remove-Item 'D:\塔2MOD\MCMonsters\pck_content\animations\monsters\pillager_captain' -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item 'D:\塔2MOD\MCMonsters\pck_content\scenes\creature_visuals\pillager_captain.tscn' -Force -ErrorAction SilentlyContinue
Write-Host 'raid_captain skel_data created; old files cleaned'
