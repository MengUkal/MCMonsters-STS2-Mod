$ErrorActionPreference = 'Stop'
$NL = [char]10
$f = 'D:\塔2MOD\MCMonsters\tools\ArtGen\Program.cs'
$c = [System.IO.File]::ReadAllText($f)
$old = '                    ["full_flash"] = new JsonObject' + $NL + '                    {' + $NL + '                        ["type"] = "region", ["x"] = 0, ["y"] = 250,' + $NL + '                        ["width"] = 300, ["height"] = 500' + $NL + '                    }' + $NL + '                }'
$new = '                    ["full_flash"] = new JsonObject' + $NL + '                    {' + $NL + '                        ["type"] = "region", ["x"] = 0, ["y"] = 250,' + $NL + '                        ["width"] = 300, ["height"] = 500' + $NL + '                    }' + $NL + '                },' + $NL + '                ["shell"] = new JsonObject' + $NL + '                {' + $NL + '                    ["full_closed"] = new JsonObject' + $NL + '                    {' + $NL + '                        ["type"] = "region", ["x"] = 0, ["y"] = 250,' + $NL + '                        ["width"] = 300, ["height"] = 500' + $NL + '                    }' + $NL + '                }'
if ($c.Contains($old)) { $c = $c.Replace($old, $new); [System.IO.File]::WriteAllText($f, $c); Write-Host 'shell skin added' } else { Write-Host 'anchor miss' }
