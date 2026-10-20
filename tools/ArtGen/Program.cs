using System.Drawing;
using System.Drawing.Imaging;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Encodings.Web;
using System.Security.Cryptography;

// ============================================================================
// ArtGen v4 - MC怪物素材管线
// 僵尸：MC皮肤UV拼合立绘；骷髅：原版贴图3D盒子等距渲染
// ============================================================================

// 仓库根目录：优先环境变量 MCMONSTERS_ROOT，其次从可执行文件位置向上找 pck_content
var repoRoot = ResolveRepoRoot();

static string ResolveRepoRoot()
{
    string? env = Environment.GetEnvironmentVariable("MCMONSTERS_ROOT");
    if (!string.IsNullOrEmpty(env) && Directory.Exists(env)) return env;
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir != null)
    {
        if (Directory.Exists(Path.Combine(dir.FullName, "pck_content")))
            return dir.FullName;
        dir = dir.Parent;
    }
    return Directory.GetCurrentDirectory();
}

// packonly 模式：只重新打包 pck，跳过所有素材生成（可在任意路径的仓库里构建）
bool packOnly = args.Length > 0 && args[0] == "packonly";
if (packOnly)
{
    string contentDir = Path.Combine(repoRoot, "pck_content");
    string outPck = Path.Combine(repoRoot, "deploy", "MCMonsters", "MCMonsters.pck");
    Directory.CreateDirectory(Path.GetDirectoryName(outPck)!);
    Console.WriteLine("仓库根: " + repoRoot);
    Console.WriteLine("\n=== 组装 MCMonsters.pck ===");
    ModPckAssembler.Assemble(contentDir, outPck);
    return;
}

var outDir = args.Length > 0 && args[0] != "preview" ? args[0]
    : Path.Combine(repoRoot, "pck_content", "animations", "monsters", "zombie");
Directory.CreateDirectory(outDir);

var userSkin = Path.Combine(repoRoot, "zombie_skin.png");

// ---- 通用辅助（全局作用域）----
JsonArray TransFrames(params (double t, double x, double y)[] kv)
{
    var arr = new JsonArray();
    foreach (var (t, x, y) in kv)
        arr.Add(new JsonObject { ["time"] = t, ["x"] = x, ["y"] = y });
    return arr;
}
JsonArray RotFrames(params (double t, double a)[] kv)
{
    var arr = new JsonArray();
    foreach (var (t, a) in kv)
        arr.Add(new JsonObject { ["time"] = t, ["angle"] = a });
    return arr;
}
var jsonOpts = new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, WriteIndented = true };

// preview模式：只拼合立绘预览
if (args.Length > 0 && args[0] == "preview")
{
    SpriteComposer.Compose(userSkin, Path.Combine(repoRoot, "zombie_composed_preview.png"), 6);
    return;
}

if (!packOnly)
{

// ============================ 1. 僵尸贴图：MC皮肤UV拼合 ============================
string composedPath = Path.Combine(outDir, "zombie.png");
SpriteComposer.Compose(userSkin, composedPath, 3);

// ============================ 2. 僵尸atlas ============================
var atlas =
"""
zombie.png
size:48,96
format:RGBA8888
filter:Nearest,Nearest
repeat:none
body
bounds:12,0,24,96
armL
bounds:0,24,12,36
armR
bounds:36,24,12,36
""";
var atlasPath = Path.Combine(outDir, "zombie.atlas");
File.WriteAllText(atlasPath, atlas);
Console.WriteLine($"OK {atlasPath}");

// ============================ 3. 僵尸Spine JSON（3骨骼）===========================
var zombieAnims = new JsonObject
{
    ["idle_loop"] = new JsonObject
    {
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["translate"] = TransFrames((0, 0, 0), (0.8, 0, 3), (1.6, 0, 0))
            },
            ["armL"] = new JsonObject
            {
                ["rotate"] = RotFrames((0, 0), (0.8, -5), (1.6, 0))
            },
            ["armR"] = new JsonObject
            {
                ["rotate"] = RotFrames((0, 0), (0.8, 5), (1.6, 0))
            }
        }
    },
    ["cast"] = new JsonObject
    {
        ["bones"] = new JsonObject
        {
            ["armL"] = new JsonObject { ["rotate"] = RotFrames((0, 0), (0.12, 35), (0.5, 0)) },
            ["armR"] = new JsonObject { ["rotate"] = RotFrames((0, 0), (0.12, -35), (0.5, 0)) }
        }
    },
    ["attack"] = new JsonObject
    {
        ["bones"] = new JsonObject
        {
            ["armR"] = new JsonObject { ["rotate"] = RotFrames((0, 0), (0.12, -55), (0.3, 20), (0.5, 0)) },
            ["root"] = new JsonObject { ["translate"] = TransFrames((0, 0, 0), (0.12, -6, 0), (0.5, 0, 0)) }
        }
    },
    ["hurt"] = new JsonObject
    {
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["translate"] = TransFrames((0, 0, 0), (0.05, -7, 0), (0.1, 7, 0), (0.15, -3, 0), (0.2, 0, 0))
            }
        }
    },
    ["die"] = new JsonObject
    {
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["rotate"] = RotFrames((0, 0), (0.6, -90)),
                ["translate"] = TransFrames((0, 0, 0), (0.6, 8, -4))
            }
        }
    }
};

var zombieSkel = new JsonObject
{
    ["skeleton"] = new JsonObject
    {
        ["hash"] = "mczombie005",
        ["spine"] = "4.2.43",
        ["x"] = -26, ["y"] = -50, ["width"] = 52, ["height"] = 100,
        ["images"] = "./",
        ["audio"] = "./"
    },
    ["bones"] = new JsonArray
    {
        new JsonObject { ["name"] = "root" },
        new JsonObject { ["name"] = "armL", ["parent"] = "root", ["x"] = -18, ["y"] = 54 },
        new JsonObject { ["name"] = "armR", ["parent"] = "root", ["x"] = 18, ["y"] = 54 }
    },
    ["slots"] = new JsonArray
    {
        new JsonObject { ["name"] = "armL", ["bone"] = "armL", ["attachment"] = "armL", ["color"] = "ffffffff" },
        new JsonObject { ["name"] = "body", ["bone"] = "root", ["attachment"] = "body", ["color"] = "ffffffff" },
        new JsonObject { ["name"] = "armR", ["bone"] = "armR", ["attachment"] = "armR", ["color"] = "ffffffff" }
    },
    ["skins"] = new JsonArray
    {
        new JsonObject
        {
            ["name"] = "default",
            ["attachments"] = new JsonObject
            {
                ["body"] = new JsonObject
                {
                    ["body"] = new JsonObject
                    {
                        ["type"] = "region", ["x"] = 0, ["y"] = 48,
                        ["width"] = 24, ["height"] = 96
                    }
                },
                ["armL"] = new JsonObject
                {
                    ["armL"] = new JsonObject
                    {
                        ["type"] = "region", ["x"] = 0, ["y"] = 0,
                        ["width"] = 12, ["height"] = 36
                    }
                },
                ["armR"] = new JsonObject
                {
                    ["armR"] = new JsonObject
                    {
                        ["type"] = "region", ["x"] = 0, ["y"] = 0,
                        ["width"] = 12, ["height"] = 36
                    }
                }
            }
        }
    },
    ["animations"] = zombieAnims
};

var zombieJsonPath = Path.Combine(outDir, "zombie.json");
File.WriteAllText(zombieJsonPath, zombieSkel.ToJsonString(jsonOpts));
Console.WriteLine($"OK {zombieJsonPath}");

} // end if (!packOnly)

// ============================ 1b. 骷髅（300x500 立绘，单骨骼整体动画）===========================
// 注意：skeleton.png（300x500，等距立体渲染带弓箭）为手工维护素材，本脚本不重新生成。
// 不透明包围盒实测：x[77..274] y[37..427]，底部留白72px，内容中心偏右25.5px。
// tscn 锚点据此推算：scale 0.64 → 可视高≈250px（与僵尸一致），脚底距地8px。
var skelDir = @"D:\塔2MOD\MCMonsters\pck_content\animations\monsters\skeleton";
Directory.CreateDirectory(skelDir);
// 基础帧（300x500 立绘）为手工维护素材；skeleton.png 是由它合成的 3帧图集（600x1000）
string skelPng = Path.Combine(skelDir, "skeleton.png");
string skelBaseFrame = Path.Combine(skelDir, "skeleton_base.png");
if (!File.Exists(skelBaseFrame))
    throw new FileNotFoundException("缺少手工素材 skeleton_base.png（300x500基础帧），请先放入 " + skelDir);
Console.WriteLine($"USE {skelBaseFrame}（手工基础帧，不重新生成）");

// atlas：3帧图集（full=原始 / full_flash=白色剪影射击帧 / full_draw=拉弓增亮帧）
// 布局：600x1000，full在(0,0)，full_flash在(300,0)，full_draw在(0,500)
string composedAtlasPng = Path.Combine(skelDir, "skeleton.png");
ComposeSkeletonFrames(skelBaseFrame, composedAtlasPng);
AddArrowToAtlasPng(composedAtlasPng, Path.Combine(skelDir, "arrow_source.png"), 300, 500);
Console.WriteLine($"OK {composedAtlasPng}（3帧图集 600x1000）");

var skelAtlas =
"""
skeleton.png
size:600,1000
format:RGBA8888
filter:Linear,Linear
repeat:none
full
bounds:0,0,300,500
full_flash
bounds:300,0,300,500
full_draw
bounds:0,500,300,500
arrow
bounds:300,500,140,140
""";
File.WriteAllText(Path.Combine(skelDir, "skeleton.atlas"), skelAtlas);
Console.WriteLine("OK skeleton.atlas");

// ---- 3帧合成：白剪影闪光帧 + 拉弓增亮帧 ----
static void ComposeSkeletonFrames(string baseFramePath, string outPath)
{
    using var baseFrame = new System.Drawing.Bitmap(baseFramePath); // 300x500
    using var atlas = new System.Drawing.Bitmap(600, 1000, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
    using (var g = System.Drawing.Graphics.FromImage(atlas))
    {
        // full：(0,0)
        g.DrawImageUnscaled(baseFrame, 0, 0);
    }
    // full_flash：(300,0) 白色剪影（保留alpha轮廓）
    using (var flash = (System.Drawing.Bitmap)baseFrame.Clone())
    {
        for (int y = 0; y < flash.Height; y++)
        for (int x = 0; x < flash.Width; x++)
        {
            var c = flash.GetPixel(x, y);
            if (c.A > 40) flash.SetPixel(x, y, System.Drawing.Color.FromArgb(c.A, 255, 255, 255));
        }
        using var g = System.Drawing.Graphics.FromImage(atlas);
        g.DrawImageUnscaled(flash, 300, 0);
    }
    // full_draw：(0,500) 拉弓帧：整体增亮1.25x（蓄力发光感）
    using (var draw = (System.Drawing.Bitmap)baseFrame.Clone())
    {
        for (int y = 0; y < draw.Height; y++)
        for (int x = 0; x < draw.Width; x++)
        {
            var c = draw.GetPixel(x, y);
            if (c.A > 40)
                draw.SetPixel(x, y, System.Drawing.Color.FromArgb(c.A,
                    (int)Math.Min(255, c.R * 1.25), (int)Math.Min(255, c.G * 1.25), (int)Math.Min(255, c.B * 1.1)));
        }
        using var g = System.Drawing.Graphics.FromImage(atlas);
        g.DrawImageUnscaled(draw, 0, 500);
    }
    atlas.Save(outPath, System.Drawing.Imaging.ImageFormat.Png);
}

// ============================ 3b. 骷髅Spine JSON（单骨骼整体动画）===========================
// 幅度基准：贴图高500单位、tscn缩放0.64 → 16单位≈1屏幕px。动作必须足够大才可感知。
// attack≈35px前冲刺+±12°倾角；hurt≈29px击退抖动；die整体后倒+淡出；cast拉弓后仰19px。
var skelAnims = new JsonObject
{
    ["idle_loop"] = new JsonObject
    {
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["translate"] = TransFrames((0, 0, 0), (0.9, 0, -10), (1.8, 0, 0)),
                ["rotate"] = RotFrames((0, 0), (0.45, 1.5), (0.9, 0), (1.35, -1.5), (1.8, 0))
            }
        }
    },
    ["cast"] = new JsonObject
    {
        // 瞄准：拉弓——后仰蓄力，保持，收回；期间换成拉弓增亮帧（attachment换帧=逐帧动画）
        ["slots"] = new JsonObject
        {
            ["full"] = new JsonObject
            {
                ["attachment"] = new JsonArray(
                    new JsonObject { ["time"] = 0, ["name"] = "full_draw" },
                    new JsonObject { ["time"] = 0.7, ["name"] = "full_draw" },
                    new JsonObject { ["time"] = 0.75, ["name"] = "full" })
            }
        },
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["rotate"] = RotFrames((0, 0), (0.45, 5), (0.75, 5), (1.0, 0)),
                ["translate"] = TransFrames((0, 0, 0), (0.45, 30, 0), (0.75, 30, 0), (1.0, 0, 0))
            }
        }
    },
    ["attack"] = new JsonObject
    {
        // 放箭：快速后坐→前冲射出→过冲回正；射出瞬间(t≈0.22)闪白帧
        ["slots"] = new JsonObject
        {
            ["full"] = new JsonObject
            {
                ["attachment"] = new JsonArray(
                    new JsonObject { ["time"] = 0, ["name"] = "full" },
                    new JsonObject { ["time"] = 0.2, ["name"] = "full_flash" },
                    new JsonObject { ["time"] = 0.32, ["name"] = "full" })
            },
            ["arrow"] = new JsonObject
            {
                ["attachment"] = new JsonArray(
                    new JsonObject { ["time"] = 0.2, ["name"] = "arrow" },
                    new JsonObject { ["time"] = 0.6 }),
                ["rgba"] = new JsonArray(
                    new JsonObject { ["time"] = 0.2, ["color"] = "ffffffff" },
                    new JsonObject { ["time"] = 0.5, ["color"] = "ffffffff" },
                    new JsonObject { ["time"] = 0.6, ["color"] = "ffffff00" })
            }
        },
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["rotate"] = RotFrames((0, 0), (0.1, 6), (0.22, -12), (0.38, 5), (0.7, 0)),
                ["translate"] = TransFrames((0, 0, 0), (0.1, 22, 0), (0.22, -55, 0), (0.38, 18, 0), (0.7, 0, 0))
            },
            ["arrowBone"] = new JsonObject
            {
                ["translate"] = new JsonArray(
                    new JsonObject { ["time"] = 0.2, ["x"] = 0, ["y"] = 0 },
                    new JsonObject { ["time"] = 0.3, ["x"] = -430, ["y"] = 45 },
                    new JsonObject { ["time"] = 0.42, ["x"] = -1000, ["y"] = 30 },
                    new JsonObject { ["time"] = 0.58, ["x"] = -1700, ["y"] = -160 }),
                ["rotate"] = new JsonArray(
                    new JsonObject { ["time"] = 0.2, ["angle"] = 140 },
                    new JsonObject { ["time"] = 0.58, ["angle"] = 158 })
            }
        }
    },
    ["hurt"] = new JsonObject
    {
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["translate"] = TransFrames((0, 0, 0), (0.08, 45, 0), (0.16, -18, 0), (0.26, 25, 0), (0.35, 0, 0)),
                ["rotate"] = RotFrames((0, 0), (0.08, -4), (0.2, 3), (0.35, 0))
            }
        }
    },
    ["die"] = new JsonObject
    {
        // 整体向后倒下（绕脚底轴），滑退；淡出交给引擎 ShouldFadeAfterDeath
        // 注意：不要加 slots/color 时间轴——Spine 4.2 要求 rgba 键名，用 color 会导致整个骨架解析失败（怪物隐身）
        ["slots"] = new JsonObject
        {
            ["full"] = new JsonObject
            {
                // 兜底：死亡时无论正处于哪个换帧状态都回到原始帧
                ["attachment"] = new JsonArray(
                    new JsonObject { ["time"] = 0, ["name"] = "full" })
            }
        },
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["rotate"] = RotFrames((0, 0), (0.55, -85), (0.9, -85)),
                ["translate"] = TransFrames((0, 0, 0), (0.55, 60, -30), (0.9, 65, -32))
            }
        }
    }
};

var skelSkel = new JsonObject
{
    ["skeleton"] = new JsonObject
    {
        ["hash"] = "mcskel013",
        ["spine"] = "4.2.43",
        ["x"] = -150, ["y"] = -260, ["width"] = 300, ["height"] = 520,
        ["images"] = "./",
        ["audio"] = "./"
    },
    ["bones"] = new JsonArray
    {
        new JsonObject { ["name"] = "root" },
        new JsonObject { ["name"] = "arrowBone", ["parent"] = "root", ["x"] = 0, ["y"] = 300 }
    },
    ["slots"] = new JsonArray
    {
        new JsonObject { ["name"] = "full", ["bone"] = "root", ["attachment"] = "full", ["color"] = "ffffffff" },
        new JsonObject { ["name"] = "arrow", ["bone"] = "arrowBone" }
    },
    ["skins"] = new JsonArray
    {
        new JsonObject
        {
            ["name"] = "default",
            ["attachments"] = new JsonObject
            {
                ["arrow"] = new JsonObject
                {
                    ["arrow"] = new JsonObject
                    {
                        ["type"] = "region", ["x"] = 0, ["y"] = 0,
                        ["width"] = 140, ["height"] = 140
                    }
                },
                ["full"] = new JsonObject
                {
                    ["full"] = new JsonObject
                    {
                        ["type"] = "region", ["x"] = 0, ["y"] = 250,
                        ["width"] = 300, ["height"] = 500
                    },
                    ["full_flash"] = new JsonObject
                    {
                        ["type"] = "region", ["x"] = 0, ["y"] = 250,
                        ["width"] = 300, ["height"] = 500
                    },
                    ["full_draw"] = new JsonObject
                    {
                        ["type"] = "region", ["x"] = 0, ["y"] = 250,
                        ["width"] = 300, ["height"] = 500
                    }
                }
            }
        }
    },
    ["animations"] = skelAnims
};

var skelJsonPath = Path.Combine(skelDir, "skeleton.json");
File.WriteAllText(skelJsonPath, skelSkel.ToJsonString(jsonOpts));
Console.WriteLine($"OK {skelJsonPath}");

// 骷髅视觉场景（锚点基于300x500贴图实测包围盒：可视高≈250px、脚底距地8px、水平居中）
var skelTscn =
"""
[gd_scene load_steps=3 format=3]

[ext_resource type="Script" uid="uid://cghtlla3sw0po" path="res://src/Core/Nodes/Combat/NCreatureVisuals.cs" id="1_visuals"]
[ext_resource type="SpineSkeletonDataResource" path="res://animations/monsters/skeleton/skeleton_skel_data.tres" id="2_skeldata"]

[node name="Skeleton" type="Node2D"]
script = ExtResource("1_visuals")
metadata/_edit_group_ = true

[node name="Visuals" type="SpineSprite" parent="."]
skeleton_data_res = ExtResource("2_skeldata")
preview_skin = "default"
preview_animation = "idle_loop"
unique_name_in_owner = true
position = Vector2(-16, 39)
scale = Vector2(0.64, 0.64)

[node name="Bounds" type="Control" parent="."]
unique_name_in_owner = true
layout_mode = 0
offset_left = -64.0
offset_top = -262.0
offset_right = 64.0
offset_bottom = -5.0
mouse_filter = 2

[node name="CenterPos" type="Marker2D" parent="."]
unique_name_in_owner = true
position = Vector2(0, -133)

[node name="IntentPos" type="Marker2D" parent="."]
unique_name_in_owner = true
position = Vector2(0, -295)

[node name="TalkPos" type="Marker2D" parent="."]
unique_name_in_owner = true
position = Vector2(-70, -220)
""";
File.WriteAllText(@"D:\塔2MOD\MCMonsters\pck_content\scenes\creature_visuals\skeleton.tscn", skelTscn);
Console.WriteLine("OK scenes/creature_visuals/skeleton.tscn");

// ============================ 1c. 苦力怕（程序化贴图 + 3D盒子等距渲染 + 双帧图集）===========================
// MC Creeper模型：头8x8x8 身8x12x4 腿4x6x4。贴图程序化生成（绿色迷彩+经典脸），风格与骷髅立体渲染一致。
var creeperDir = @"D:\塔2MOD\MCMonsters\pck_content\animations\monsters\creeper";
Directory.CreateDirectory(creeperDir);
string creeperTexPath = Path.Combine(creeperDir, "creeper_raw.png");
GenerateCreeperTexture(creeperTexPath);

// 基础帧：优先官方立绘 creeper_official.png（透明底整图，自动适配300x500）；否则盒子渲染回退
string creeperBaseFrame = Path.Combine(creeperDir, "creeper_base.png");
string officialArt = Path.Combine(creeperDir, "creeper_official.png");
if (File.Exists(officialArt))
{
    FitIntoCanvas(officialArt, creeperBaseFrame, 300, 500, bottomMargin: 5);
    Console.WriteLine($"USE {officialArt}（官方立绘，自动适配300x500）");
}
else
{
    // 盒子渲染回退：后腿→前腿→身体→头（后画的在上层），脚底 bottomY=35，scale=12
    var boxes = new[]
    {
        new BoxRenderer.Box(-2, 35, 4, 6, 4, ( 4,20), ( 0,20), ( 4,16)),          // 后左腿
        new BoxRenderer.Box( 2, 35, 4, 6, 4, ( 4,20), ( 0,20), ( 4,16)),          // 后右腿
        new BoxRenderer.Box(-3, 35, 4, 6, 4, ( 4,20), ( 0,20), ( 4,16), true),    // 前左腿（近侧，外移留出腿间凹槽）
        new BoxRenderer.Box( 3, 35, 4, 6, 4, ( 4,20), ( 0,20), ( 4,16)),          // 前右腿
        new BoxRenderer.Box( 0, 29, 8, 12, 4, (20,20), (16,20), (20,16)),         // 身体
        new BoxRenderer.Box( 0, 17, 8,  8, 8, ( 8, 8), ( 0, 8), ( 8, 0)),         // 头（脸在front面片）
    };
    using var creeperBitmap = BoxRenderer.Render(creeperTexPath, boxes, 300, 500, 8, 35, 12);
    creeperBitmap.Save(creeperBaseFrame, System.Drawing.Imaging.ImageFormat.Png);
    Console.WriteLine($"OK {creeperBaseFrame}（苦力怕盒子渲染回退）");
}

// 双帧图集：full(0,0) + full_flash(300,0)
string creeperAtlasPng = Path.Combine(creeperDir, "creeper.png");
ComposeTwoFrameAtlas(creeperBaseFrame, creeperAtlasPng);
Console.WriteLine($"OK {creeperAtlasPng}（苦力怕双帧图集 600x500）");

var creeperAtlas =
"""
creeper.png
size:600,500
format:RGBA8888
filter:Linear,Linear
repeat:none
full
bounds:0,0,300,500
full_flash
bounds:300,0,300,500
""";
File.WriteAllText(Path.Combine(creeperDir, "creeper.atlas"), creeperAtlas);
Console.WriteLine("OK creeper.atlas");

// ---- 苦力怕贴图：64x32 迷彩（2x2成簇，绿+灰白斑，贴近原版观感）+ 经典脸 ----
static void GenerateCreeperTexture(string outPath)
{
    using var tex = new System.Drawing.Bitmap(64, 32, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
    var rnd = new Random(20241016);   // 固定种子，可复现
    // 参考原版渲染：明暗绿簇 + 少量灰白/深灰斑
    var camo = new System.Drawing.Color[]
    {
        System.Drawing.Color.FromArgb(255,  26, 148,  34),
        System.Drawing.Color.FromArgb(255,  40, 176,  46),
        System.Drawing.Color.FromArgb(255,  58, 198,  62),
        System.Drawing.Color.FromArgb(255,  18, 108,  24),
        System.Drawing.Color.FromArgb(255,  10,  74,  16),
        System.Drawing.Color.FromArgb(255,  88, 214,  84),
        System.Drawing.Color.FromArgb(255, 150, 196, 132),   // 灰绿浅斑
        System.Drawing.Color.FromArgb(255, 120, 138, 116),   // 灰斑
        System.Drawing.Color.FromArgb(255,  70,  88,  66),   // 深灰绿
    };
    // 2x2像素成簇上色（原版贴图的迷彩是块状而非逐像素雪花）
    for (int y = 0; y < 32; y += 2)
    for (int x = 0; x < 64; x += 2)
    {
        var c = camo[rnd.Next(camo.Length)];
        for (int dy = 0; dy < 2 && y + dy < 32; dy++)
        for (int dx = 0; dx < 2 && x + dx < 64; dx++)
            tex.SetPixel(x + dx, y + dy, c);
    }

    // 头正面(8,8)-(16,16)：经典脸。局部坐标(0..7)：眼 rows2-3 cols1-2/5-6，嘴 row4 cols3-4，rows5-7 cols2-5
    var dark = System.Drawing.Color.FromArgb(255, 16, 16, 16);
    void FacePx(int cx, int cy) => tex.SetPixel(8 + cx, 8 + cy, dark);
    for (int r = 2; r <= 3; r++) { FacePx(1, r); FacePx(2, r); FacePx(5, r); FacePx(6, r); }
    FacePx(3, 4); FacePx(4, 4);
    for (int r = 5; r <= 7; r++) for (int c = 2; c <= 5; c++) FacePx(c, r);
    tex.Save(outPath, System.Drawing.Imaging.ImageFormat.Png);
}

// ---- 官方立绘适配：等比缩放进WxH画布，脚底贴底（留bottomMargin），水平居中 ----
// 支持webp源（GDI+不支持，走ImageMagick解码后转独立Bitmap——注意流生命周期坑）
static void FitIntoCanvas(string srcPath, string outPath, int w, int h, int bottomMargin)
{
    System.Drawing.Bitmap src;
    if (srcPath.EndsWith(".webp", StringComparison.OrdinalIgnoreCase))
    {
        using var ms = new MemoryStream();
        using (var mi = new ImageMagick.MagickImage(srcPath))
        {
            mi.Write(ms, ImageMagick.MagickFormat.Png);
        }
        ms.Position = 0;
        using var tmp = new System.Drawing.Bitmap(ms);
        src = (System.Drawing.Bitmap)tmp.Clone();
    }
    else
    {
        src = new System.Drawing.Bitmap(srcPath);
    }
    using (src)
    {
        double scale = Math.Min((double)w / src.Width, (double)(h - bottomMargin) / src.Height);
        int dw = Math.Max(1, (int)Math.Round(src.Width * scale));
        int dh = Math.Max(1, (int)Math.Round(src.Height * scale));
        using var canvas = new System.Drawing.Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using (var g = System.Drawing.Graphics.FromImage(canvas))
        {
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            g.DrawImage(src, (w - dw) / 2, h - bottomMargin - dh, dw, dh);
        }
        canvas.Save(outPath, System.Drawing.Imaging.ImageFormat.Png);
    }
}

// ---- 双帧合成：原始 + 白剪影 ----

// ---- 把箭矢贴入图集并扩展画布 ----
static void AddArrowToAtlasPng(string atlasPngPath, string arrowPath, int px, int py)
{
    // 先复制到内存位图，释放原文件句柄（GDI+保存时不能与原文件读取句柄共存）
    System.Drawing.Bitmap atlasCopy;
    using (var src = new System.Drawing.Bitmap(atlasPngPath))
    {
        atlasCopy = new System.Drawing.Bitmap(src);
    }
    System.Drawing.Bitmap arrowCopy;
    using (var src = new System.Drawing.Bitmap(arrowPath))
    {
        arrowCopy = new System.Drawing.Bitmap(src);
    }
    using (atlasCopy)
    using (arrowCopy)
    using (var canvas = new System.Drawing.Bitmap(Math.Max(atlasCopy.Width, px + 140), Math.Max(atlasCopy.Height, py + 140), System.Drawing.Imaging.PixelFormat.Format32bppArgb))
    {
        using (var g = System.Drawing.Graphics.FromImage(canvas))
        {
            g.DrawImageUnscaled(atlasCopy, 0, 0);
            // 箭矢缩小绘制（140x140，原图300x300）
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.DrawImage(arrowCopy, new System.Drawing.Rectangle(px, py, 140, 140));
        }
        canvas.Save(atlasPngPath, System.Drawing.Imaging.ImageFormat.Png);
    }
}

static void ComposeTwoFrameAtlasWithArrow(string baseFramePath, string arrowPath, string outPath)
{
    ComposeTwoFrameAtlas(baseFramePath, outPath);
    AddArrowToAtlasPng(outPath, arrowPath, 600, 100);
}

// ---- 潜影贝三帧图集：开壳(0) + 白闪(300) + 闭壳(600) ----
static void ComposeShulkerAtlas(string openFramePath, string closedSourcePath, string outPath)
{
    using var openImg = new System.Drawing.Bitmap(openFramePath);
    using var closedSrc = new System.Drawing.Bitmap(closedSourcePath);
    using var canvas = new System.Drawing.Bitmap(900, 500, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
    using (var g = System.Drawing.Graphics.FromImage(canvas))
    {
        // 开壳帧
        g.DrawImageUnscaled(openImg, 0, 0);
        // 白闪帧（开壳剪影）
        using var flash = (System.Drawing.Bitmap)openImg.Clone();
        for (int y = 0; y < flash.Height; y++)
        for (int x = 0; x < flash.Width; x++)
        {
            var px = flash.GetPixel(x, y);
            if (px.A > 40) flash.SetPixel(x, y, System.Drawing.Color.FromArgb(px.A, 255, 255, 255));
        }
        g.DrawImageUnscaled(flash, 300, 0);
        // 闭壳帧：等比适配300x500
        double sc = Math.Min(300.0 / closedSrc.Width, 495.0 / closedSrc.Height);
        int dw = Math.Max(1, (int)(closedSrc.Width * sc));
        int dh = Math.Max(1, (int)(closedSrc.Height * sc));
        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        g.DrawImage(closedSrc, 600 + (300 - dw) / 2, 500 - 5 - dh, dw, dh);
    }
    canvas.Save(outPath, System.Drawing.Imaging.ImageFormat.Png);
}
static void ComposeTwoFrameAtlas(string baseFramePath, string outPath)
{
    using var baseFrame = new System.Drawing.Bitmap(baseFramePath);
    using var atlas = new System.Drawing.Bitmap(600, 500, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
    using (var g = System.Drawing.Graphics.FromImage(atlas))
    {
        g.DrawImageUnscaled(baseFrame, 0, 0);
    }
    using (var flash = (System.Drawing.Bitmap)baseFrame.Clone())
    {
        for (int y = 0; y < flash.Height; y++)
        for (int x = 0; x < flash.Width; x++)
        {
            var c = flash.GetPixel(x, y);
            if (c.A > 40) flash.SetPixel(x, y, System.Drawing.Color.FromArgb(c.A, 255, 255, 255));
        }
        using var g = System.Drawing.Graphics.FromImage(atlas);
        g.DrawImageUnscaled(flash, 300, 0);
    }
    atlas.Save(outPath, System.Drawing.Imaging.ImageFormat.Png);
}

// ---- 苦力怕Spine JSON：膨胀(scale) + 爆炸闪白(attachment换帧) ----
var creeperAnims = new JsonObject
{
    ["idle_loop"] = new JsonObject
    {
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["translate"] = TransFrames((0, 0, 0), (0.9, 0, -9), (1.8, 0, 0)),
                ["rotate"] = RotFrames((0, 0), (0.45, 1.2), (0.9, 0), (1.35, -1.2), (1.8, 0))
            }
        }
    },
    ["cast"] = new JsonObject
    {
        // 嘶嘶：膨胀（scale时间轴，绕脚底放大）+ 轻微颤抖
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["scale"] = new JsonArray(
                    new JsonObject { ["time"] = 0, ["x"] = 1, ["y"] = 1 },
                    new JsonObject { ["time"] = 0.2, ["x"] = 1.12, ["y"] = 1.1 },
                    new JsonObject { ["time"] = 0.55, ["x"] = 1.08, ["y"] = 1.12 },
                    new JsonObject { ["time"] = 0.8, ["x"] = 1, ["y"] = 1 }),
                ["rotate"] = RotFrames((0, 0), (0.15, 1.5), (0.3, -1.5), (0.45, 1.5), (0.6, 0))
            }
        }
    },
    ["explode"] = new JsonObject
    {
        // 爆炸：急速膨胀到1.4x，t=0.25闪白帧，随后引擎自毁移除
        ["slots"] = new JsonObject
        {
            ["full"] = new JsonObject
            {
                ["attachment"] = new JsonArray(
                    new JsonObject { ["time"] = 0, ["name"] = "full" },
                    new JsonObject { ["time"] = 0.25, ["name"] = "full_flash" },
                    new JsonObject { ["time"] = 0.5, ["name"] = "full_flash" })
            }
        },
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["scale"] = new JsonArray(
                    new JsonObject { ["time"] = 0, ["x"] = 1, ["y"] = 1 },
                    new JsonObject { ["time"] = 0.15, ["x"] = 1.18, ["y"] = 1.14 },
                    new JsonObject { ["time"] = 0.4, ["x"] = 1.4, ["y"] = 1.32 }),
                ["translate"] = TransFrames((0, 0, 0), (0.05, -4, 0), (0.1, 4, 0), (0.15, -4, 0), (0.2, 0, 0))
            }
        }
    },
    ["hurt"] = new JsonObject
    {
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["translate"] = TransFrames((0, 0, 0), (0.08, 45, 0), (0.16, -18, 0), (0.26, 25, 0), (0.35, 0, 0)),
                ["rotate"] = RotFrames((0, 0), (0.08, -4), (0.2, 3), (0.35, 0))
            }
        }
    },
    ["die"] = new JsonObject
    {
        // 被玩家击杀（未爆）：向后倾倒；淡出交给引擎
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["rotate"] = RotFrames((0, 0), (0.55, -80), (0.9, -80)),
                ["translate"] = TransFrames((0, 0, 0), (0.55, 50, -20), (0.9, 55, -22))
            }
        }
    }
};

var creeperSkel = new JsonObject
{
    ["skeleton"] = new JsonObject
    {
        ["hash"] = "mccreeper001",
        ["spine"] = "4.2.43",
        ["x"] = -150, ["y"] = -260, ["width"] = 300, ["height"] = 520,
        ["images"] = "./",
        ["audio"] = "./"
    },
    ["bones"] = new JsonArray
    {
        new JsonObject { ["name"] = "root" }
    },
    ["slots"] = new JsonArray
    {
        new JsonObject { ["name"] = "full", ["bone"] = "root", ["attachment"] = "full", ["color"] = "ffffffff" }
    },
    ["skins"] = new JsonArray
    {
        new JsonObject
        {
            ["name"] = "default",
            ["attachments"] = new JsonObject
            {
                ["full"] = new JsonObject
                {
                    ["full"] = new JsonObject
                    {
                        ["type"] = "region", ["x"] = 0, ["y"] = 250,
                        ["width"] = 300, ["height"] = 500
                    },
                    ["full_flash"] = new JsonObject
                    {
                        ["type"] = "region", ["x"] = 0, ["y"] = 250,
                        ["width"] = 300, ["height"] = 500
                    }
                }
            }
        }
    },
    ["animations"] = creeperAnims
};

File.WriteAllText(Path.Combine(creeperDir, "creeper.json"), creeperSkel.ToJsonString(jsonOpts));
Console.WriteLine("OK creeper.json");

// Spine资源桥文件（atlas+json → SpineSkeletonDataResource）；漏掉会导致tscn加载失败怪物隐身
var creeperTres =
"""
[gd_resource type="SpineSkeletonDataResource" load_steps=3 format=3]

[ext_resource type="SpineAtlasResource" path="res://animations/monsters/creeper/creeper.atlas" id="1_atlas"]
[ext_resource type="SpineSkeletonFileResource" path="res://animations/monsters/creeper/creeper.json" id="2_skel"]

[resource]
atlas_res = ExtResource("1_atlas")
skeleton_file_res = ExtResource("2_skel")
default_mix = 0.02
""";
File.WriteAllText(Path.Combine(creeperDir, "creeper_skel_data.tres"), creeperTres);
Console.WriteLine("OK creeper_skel_data.tres");

// 苦力怕视觉场景：锚点从基础帧不透明包围盒自动推算（可视高目标205px、脚底距地8px、水平居中）
var creeperTscn = BuildCreeperTscn(creeperBaseFrame);
File.WriteAllText(@"D:\塔2MOD\MCMonsters\pck_content\scenes\creature_visuals\creeper.tscn", creeperTscn);
Console.WriteLine("OK scenes/creature_visuals/creeper.tscn");

static string BuildCreeperTscn(string baseFramePath)
{
    using var bmp = new System.Drawing.Bitmap(baseFramePath);
    int minX = bmp.Width, minY = bmp.Height, maxX = -1, maxY = -1;
    for (int y = 0; y < bmp.Height; y++)
    for (int x = 0; x < bmp.Width; x++)
    {
        if (bmp.GetPixel(x, y).A > 40)
        {
            if (x < minX) minX = x;
            if (x > maxX) maxX = x;
            if (y < minY) minY = y;
            if (y > maxY) maxY = y;
        }
    }
    double visibleH = maxY - minY + 1, visibleW = maxX - minX + 1;
    const double targetH = 240.0;         // 可视高目标（与僵尸/骷髅同等存在感，瘦长立绘略放大）
    const double feetAboveGround = 8.0;   // 脚底距地面锚点8px
    double s = targetH / visibleH;
    double feetFromTopSpine = 500.0 - maxY;              // 脚底以上部分的spine高度
    double py = -feetAboveGround + feetFromTopSpine * s; // Visuals节点y
    double px = -((minX + maxX) / 2.0 - 150.0) * s;      // 水平居中
    double topY = py - (500.0 - minY) * s;
    double halfW = visibleW * s / 2.0 + 6;
    double centerPosY = (topY - feetAboveGround) / 2.0;
    return $"""
[gd_scene load_steps=3 format=3]

[ext_resource type="Script" uid="uid://cghtlla3sw0po" path="res://src/Core/Nodes/Combat/NCreatureVisuals.cs" id="1_visuals"]
[ext_resource type="SpineSkeletonDataResource" path="res://animations/monsters/creeper/creeper_skel_data.tres" id="2_skeldata"]

[node name="Creeper" type="Node2D"]
script = ExtResource("1_visuals")
metadata/_edit_group_ = true

[node name="Visuals" type="SpineSprite" parent="."]
skeleton_data_res = ExtResource("2_skeldata")
preview_skin = "default"
preview_animation = "idle_loop"
unique_name_in_owner = true
position = Vector2({px:F1}, {py:F1})
scale = Vector2({s:F3}, {s:F3})

[node name="Bounds" type="Control" parent="."]
unique_name_in_owner = true
layout_mode = 0
offset_left = {-halfW:F1}
offset_top = {topY - 4:F1}
offset_right = {halfW:F1}
offset_bottom = -5.0
mouse_filter = 2

[node name="CenterPos" type="Marker2D" parent="."]
unique_name_in_owner = true
position = Vector2(0, {centerPosY:F1})

[node name="IntentPos" type="Marker2D" parent="."]
unique_name_in_owner = true
position = Vector2(0, {topY - 28:F1})

[node name="TalkPos" type="Marker2D" parent="."]
unique_name_in_owner = true
position = Vector2(-70, {topY + 40:F1})
""";
}

// ============================ 1d. 小僵尸鸡骑士（官方立绘 + 双帧图集，第1幕精英）===========================
var jockeyDir = @"D:\塔2MOD\MCMonsters\pck_content\animations\monsters\chicken_jockey";
Directory.CreateDirectory(jockeyDir);
// 基础帧与图集必须不同名：base=chicken_jockey_base.png（300x500），图集=chicken_jockey.png（600x500）
string jockeyBaseFrame = Path.Combine(jockeyDir, "chicken_jockey_base.png");
string jockeyArt = Path.Combine(jockeyDir, "jockey_official.png");
if (!File.Exists(jockeyArt))
    throw new FileNotFoundException("缺少官方立绘 jockey_official.png：" + jockeyArt);
FitIntoCanvas(jockeyArt, jockeyBaseFrame, 300, 500, bottomMargin: 5);
Console.WriteLine($"USE {jockeyArt}（鸡骑士官方立绘，自动适配300x500）");

// 双帧图集：full(0,0) + full_flash(300,0)
string jockeyAtlasPng = Path.Combine(jockeyDir, "chicken_jockey.png");
ComposeTwoFrameAtlas(jockeyBaseFrame, jockeyAtlasPng);
Console.WriteLine($"OK {jockeyAtlasPng}（鸡骑士双帧图集 600x500）");

var jockeyAtlas =
"""
chicken_jockey.png
size:600,500
format:RGBA8888
filter:Linear,Linear
repeat:none
full
bounds:0,0,300,500
full_flash
bounds:300,0,300,500
""";
File.WriteAllText(Path.Combine(jockeyDir, "chicken_jockey.atlas"), jockeyAtlas);
Console.WriteLine("OK chicken_jockey.atlas");

var jockeyAnims = new JsonObject
{
    ["idle_loop"] = new JsonObject
    {
        // 鸡步摇摆：上下颠 + 左右轻晃
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["translate"] = TransFrames((0, 0, 0), (0.45, 0, -8), (0.9, 0, 0), (1.35, 0, -8), (1.8, 0, 0)),
                ["rotate"] = RotFrames((0, 0), (0.45, 2), (0.9, 0), (1.35, -2), (1.8, 0))
            }
        }
    },
    ["cast"] = new JsonObject
    {
        // 振翅：快速抖动 + 轻微放大
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["scale"] = new JsonArray(
                    new JsonObject { ["time"] = 0, ["x"] = 1, ["y"] = 1 },
                    new JsonObject { ["time"] = 0.12, ["x"] = 1.06, ["y"] = 1.04 },
                    new JsonObject { ["time"] = 0.24, ["x"] = 1, ["y"] = 1 },
                    new JsonObject { ["time"] = 0.36, ["x"] = 1.06, ["y"] = 1.04 },
                    new JsonObject { ["time"] = 0.5, ["x"] = 1, ["y"] = 1 }),
                ["rotate"] = RotFrames((0, 0), (0.1, 3), (0.2, -3), (0.3, 3), (0.4, 0), (0.5, 0))
            }
        }
    },
    ["attack"] = new JsonObject
    {
        // 冲锋：后坐 → 前冲 → 回正
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["rotate"] = RotFrames((0, 0), (0.1, 5), (0.22, -10), (0.38, 4), (0.65, 0)),
                ["translate"] = TransFrames((0, 0, 0), (0.1, 20, 0), (0.22, -50, 0), (0.38, 15, 0), (0.65, 0, 0))
            }
        }
    },
    ["hurt"] = new JsonObject
    {
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["translate"] = TransFrames((0, 0, 0), (0.08, 45, 0), (0.16, -18, 0), (0.26, 25, 0), (0.35, 0, 0)),
                ["rotate"] = RotFrames((0, 0), (0.08, -4), (0.2, 3), (0.35, 0))
            }
        }
    },
    ["die"] = new JsonObject
    {
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["rotate"] = RotFrames((0, 0), (0.55, -85), (0.9, -85)),
                ["translate"] = TransFrames((0, 0, 0), (0.55, 55, -25), (0.9, 60, -27))
            }
        }
    }
};

var jockeySkel = new JsonObject
{
    ["skeleton"] = new JsonObject
    {
        ["hash"] = "mcjockey001",
        ["spine"] = "4.2.43",
        ["x"] = -150, ["y"] = -260, ["width"] = 300, ["height"] = 520,
        ["images"] = "./",
        ["audio"] = "./"
    },
    ["bones"] = new JsonArray
    {
        new JsonObject { ["name"] = "root" }
    },
    ["slots"] = new JsonArray
    {
        new JsonObject { ["name"] = "full", ["bone"] = "root", ["attachment"] = "full", ["color"] = "ffffffff" }
    },
    ["skins"] = new JsonArray
    {
        new JsonObject
        {
            ["name"] = "default",
            ["attachments"] = new JsonObject
            {
                ["full"] = new JsonObject
                {
                    ["full"] = new JsonObject
                    {
                        ["type"] = "region", ["x"] = 0, ["y"] = 250,
                        ["width"] = 300, ["height"] = 500
                    },
                    ["full_flash"] = new JsonObject
                    {
                        ["type"] = "region", ["x"] = 0, ["y"] = 250,
                        ["width"] = 300, ["height"] = 500
                    }
                }
            }
        }
    },
    ["animations"] = jockeyAnims
};

File.WriteAllText(Path.Combine(jockeyDir, "chicken_jockey.json"), jockeySkel.ToJsonString(jsonOpts));
Console.WriteLine("OK chicken_jockey.json");

// Spine资源桥文件
var jockeyTres =
"""
[gd_resource type="SpineSkeletonDataResource" load_steps=3 format=3]

[ext_resource type="SpineAtlasResource" path="res://animations/monsters/chicken_jockey/chicken_jockey.atlas" id="1_atlas"]
[ext_resource type="SpineSkeletonFileResource" path="res://animations/monsters/chicken_jockey/chicken_jockey.json" id="2_skel"]

[resource]
atlas_res = ExtResource("1_atlas")
skeleton_file_res = ExtResource("2_skel")
default_mix = 0.02
""";
File.WriteAllText(Path.Combine(jockeyDir, "chicken_jockey_skel_data.tres"), jockeyTres);
Console.WriteLine("OK chicken_jockey_skel_data.tres");

// 视觉场景（自动锚点：可视高250px、脚底距地8px、水平居中）
var jockeyTscn = BuildCreatureTscn(jockeyBaseFrame, "chicken_jockey", 250.0);
File.WriteAllText(@"D:\塔2MOD\MCMonsters\pck_content\scenes\creature_visuals\chicken_jockey.tscn", jockeyTscn);
Console.WriteLine("OK scenes/creature_visuals/chicken_jockey.tscn");

// ---- 通用视觉场景构建：从基础帧不透明包围盒自动推算锚点 ----
// intentScreenY >= 0 时为绝对定位：意图强制落在屏幕内部坐标intentScreenY处（巨型怪用）
static string BuildCreatureTscn(string baseFramePath, string slug, double targetH, double intentLift = 0, double markerY = 715.0, double intentScreenY = -1)
{
    using var bmp = new System.Drawing.Bitmap(baseFramePath);
    int minX = bmp.Width, minY = bmp.Height, maxX = -1, maxY = -1;
    for (int y = 0; y < bmp.Height; y++)
    for (int x = 0; x < bmp.Width; x++)
    {
        if (bmp.GetPixel(x, y).A > 40)
        {
            if (x < minX) minX = x;
            if (x > maxX) maxX = x;
            if (y < minY) minY = y;
            if (y > maxY) maxY = y;
        }
    }
    double visibleH = maxY - minY + 1, visibleW = maxX - minX + 1;
    const double feetAboveGround = 8.0;
    double s = targetH / visibleH;
    double feetFromTopSpine = 500.0 - maxY;
    double py = -feetAboveGround + feetFromTopSpine * s;
    double px = -((minX + maxX) / 2.0 - 150.0) * s;
    double topY = py - (500.0 - minY) * s;
    double halfW = visibleW * s / 2.0 + 6;
    double centerPosY = (topY - feetAboveGround) / 2.0;
    // 意图定位：intentScreenY >= 0 为绝对屏幕坐标（巨型怪用），否则默认精灵顶上方28屏幕px
    double ipY = (intentScreenY >= 0)
        ? (intentScreenY - markerY) / s
        : topY - 28 - intentLift / s;
    string cap = char.ToUpper(slug[0]) + slug[1..];
    return $"""
[gd_scene load_steps=3 format=3]

[ext_resource type="Script" uid="uid://cghtlla3sw0po" path="res://src/Core/Nodes/Combat/NCreatureVisuals.cs" id="1_visuals"]
[ext_resource type="SpineSkeletonDataResource" path="res://animations/monsters/{slug}/{slug}_skel_data.tres" id="2_skeldata"]

[node name="{cap}" type="Node2D"]
script = ExtResource("1_visuals")
metadata/_edit_group_ = true

[node name="Visuals" type="SpineSprite" parent="."]
skeleton_data_res = ExtResource("2_skeldata")
preview_skin = "default"
preview_animation = "idle_loop"
unique_name_in_owner = true
position = Vector2({px:F1}, {py:F1})
scale = Vector2({s:F3}, {s:F3})

[node name="Bounds" type="Control" parent="."]
unique_name_in_owner = true
layout_mode = 0
offset_left = {-halfW:F1}
offset_top = {topY - 4:F1}
offset_right = {halfW:F1}
offset_bottom = -5.0
mouse_filter = 2

[node name="CenterPos" type="Marker2D" parent="."]
unique_name_in_owner = true
position = Vector2(0, {centerPosY:F1})

[node name="IntentPos" type="Marker2D" parent="."]
unique_name_in_owner = true
position = Vector2(0, {ipY:F1})

[node name="TalkPos" type="Marker2D" parent="."]
unique_name_in_owner = true
position = Vector2(-70, {topY + 40:F1})
""";
}

// ============================ 1e. 蜘蛛（官方立绘 + 双帧图集，第1幕普通怪）===========================
var spiderDir = @"D:\塔2MOD\MCMonsters\pck_content\animations\monsters\spider";
Directory.CreateDirectory(spiderDir);
string spiderBaseFrame = Path.Combine(spiderDir, "spider_base.png");
string spiderArt = Path.Combine(spiderDir, "spider_official.png");
if (!File.Exists(spiderArt))
    throw new FileNotFoundException("缺少官方立绘 spider_official.png：" + spiderArt);
FitIntoCanvas(spiderArt, spiderBaseFrame, 300, 500, bottomMargin: 5);
Console.WriteLine($"USE {spiderArt}（蜘蛛官方立绘，自动适配300x500）");

string spiderAtlasPng = Path.Combine(spiderDir, "spider.png");
ComposeTwoFrameAtlas(spiderBaseFrame, spiderAtlasPng);
Console.WriteLine($"OK {spiderAtlasPng}（蜘蛛双帧图集 600x500）");

var spiderAtlas =
"""
spider.png
size:600,500
format:RGBA8888
filter:Linear,Linear
repeat:none
full
bounds:0,0,300,500
full_flash
bounds:300,0,300,500
""";
File.WriteAllText(Path.Combine(spiderDir, "spider.atlas"), spiderAtlas);
Console.WriteLine("OK spider.atlas");

var spiderAnims = new JsonObject
{
    ["idle_loop"] = new JsonObject
    {
        // 蜘蛛伏地轻颤：小幅起伏 + 微晃
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["translate"] = TransFrames((0, 0, 0), (0.45, 0, -5), (0.9, 0, 0), (1.35, 0, -5), (1.8, 0, 0)),
                ["rotate"] = RotFrames((0, 0), (0.45, 1), (0.9, 0), (1.35, -1), (1.8, 0))
            }
        }
    },
    ["cast"] = new JsonObject
    {
        // 喷丝/攀爬准备：后仰蓄力再前倾
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["rotate"] = RotFrames((0, 0), (0.2, 4), (0.4, -3), (0.6, 0)),
                ["translate"] = TransFrames((0, 0, 0), (0.2, 18, 0), (0.45, -8, 0), (0.6, 0, 0))
            }
        }
    },
    ["attack"] = new JsonObject
    {
        // 扑咬：压低后猛扑
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["rotate"] = RotFrames((0, 0), (0.1, 4), (0.22, -8), (0.38, 3), (0.6, 0)),
                ["translate"] = TransFrames((0, 0, 0), (0.1, 15, -6), (0.22, -58, -4), (0.38, 12, 0), (0.6, 0, 0))
            }
        }
    },
    ["hurt"] = new JsonObject
    {
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["translate"] = TransFrames((0, 0, 0), (0.08, 45, 0), (0.16, -18, 0), (0.26, 25, 0), (0.35, 0, 0)),
                ["rotate"] = RotFrames((0, 0), (0.08, -4), (0.2, 3), (0.35, 0))
            }
        }
    },
    ["die"] = new JsonObject
    {
        // 蜘蛛翻肚（rotate正角=向屏幕内翻）+ 微缩
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["rotate"] = RotFrames((0, 0), (0.5, 85), (0.9, 85)),
                ["translate"] = TransFrames((0, 0, 0), (0.5, -40, -20), (0.9, -45, -22))
            }
        }
    }
};

var spiderSkel = new JsonObject
{
    ["skeleton"] = new JsonObject
    {
        ["hash"] = "mcspider001",
        ["spine"] = "4.2.43",
        ["x"] = -150, ["y"] = -260, ["width"] = 300, ["height"] = 520,
        ["images"] = "./",
        ["audio"] = "./"
    },
    ["bones"] = new JsonArray
    {
        new JsonObject { ["name"] = "root" }
    },
    ["slots"] = new JsonArray
    {
        new JsonObject { ["name"] = "full", ["bone"] = "root", ["attachment"] = "full", ["color"] = "ffffffff" }
    },
    ["skins"] = new JsonArray
    {
        new JsonObject
        {
            ["name"] = "default",
            ["attachments"] = new JsonObject
            {
                ["full"] = new JsonObject
                {
                    ["full"] = new JsonObject
                    {
                        ["type"] = "region", ["x"] = 0, ["y"] = 250,
                        ["width"] = 300, ["height"] = 500
                    },
                    ["full_flash"] = new JsonObject
                    {
                        ["type"] = "region", ["x"] = 0, ["y"] = 250,
                        ["width"] = 300, ["height"] = 500
                    }
                }
            }
        }
    },
    ["animations"] = spiderAnims
};

File.WriteAllText(Path.Combine(spiderDir, "spider.json"), spiderSkel.ToJsonString(jsonOpts));
Console.WriteLine("OK spider.json");

var spiderTres =
"""
[gd_resource type="SpineSkeletonDataResource" load_steps=3 format=3]

[ext_resource type="SpineAtlasResource" path="res://animations/monsters/spider/spider.atlas" id="1_atlas"]
[ext_resource type="SpineSkeletonFileResource" path="res://animations/monsters/spider/spider.json" id="2_skel"]

[resource]
atlas_res = ExtResource("1_atlas")
skeleton_file_res = ExtResource("2_skel")
default_mix = 0.02
""";
File.WriteAllText(Path.Combine(spiderDir, "spider_skel_data.tres"), spiderTres);
Console.WriteLine("OK spider_skel_data.tres");

// 视觉场景（自动锚点：蜘蛛低矮宽扁，可视高目标160px）
var spiderTscn = BuildCreatureTscn(spiderBaseFrame, "spider", 160.0);
File.WriteAllText(@"D:\塔2MOD\MCMonsters\pck_content\scenes\creature_visuals\spider.tscn", spiderTscn);
Console.WriteLine("OK scenes/creature_visuals/spider.tscn");

// ============================ 1f. 烈焰人（官方动图逐帧 + N帧图集，第2幕普通怪）===========================
// 参考素材是WebP动图：用ImageMagick抽帧 → 每帧适配300x500 → 图集full/full_2..+full_flash
// idle_loop 用 attachment 换帧时间轴循环播放各帧（真·逐帧待机动画）
var blazeDir = @"D:\塔2MOD\MCMonsters\pck_content\animations\monsters\blaze";
Directory.CreateDirectory(blazeDir);
string blazeArt = Path.Combine(blazeDir, "blaze_official.webp");
if (!File.Exists(blazeArt))
    throw new FileNotFoundException("缺少官方动图 blaze_official.webp：" + blazeArt);

// 抽帧（最多4帧，等距采样）
var frameBitmaps = new List<System.Drawing.Bitmap>();
using (var collection = new ImageMagick.MagickImageCollection(blazeArt))
{
    int total = collection.Count;
    int take = Math.Min(4, total);
    Console.WriteLine($"动图共 {total} 帧，采样 {take} 帧做待机逐帧动画");
    for (int i = 0; i < take; i++)
    {
        int idx = total <= take ? i : (int)Math.Round(i * (total - 1) / (double)(take - 1));
        using var ms = new MemoryStream();
        collection[idx].Write(ms, ImageMagick.MagickFormat.Png);
        ms.Position = 0;
        // GDI+坑：Bitmap(Stream)会持有流引用，流关闭后任何绘制操作随机炸"generic error"
        // → 用临时位图复制出一份独立内存的位图
        using var tmp = new System.Drawing.Bitmap(ms);
        frameBitmaps.Add(new System.Drawing.Bitmap(tmp));
    }
}

// 每帧适配300x500画布（脚底贴底留5px）
string blazeBaseFrame = Path.Combine(blazeDir, "blaze_base.png");
int blazeFrameCount = frameBitmaps.Count;
using (var canvas = new System.Drawing.Bitmap(300, 500, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
{
    using (var g = System.Drawing.Graphics.FromImage(canvas))
    {
        var first = frameBitmaps[0];
        double scale = Math.Min(300.0 / first.Width, 495.0 / first.Height);
        int dw = Math.Max(1, (int)Math.Round(first.Width * scale));
        int dh = Math.Max(1, (int)Math.Round(first.Height * scale));
        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        g.DrawImage(first, (300 - dw) / 2, 500 - 5 - dh, dw, dh);
    }
    canvas.Save(blazeBaseFrame, System.Drawing.Imaging.ImageFormat.Png);
}
Console.WriteLine($"OK {blazeBaseFrame}（烈焰人首帧 300x500）");

// N帧图集：full(0) full_2(300) full_3(600) full_4(900) + full_flash(最后)，每格300x500
string blazeAtlasPng = Path.Combine(blazeDir, "blaze.png");
int blazeCols = blazeFrameCount + 1;
using (var atlas = new System.Drawing.Bitmap(300 * blazeCols, 500, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
{
    using (var g = System.Drawing.Graphics.FromImage(atlas))
    {
        // 每帧都按"适配300x500"方式绘制（与blaze_base.png一致），不能直接贴原始尺寸
        for (int i = 0; i < blazeFrameCount; i++)
            DrawFitted(g, frameBitmaps[i], 300 * i, 0, 300, 500, 5);
        // 白剪影闪光帧：基于首帧独立拷贝逐像素白化
        using var first = frameBitmaps[0];
        using var flash = (System.Drawing.Bitmap)first.Clone();
        for (int y = 0; y < flash.Height; y++)
        for (int x = 0; x < flash.Width; x++)
        {
            var c = flash.GetPixel(x, y);
            if (c.A > 40) flash.SetPixel(x, y, System.Drawing.Color.FromArgb(c.A, 255, 255, 255));
        }
        DrawFitted(g, flash, 300 * blazeFrameCount, 0, 300, 500, 5);
    }
    atlas.Save(blazeAtlasPng, System.Drawing.Imaging.ImageFormat.Png);
}
AddArrowToAtlasPng(blazeAtlasPng, Path.Combine(blazeDir, "fireball_source.png"), 300 * blazeCols, 0);
frameBitmaps.ForEach(b => b.Dispose());
Console.WriteLine($"OK {blazeAtlasPng}（烈焰人{blazeCols}列图集）");

// atlas文本：动态生成区域行
var atlasSb = new System.Text.StringBuilder();
atlasSb.AppendLine("blaze.png").AppendLine($"size:{300 * blazeCols + 140},500").AppendLine("format:RGBA8888")
    .AppendLine("filter:Linear,Linear").AppendLine("repeat:none").AppendLine("full")
    .AppendLine("bounds:0,0,300,500");
for (int i = 1; i < blazeFrameCount; i++)
    atlasSb.AppendLine($"full_{i + 1}").AppendLine($"bounds:{300 * i},0,300,500");
atlasSb.AppendLine("full_flash").AppendLine($"bounds:{300 * blazeFrameCount},0,300,500");
atlasSb.AppendLine("fireball").AppendLine($"bounds:{300 * blazeCols},0,140,140");
File.WriteAllText(Path.Combine(blazeDir, "blaze.atlas"), atlasSb.ToString());
Console.WriteLine("OK blaze.atlas");

// 待机逐帧：attachment时间轴按 0.45s/帧 轮播
var idleFrames = new JsonArray();
for (int i = 0; i < blazeFrameCount; i++)
{
    string name = i == 0 ? "full" : $"full_{i + 1}";
    idleFrames.Add(new JsonObject { ["time"] = Math.Round(i * 0.45, 2), ["name"] = name });
}
idleFrames.Add(new JsonObject { ["time"] = Math.Round(blazeFrameCount * 0.45, 2), ["name"] = "full" });

var blazeAnims = new JsonObject
{
    ["idle_loop"] = new JsonObject
    {
        ["slots"] = new JsonObject
        {
            ["full"] = new JsonObject { ["attachment"] = idleFrames }
        },
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["translate"] = TransFrames((0, 0, 0), (0.9, 0, -9), (1.8, 0, 0)),
                ["rotate"] = RotFrames((0, 0), (0.45, 1.2), (0.9, 0), (1.35, -1.2), (1.8, 0))
            }
        }
    },
    ["cast"] = new JsonObject
    {
        // 点燃：膨胀+高频颤抖（烧起来了）
        ["slots"] = new JsonObject
        {
            ["full"] = new JsonObject { ["attachment"] = new JsonArray(new JsonObject { ["time"] = 0, ["name"] = "full" }) },
            ["fireball"] = new JsonObject
            {
                ["attachment"] = new JsonArray(
                    new JsonObject { ["time"] = 0.05, ["name"] = "fireball" },
                    new JsonObject { ["time"] = 0.55 }),
                ["rgba"] = new JsonArray(
                    new JsonObject { ["time"] = 0.05, ["color"] = "ffffffff" },
                    new JsonObject { ["time"] = 0.42, ["color"] = "ffffffff" },
                    new JsonObject { ["time"] = 0.55, ["color"] = "ffffff00" })
            }
        },
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["scale"] = new JsonArray(
                    new JsonObject { ["time"] = 0, ["x"] = 1, ["y"] = 1 },
                    new JsonObject { ["time"] = 0.25, ["x"] = 1.08, ["y"] = 1.06 },
                    new JsonObject { ["time"] = 0.5, ["x"] = 1, ["y"] = 1 }),
                ["rotate"] = RotFrames((0, 0), (0.08, 2), (0.16, -2), (0.24, 2), (0.32, -2), (0.4, 0), (0.6, 0))
            },
            ["fireballBone"] = new JsonObject
            {
                ["translate"] = new JsonArray(
                    new JsonObject { ["time"] = 0.05, ["x"] = 0, ["y"] = 0 },
                    new JsonObject { ["time"] = 0.4, ["x"] = -1500, ["y"] = -30 }),
                ["scale"] = new JsonArray(
                    new JsonObject { ["time"] = 0.4, ["x"] = 1, ["y"] = 1 },
                    new JsonObject { ["time"] = 0.55, ["x"] = 2.2, ["y"] = 2.2 })
            }
        }
    },
    ["attack"] = new JsonObject
    {
        // 发射火球：后坐→前推
        ["slots"] = new JsonObject
        {
            ["full"] = new JsonObject { ["attachment"] = new JsonArray(new JsonObject { ["time"] = 0, ["name"] = "full" }) },
            ["fireball"] = new JsonObject
            {
                ["attachment"] = new JsonArray(
                    new JsonObject { ["time"] = 0.05, ["name"] = "fireball" },
                    new JsonObject { ["time"] = 0.55 }),
                ["rgba"] = new JsonArray(
                    new JsonObject { ["time"] = 0.05, ["color"] = "ffffffff" },
                    new JsonObject { ["time"] = 0.42, ["color"] = "ffffffff" },
                    new JsonObject { ["time"] = 0.55, ["color"] = "ffffff00" })
            }
        },
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["rotate"] = RotFrames((0, 0), (0.1, 5), (0.22, -8), (0.38, 3), (0.6, 0)),
                ["translate"] = TransFrames((0, 0, 0), (0.1, 18, 0), (0.22, -45, 0), (0.38, 12, 0), (0.6, 0, 0))
            },
            ["fireballBone"] = new JsonObject
            {
                ["translate"] = new JsonArray(
                    new JsonObject { ["time"] = 0.05, ["x"] = 0, ["y"] = 0 },
                    new JsonObject { ["time"] = 0.4, ["x"] = -1500, ["y"] = -30 }),
                ["scale"] = new JsonArray(
                    new JsonObject { ["time"] = 0.4, ["x"] = 1, ["y"] = 1 },
                    new JsonObject { ["time"] = 0.55, ["x"] = 2.2, ["y"] = 2.2 })
            }
        }
    },
    ["hurt"] = new JsonObject
    {
        ["slots"] = new JsonObject
        {
            ["full"] = new JsonObject { ["attachment"] = new JsonArray(new JsonObject { ["time"] = 0, ["name"] = "full" }) }
        },
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["translate"] = TransFrames((0, 0, 0), (0.08, 45, 0), (0.16, -18, 0), (0.26, 25, 0), (0.35, 0, 0)),
                ["rotate"] = RotFrames((0, 0), (0.08, -4), (0.2, 3), (0.35, 0))
            }
        }
    },
    ["die"] = new JsonObject
    {
        // 熄灭：熄火塌陷（缩小下沉）——烈焰人死时火灭烟散
        ["slots"] = new JsonObject
        {
            ["full"] = new JsonObject { ["attachment"] = new JsonArray(new JsonObject { ["time"] = 0, ["name"] = "full" }) }
        },
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["scale"] = new JsonArray(
                    new JsonObject { ["time"] = 0, ["x"] = 1, ["y"] = 1 },
                    new JsonObject { ["time"] = 0.5, ["x"] = 0.75, ["y"] = 0.55 },
                    new JsonObject { ["time"] = 0.9, ["x"] = 0.6, ["y"] = 0.4 }),
                ["translate"] = TransFrames((0, 0, 0), (0.5, 0, -40), (0.9, 0, -60))
            }
        }
    }
};

var blazeSkel = new JsonObject
{
    ["skeleton"] = new JsonObject
    {
        ["hash"] = "mcblaze001",
        ["spine"] = "4.2.43",
        ["x"] = -150, ["y"] = -260, ["width"] = 300, ["height"] = 520,
        ["images"] = "./",
        ["audio"] = "./"
    },
    ["bones"] = new JsonArray
    {
        new JsonObject { ["name"] = "root" },
        new JsonObject { ["name"] = "fireballBone", ["parent"] = "root", ["x"] = 0, ["y"] = 300 }
    },
    ["slots"] = new JsonArray
    {
        new JsonObject { ["name"] = "full", ["bone"] = "root", ["attachment"] = "full", ["color"] = "ffffffff" },
        new JsonObject { ["name"] = "fireball", ["bone"] = "fireballBone" }
    },
    ["skins"] = new JsonArray
    {
        new JsonObject
        {
            ["name"] = "default",
            ["attachments"] = new JsonObject
            {
                ["fireball"] = new JsonObject
                {
                    ["fireball"] = new JsonObject
                    {
                        ["type"] = "region", ["x"] = 0, ["y"] = 0,
                        ["width"] = 140, ["height"] = 140
                    }
                },
                ["full"] = BuildBlazeAttachments(blazeFrameCount)
            }
        }
    },
    ["animations"] = blazeAnims
};

File.WriteAllText(Path.Combine(blazeDir, "blaze.json"), blazeSkel.ToJsonString(jsonOpts));
Console.WriteLine($"OK blaze.json（皮肤注册 {blazeFrameCount}个待机帧 + full_flash）");

var blazeTres =
"""
[gd_resource type="SpineSkeletonDataResource" load_steps=3 format=3]

[ext_resource type="SpineAtlasResource" path="res://animations/monsters/blaze/blaze.atlas" id="1_atlas"]
[ext_resource type="SpineSkeletonFileResource" path="res://animations/monsters/blaze/blaze.json" id="2_skel"]

[resource]
atlas_res = ExtResource("1_atlas")
skeleton_file_res = ExtResource("2_skel")
default_mix = 0.02
""";
File.WriteAllText(Path.Combine(blazeDir, "blaze_skel_data.tres"), blazeTres);
Console.WriteLine("OK blaze_skel_data.tres");

// 视觉场景（自动锚点：烈焰人目标可视高480px——用户要求放大一倍）
var blazeTscn = BuildCreatureTscn(blazeBaseFrame, "blaze", 480.0);
File.WriteAllText(@"D:\塔2MOD\MCMonsters\pck_content\scenes\creature_visuals\blaze.tscn", blazeTscn);
Console.WriteLine("OK scenes/creature_visuals/blaze.tscn");

// ---- 等比适配绘制到指定画布格（脚底贴底留bottomMargin，水平居中）----
static void DrawFitted(System.Drawing.Graphics g, System.Drawing.Bitmap src, int cellX, int cellY, int cellW, int cellH, int bottomMargin)
{
    double scale = Math.Min((double)cellW / src.Width, (double)(cellH - bottomMargin) / src.Height);
    int dw = Math.Max(1, (int)Math.Round(src.Width * scale));
    int dh = Math.Max(1, (int)Math.Round(src.Height * scale));
    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
    g.DrawImage(src, cellX + (cellW - dw) / 2, cellY + cellH - bottomMargin - dh, dw, dh);
}

// ---- 烈焰人皮肤附件：按实际帧数注册（引用不存在的region会导致骨架加载失败→隐身）----
// 返回值直接就是slot内容：{ "full": {...}, "full_flash": {...} }，调用方放进 attachments[槽名]
static JsonObject BuildBlazeAttachments(int frameCount)
{
    var slot = new JsonObject();
    for (int i = 0; i < frameCount; i++)
    {
        string name = i == 0 ? "full" : $"full_{i + 1}";
        slot[name] = new JsonObject
        {
            ["type"] = "region", ["x"] = 0, ["y"] = 250,
            ["width"] = 300, ["height"] = 500
        };
    }
    slot["full_flash"] = new JsonObject
    {
        ["type"] = "region", ["x"] = 0, ["y"] = 250,
        ["width"] = 300, ["height"] = 500
    };
    return slot;
}

// ============================ 5. 自定义力量图标（已放弃）===========================
// 尝试过：mod自建ctex（过不了Godot WebP校验→NOPE占位）、引用官方power_atlas区域（同样失败）。
// 结论：mod自定义PowerModel图标在本游戏环境下无法正常显示，放弃。
// 现状：BUFF角标显示官方NOPE占位图，悬停名称/描述正常（走monsters表）。
// 如未来需要：可研究把图标塞进游戏已有的AtlasManager或替换missing_power资源。

// ============================ 5.5 鸡骑士遭遇战斗场景（槽位坐标）===========================
// HasScene=true 的遭遇必须有场景：每个槽位一个Marker2D标记站位，否则怪物没有坐标会飞天/回合卡死
var encSceneDir = @"D:\塔2MOD\MCMonsters\pck_content\scenes\encounters";
Directory.CreateDirectory(encSceneDir);
File.WriteAllText(Path.Combine(encSceneDir, "chicken_jockey_elite.tscn"),
"""
[gd_scene format=3]

[node name="Encounter" type="Control"]
layout_mode = 3
anchors_preset = 15
anchor_right = 1.0
anchor_bottom = 1.0
grow_horizontal = 2
grow_vertical = 2
mouse_filter = 2

[node name="jockey" type="Marker2D" parent="."]
position = Vector2(1505, 715)

[node name="minion1" type="Marker2D" parent="."]
position = Vector2(1260, 700)

[node name="minion2" type="Marker2D" parent="."]
position = Vector2(1750, 700)
""");
Console.WriteLine("OK scenes/encounters/chicken_jockey_elite.tscn（鸡骑士遭遇场景：3槽位坐标）");

// ============================ 1g. 僵尸猪人（官方立绘 + 双帧图集，第2幕普通怪）===========================
var piglinDir = @"D:\塔2MOD\MCMonsters\pck_content\animations\monsters\zombie_piglin";
Directory.CreateDirectory(piglinDir);
string piglinBaseFrame = Path.Combine(piglinDir, "zombie_piglin_base.png");
string piglinArt = Path.Combine(piglinDir, "piglin_official.png");
if (!File.Exists(piglinArt))
    throw new FileNotFoundException("缺少官方立绘 piglin_official.png：" + piglinArt);
FitIntoCanvas(piglinArt, piglinBaseFrame, 300, 500, bottomMargin: 5);
Console.WriteLine($"USE {piglinArt}（猪人官方立绘，自动适配300x500）");

string piglinAtlasPng = Path.Combine(piglinDir, "zombie_piglin.png");
ComposeTwoFrameAtlas(piglinBaseFrame, piglinAtlasPng);
Console.WriteLine($"OK {piglinAtlasPng}（猪人双帧图集 600x500）");

var piglinAtlas =
"""
zombie_piglin.png
size:600,500
format:RGBA8888
filter:Linear,Linear
repeat:none
full
bounds:0,0,300,500
full_flash
bounds:300,0,300,500
""";
File.WriteAllText(Path.Combine(piglinDir, "zombie_piglin.atlas"), piglinAtlas);
Console.WriteLine("OK zombie_piglin.atlas");

var piglinAnims = new JsonObject
{
    ["idle_loop"] = new JsonObject
    {
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["translate"] = TransFrames((0, 0, 0), (0.9, 0, -8), (1.8, 0, 0)),
                ["rotate"] = RotFrames((0, 0), (0.45, 1.5), (0.9, 0), (1.35, -1.5), (1.8, 0))
            }
        }
    },
    ["cast"] = new JsonObject
    {
        // 愤怒咆哮：抬头张口
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["rotate"] = RotFrames((0, 0), (0.2, 4), (0.45, -3), (0.6, 0)),
                ["translate"] = TransFrames((0, 0, 0), (0.2, 10, -5), (0.45, 0, 0), (0.6, 0, 0))
            }
        }
    },
    ["attack"] = new JsonObject
    {
        // 金剑斩：后坐→挥砍前冲
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["rotate"] = RotFrames((0, 0), (0.1, 5), (0.22, -11), (0.38, 4), (0.65, 0)),
                ["translate"] = TransFrames((0, 0, 0), (0.1, 20, 0), (0.22, -52, 0), (0.38, 16, 0), (0.65, 0, 0))
            }
        }
    },
    ["hurt"] = new JsonObject
    {
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["translate"] = TransFrames((0, 0, 0), (0.08, 45, 0), (0.16, -18, 0), (0.26, 25, 0), (0.35, 0, 0)),
                ["rotate"] = RotFrames((0, 0), (0.08, -4), (0.2, 3), (0.35, 0))
            }
        }
    },
    ["die"] = new JsonObject
    {
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["rotate"] = RotFrames((0, 0), (0.55, -85), (0.9, -85)),
                ["translate"] = TransFrames((0, 0, 0), (0.55, 55, -25), (0.9, 60, -27))
            }
        }
    }
};

var piglinSkel = new JsonObject
{
    ["skeleton"] = new JsonObject
    {
        ["hash"] = "mcpiglin001",
        ["spine"] = "4.2.43",
        ["x"] = -150, ["y"] = -260, ["width"] = 300, ["height"] = 520,
        ["images"] = "./",
        ["audio"] = "./"
    },
    ["bones"] = new JsonArray
    {
        new JsonObject { ["name"] = "root" }
    },
    ["slots"] = new JsonArray
    {
        new JsonObject { ["name"] = "full", ["bone"] = "root", ["attachment"] = "full", ["color"] = "ffffffff" }
    },
    ["skins"] = new JsonArray
    {
        new JsonObject
        {
            ["name"] = "default",
            ["attachments"] = new JsonObject
            {
                ["full"] = new JsonObject
                {
                    ["full"] = new JsonObject
                    {
                        ["type"] = "region", ["x"] = 0, ["y"] = 250,
                        ["width"] = 300, ["height"] = 500
                    },
                    ["full_flash"] = new JsonObject
                    {
                        ["type"] = "region", ["x"] = 0, ["y"] = 250,
                        ["width"] = 300, ["height"] = 500
                    }
                }
            }
        }
    },
    ["animations"] = piglinAnims
};

File.WriteAllText(Path.Combine(piglinDir, "zombie_piglin.json"), piglinSkel.ToJsonString(jsonOpts));
Console.WriteLine("OK zombie_piglin.json");

var piglinTres =
"""
[gd_resource type="SpineSkeletonDataResource" load_steps=3 format=3]

[ext_resource type="SpineAtlasResource" path="res://animations/monsters/zombie_piglin/zombie_piglin.atlas" id="1_atlas"]
[ext_resource type="SpineSkeletonFileResource" path="res://animations/monsters/zombie_piglin/zombie_piglin.json" id="2_skel"]

[resource]
atlas_res = ExtResource("1_atlas")
skeleton_file_res = ExtResource("2_skel")
default_mix = 0.02
""";
File.WriteAllText(Path.Combine(piglinDir, "zombie_piglin_skel_data.tres"), piglinTres);
Console.WriteLine("OK zombie_piglin_skel_data.tres");

// 视觉场景（自动锚点：猪人目标可视高250px）
var piglinTscn = BuildCreatureTscn(piglinBaseFrame, "zombie_piglin", 250.0);
File.WriteAllText(@"D:\塔2MOD\MCMonsters\pck_content\scenes\creature_visuals\zombie_piglin.tscn", piglinTscn);
Console.WriteLine("OK scenes/creature_visuals/zombie_piglin.tscn");

// ============================ 1h. 凋零骷髅（官方立绘 + 双帧图集，第2幕普通怪）===========================
var witherDir = @"D:\塔2MOD\MCMonsters\pck_content\animations\monsters\wither_skeleton";
Directory.CreateDirectory(witherDir);
string witherBaseFrame = Path.Combine(witherDir, "wither_skeleton_base.png");
string witherArt = Path.Combine(witherDir, "wither_skeleton_official.png");
if (!File.Exists(witherArt))
    throw new FileNotFoundException("缺少官方立绘 wither_skeleton_official.png：" + witherArt);
FitIntoCanvas(witherArt, witherBaseFrame, 300, 500, bottomMargin: 5);
Console.WriteLine($"USE {witherArt}（凋零骷髅官方立绘，自动适配300x500）");

string witherAtlasPng = Path.Combine(witherDir, "wither_skeleton.png");
ComposeTwoFrameAtlas(witherBaseFrame, witherAtlasPng);
Console.WriteLine($"OK {witherAtlasPng}（凋零骷髅双帧图集 600x500）");

var witherAtlas =
"""
wither_skeleton.png
size:600,500
format:RGBA8888
filter:Linear,Linear
repeat:none
full
bounds:0,0,300,500
full_flash
bounds:300,0,300,500
""";
File.WriteAllText(Path.Combine(witherDir, "wither_skeleton.atlas"), witherAtlas);
Console.WriteLine("OK wither_skeleton.atlas");

var witherAnims = new JsonObject
{
    ["idle_loop"] = new JsonObject
    {
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["translate"] = TransFrames((0, 0, 0), (0.9, 0, -8), (1.8, 0, 0)),
                ["rotate"] = RotFrames((0, 0), (0.45, 1.2), (0.9, 0), (1.35, -1.2), (1.8, 0))
            }
        }
    },
    ["cast"] = new JsonObject
    {
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["rotate"] = RotFrames((0, 0), (0.2, 3), (0.4, -3), (0.6, 0)),
                ["translate"] = TransFrames((0, 0, 0), (0.2, 12, -4), (0.45, 0, 0), (0.6, 0, 0))
            }
        }
    },
    ["attack"] = new JsonObject
    {
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["rotate"] = RotFrames((0, 0), (0.1, 5), (0.22, -12), (0.38, 4), (0.65, 0)),
                ["translate"] = TransFrames((0, 0, 0), (0.1, 18, 0), (0.22, -50, 0), (0.38, 15, 0), (0.65, 0, 0))
            }
        }
    },
    ["hurt"] = new JsonObject
    {
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["translate"] = TransFrames((0, 0, 0), (0.08, 45, 0), (0.16, -18, 0), (0.26, 25, 0), (0.35, 0, 0)),
                ["rotate"] = RotFrames((0, 0), (0.08, -4), (0.2, 3), (0.35, 0))
            }
        }
    },
    ["die"] = new JsonObject
    {
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["rotate"] = RotFrames((0, 0), (0.55, -85), (0.9, -85)),
                ["translate"] = TransFrames((0, 0, 0), (0.55, 55, -25), (0.9, 60, -27))
            }
        }
    }
};

var witherSkel = new JsonObject
{
    ["skeleton"] = new JsonObject
    {
        ["hash"] = "mcwither001",
        ["spine"] = "4.2.43",
        ["x"] = -150, ["y"] = -260, ["width"] = 300, ["height"] = 520,
        ["images"] = "./",
        ["audio"] = "./"
    },
    ["bones"] = new JsonArray
    {
        new JsonObject { ["name"] = "root" }
    },
    ["slots"] = new JsonArray
    {
        new JsonObject { ["name"] = "full", ["bone"] = "root", ["attachment"] = "full", ["color"] = "ffffffff" }
    },
    ["skins"] = new JsonArray
    {
        new JsonObject
        {
            ["name"] = "default",
            ["attachments"] = new JsonObject
            {
                ["full"] = new JsonObject
                {
                    ["full"] = new JsonObject
                    {
                        ["type"] = "region", ["x"] = 0, ["y"] = 250,
                        ["width"] = 300, ["height"] = 500
                    },
                    ["full_flash"] = new JsonObject
                    {
                        ["type"] = "region", ["x"] = 0, ["y"] = 250,
                        ["width"] = 300, ["height"] = 500
                    }
                }
            }
        }
    },
    ["animations"] = witherAnims
};

File.WriteAllText(Path.Combine(witherDir, "wither_skeleton.json"), witherSkel.ToJsonString(jsonOpts));
Console.WriteLine("OK wither_skeleton.json");

var witherTres =
"""
[gd_resource type="SpineSkeletonDataResource" load_steps=3 format=3]

[ext_resource type="SpineAtlasResource" path="res://animations/monsters/wither_skeleton/wither_skeleton.atlas" id="1_atlas"]
[ext_resource type="SpineSkeletonFileResource" path="res://animations/monsters/wither_skeleton/wither_skeleton.json" id="2_skel"]

[resource]
atlas_res = ExtResource("1_atlas")
skeleton_file_res = ExtResource("2_skel")
default_mix = 0.02
""";
File.WriteAllText(Path.Combine(witherDir, "wither_skeleton_skel_data.tres"), witherTres);
Console.WriteLine("OK wither_skeleton_skel_data.tres");

// 视觉场景（自动锚点：凋零骷髅目标可视高250px）
var witherTscn = BuildCreatureTscn(witherBaseFrame, "wither_skeleton", 250.0);
File.WriteAllText(@"D:\塔2MOD\MCMonsters\pck_content\scenes\creature_visuals\wither_skeleton.tscn", witherTscn);
Console.WriteLine("OK scenes/creature_visuals/wither_skeleton.tscn");

// ============================ 1i. 猪灵蛮兵（官方立绘 + 双帧图集，第2幕普通怪）===========================
var bruteDir = @"D:\塔2MOD\MCMonsters\pck_content\animations\monsters\piglin_brute";
Directory.CreateDirectory(bruteDir);
string bruteBaseFrame = Path.Combine(bruteDir, "piglin_brute_base.png");
string bruteArt = Path.Combine(bruteDir, "brute_official.png");
if (!File.Exists(bruteArt))
    throw new FileNotFoundException("缺少官方立绘 brute_official.png：" + bruteArt);
FitIntoCanvas(bruteArt, bruteBaseFrame, 300, 500, bottomMargin: 5);
Console.WriteLine($"USE {bruteArt}（蛮兵官方立绘，自动适配300x500）");

string bruteAtlasPng = Path.Combine(bruteDir, "piglin_brute.png");
ComposeTwoFrameAtlas(bruteBaseFrame, bruteAtlasPng);
Console.WriteLine($"OK {bruteAtlasPng}（蛮兵双帧图集 600x500）");

var bruteAtlas =
"""
piglin_brute.png
size:600,500
format:RGBA8888
filter:Linear,Linear
repeat:none
full
bounds:0,0,300,500
full_flash
bounds:300,0,300,500
""";
File.WriteAllText(Path.Combine(bruteDir, "piglin_brute.atlas"), bruteAtlas);
Console.WriteLine("OK piglin_brute.atlas");

var bruteAnims = new JsonObject
{
    ["idle_loop"] = new JsonObject
    {
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["translate"] = TransFrames((0, 0, 0), (0.9, 0, -7), (1.8, 0, 0)),
                ["rotate"] = RotFrames((0, 0), (0.45, 1), (0.9, 0), (1.35, -1), (1.8, 0))
            }
        }
    },
    ["cast"] = new JsonObject
    {
        // 不退：架斧格挡姿态（微蹲+前倾）
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["rotate"] = RotFrames((0, 0), (0.25, 2), (0.5, 2), (0.7, 0)),
                ["translate"] = TransFrames((0, 0, 0), (0.25, 6, 3), (0.5, 6, 3), (0.7, 0, 0))
            }
        }
    },
    ["attack"] = new JsonObject
    {
        // 金斧重劈：高举（后仰+抬起）→ 全力下劈
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["rotate"] = RotFrames((0, 0), (0.14, 7), (0.26, -13), (0.42, 4), (0.68, 0)),
                ["translate"] = TransFrames((0, 0, 0), (0.14, 22, 8), (0.26, -55, 6), (0.42, 18, 0), (0.68, 0, 0))
            }
        }
    },
    ["hurt"] = new JsonObject
    {
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["translate"] = TransFrames((0, 0, 0), (0.08, 40, 0), (0.16, -15, 0), (0.26, 22, 0), (0.35, 0, 0)),
                ["rotate"] = RotFrames((0, 0), (0.08, -3), (0.2, 3), (0.35, 0))
            }
        }
    },
    ["die"] = new JsonObject
    {
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["rotate"] = RotFrames((0, 0), (0.6, -88), (0.95, -88)),
                ["translate"] = TransFrames((0, 0, 0), (0.6, 55, -25), (0.95, 60, -27))
            }
        }
    }
};

var bruteSkel = new JsonObject
{
    ["skeleton"] = new JsonObject
    {
        ["hash"] = "mcbrute001",
        ["spine"] = "4.2.43",
        ["x"] = -150, ["y"] = -260, ["width"] = 300, ["height"] = 520,
        ["images"] = "./",
        ["audio"] = "./"
    },
    ["bones"] = new JsonArray
    {
        new JsonObject { ["name"] = "root" }
    },
    ["slots"] = new JsonArray
    {
        new JsonObject { ["name"] = "full", ["bone"] = "root", ["attachment"] = "full", ["color"] = "ffffffff" }
    },
    ["skins"] = new JsonArray
    {
        new JsonObject
        {
            ["name"] = "default",
            ["attachments"] = new JsonObject
            {
                ["full"] = new JsonObject
                {
                    ["full"] = new JsonObject
                    {
                        ["type"] = "region", ["x"] = 0, ["y"] = 250,
                        ["width"] = 300, ["height"] = 500
                    },
                    ["full_flash"] = new JsonObject
                    {
                        ["type"] = "region", ["x"] = 0, ["y"] = 250,
                        ["width"] = 300, ["height"] = 500
                    }
                }
            }
        }
    },
    ["animations"] = bruteAnims
};

File.WriteAllText(Path.Combine(bruteDir, "piglin_brute.json"), bruteSkel.ToJsonString(jsonOpts));
Console.WriteLine("OK piglin_brute.json");

var bruteTres =
"""
[gd_resource type="SpineSkeletonDataResource" load_steps=3 format=3]

[ext_resource type="SpineAtlasResource" path="res://animations/monsters/piglin_brute/piglin_brute.atlas" id="1_atlas"]
[ext_resource type="SpineSkeletonFileResource" path="res://animations/monsters/piglin_brute/piglin_brute.json" id="2_skel"]

[resource]
atlas_res = ExtResource("1_atlas")
skeleton_file_res = ExtResource("2_skel")
default_mix = 0.02
""";
File.WriteAllText(Path.Combine(bruteDir, "piglin_brute_skel_data.tres"), bruteTres);
Console.WriteLine("OK piglin_brute_skel_data.tres");

// 视觉场景（自动锚点：蛮兵宽厚，目标可视高255px）
var bruteTscn = BuildCreatureTscn(bruteBaseFrame, "piglin_brute", 255.0);
File.WriteAllText(@"D:\塔2MOD\MCMonsters\pck_content\scenes\creature_visuals\piglin_brute.tscn", bruteTscn);
Console.WriteLine("OK scenes/creature_visuals/piglin_brute.tscn");

// ============================ 1j. 恶魂（官方立绘 + 双帧图集，第2幕精英）===========================
var ghastDir = @"D:\塔2MOD\MCMonsters\pck_content\animations\monsters\ghast";
Directory.CreateDirectory(ghastDir);
string ghastBaseFrame = Path.Combine(ghastDir, "ghast_base.png");
string ghastArt = Path.Combine(ghastDir, "ghast_official.webp");
if (!File.Exists(ghastArt))
    throw new FileNotFoundException("缺少官方立绘 ghast_official.webp：" + ghastArt);
FitIntoCanvas(ghastArt, ghastBaseFrame, 300, 500, bottomMargin: 5);
Console.WriteLine($"USE {ghastArt}（恶魂官方立绘，自动适配300x500）");

string ghastAtlasPng = Path.Combine(ghastDir, "ghast.png");
ComposeTwoFrameAtlasWithArrow(ghastBaseFrame, Path.Combine(ghastDir, "fireball_source.png"), ghastAtlasPng);
Console.WriteLine($"OK {ghastAtlasPng}（恶魂双帧图集 600x500）");

var ghastAtlas =
"""
ghast.png
size:740,500
format:RGBA8888
filter:Linear,Linear
repeat:none
full
bounds:0,0,300,500
full_flash
bounds:300,0,300,500
fireball
bounds:600,100,140,140
""";
File.WriteAllText(Path.Combine(ghastDir, "ghast.atlas"), ghastAtlas);
Console.WriteLine("OK ghast.atlas");

var ghastAnims = new JsonObject
{
    ["idle_loop"] = new JsonObject
    {
        // 悬浮：缓慢大幅升降（飞行感）
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["translate"] = TransFrames((0, 0, 0), (0.9, 0, -16), (1.8, 0, 0)),
                ["rotate"] = RotFrames((0, 0), (0.45, 1), (0.9, 0), (1.35, -1), (1.8, 0))
            }
        }
    },
    ["cast"] = new JsonObject
    {
        // 蓄力吐火球：后仰蓄力 + 火焰弹飞出
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["rotate"] = RotFrames((0, 0), (0.25, 4), (0.45, 4), (0.7, 0)),
                ["translate"] = TransFrames((0, 0, 0), (0.25, 16, 4), (0.45, 16, 4), (0.7, 0, 0))
            },
            ["fireballBone"] = new JsonObject
            {
                ["translate"] = new JsonArray(
                    new JsonObject { ["time"] = 0.05, ["x"] = 0, ["y"] = 0 },
                    new JsonObject { ["time"] = 0.4, ["x"] = -1400, ["y"] = -330 }),
                ["scale"] = new JsonArray(
                    new JsonObject { ["time"] = 0.4, ["x"] = 1, ["y"] = 1 },
                    new JsonObject { ["time"] = 0.55, ["x"] = 2.2, ["y"] = 2.2 })
            }
        },
        ["slots"] = new JsonObject
        {
            ["fireball"] = new JsonObject
            {
                ["attachment"] = new JsonArray(
                    new JsonObject { ["time"] = 0.05, ["name"] = "fireball" },
                    new JsonObject { ["time"] = 0.55 }),
                ["rgba"] = new JsonArray(
                    new JsonObject { ["time"] = 0.05, ["color"] = "ffffffff" },
                    new JsonObject { ["time"] = 0.42, ["color"] = "ffffffff" },
                    new JsonObject { ["time"] = 0.55, ["color"] = "ffffff00" })
            }
        }
    },
    ["attack"] = new JsonObject
    {
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["rotate"] = RotFrames((0, 0), (0.1, -3), (0.24, 6), (0.4, -2), (0.6, 0)),
                ["translate"] = TransFrames((0, 0, 0), (0.1, -14, 0), (0.24, 40, 0), (0.4, -8, 0), (0.6, 0, 0))
            }
        }
    },
    ["hurt"] = new JsonObject
    {
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["translate"] = TransFrames((0, 0, 0), (0.08, 40, 0), (0.16, -16, 0), (0.26, 22, 0), (0.35, 0, 0)),
                ["rotate"] = RotFrames((0, 0), (0.08, -3), (0.2, 3), (0.35, 0))
            }
        }
    },
    ["die"] = new JsonObject
    {
        // 恶魂死亡：哭喊坠落
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["rotate"] = RotFrames((0, 0), (0.55, -12), (0.95, 20)),
                ["translate"] = TransFrames((0, 0, 0), (0.55, 20, 60), (0.95, 30, 140))
            }
        }
    }
};

var ghastSkel = new JsonObject
{
    ["skeleton"] = new JsonObject
    {
        ["hash"] = "mcghast001",
        ["spine"] = "4.2.43",
        ["x"] = -150, ["y"] = -260, ["width"] = 300, ["height"] = 520,
        ["images"] = "./",
        ["audio"] = "./"
    },
    ["bones"] = new JsonArray
    {
        new JsonObject { ["name"] = "root" },
        new JsonObject { ["name"] = "fireballBone", ["parent"] = "root", ["x"] = 0, ["y"] = 300 }
    },
    ["slots"] = new JsonArray
    {
        new JsonObject { ["name"] = "full", ["bone"] = "root", ["attachment"] = "full", ["color"] = "ffffffff" },
        new JsonObject { ["name"] = "fireball", ["bone"] = "fireballBone" }
    },
    ["skins"] = new JsonArray
    {
        new JsonObject
        {
            ["name"] = "default",
            ["attachments"] = new JsonObject
            {
                ["fireball"] = new JsonObject
                {
                    ["fireball"] = new JsonObject
                    {
                        ["type"] = "region", ["x"] = 0, ["y"] = 0,
                        ["width"] = 140, ["height"] = 140
                    }
                },
                ["full"] = new JsonObject
                {
                    ["full"] = new JsonObject
                    {
                        ["type"] = "region", ["x"] = 0, ["y"] = 250,
                        ["width"] = 300, ["height"] = 500
                    },
                    ["full_flash"] = new JsonObject
                    {
                        ["type"] = "region", ["x"] = 0, ["y"] = 250,
                        ["width"] = 300, ["height"] = 500
                    }
                }
            }
        }
    },
    ["animations"] = ghastAnims
};

File.WriteAllText(Path.Combine(ghastDir, "ghast.json"), ghastSkel.ToJsonString(jsonOpts));
Console.WriteLine("OK ghast.json");

var ghastTres =
"""
[gd_resource type="SpineSkeletonDataResource" load_steps=3 format=3]

[ext_resource type="SpineAtlasResource" path="res://animations/monsters/ghast/ghast.atlas" id="1_atlas"]
[ext_resource type="SpineSkeletonFileResource" path="res://animations/monsters/ghast/ghast.json" id="2_skel"]

[resource]
atlas_res = ExtResource("1_atlas")
skeleton_file_res = ExtResource("2_skel")
default_mix = 0.02
""";
File.WriteAllText(Path.Combine(ghastDir, "ghast_skel_data.tres"), ghastTres);
Console.WriteLine("OK ghast_skel_data.tres");

// 视觉场景（自动锚点：恶魂巨型目标可视高450px——用户要求放大50%）
var ghastTscn = BuildCreatureTscn(ghastBaseFrame, "ghast", 450.0);
File.WriteAllText(@"D:\塔2MOD\MCMonsters\pck_content\scenes\creature_visuals\ghast.tscn", ghastTscn);
Console.WriteLine("OK scenes/creature_visuals/ghast.tscn");

// ============================ 5.5b 恶魂遭遇战斗场景（悬浮槽位坐标）===========================
// 恶魂的ghast槽位标记抬高（y=530 vs 地面715）→ 悬浮在空中
// 注意：y不能低于~525，否则放大后的意图图标（sprite顶上方28px）会超出屏幕上沿
var ghastEncDir = @"D:\塔2MOD\MCMonsters\pck_content\scenes\encounters";
Directory.CreateDirectory(ghastEncDir);
File.WriteAllText(Path.Combine(ghastEncDir, "ghast_elite.tscn"),
"""
[gd_scene format=3]

[node name="Encounter" type="Control"]
layout_mode = 3
anchors_preset = 15
anchor_right = 1.0
anchor_bottom = 1.0
grow_horizontal = 2
grow_vertical = 2
mouse_filter = 2

[node name="ghast" type="Marker2D" parent="."]
position = Vector2(1505, 600)
""");
Console.WriteLine("OK scenes/encounters/ghast_elite.tscn（恶魂悬浮槽位 y=530）");

// ============================ 1k. 凋零BOSS（官方立绘 + 双帧图集，第2幕BOSS三形态）===========================
var wbDir = @"D:\塔2MOD\MCMonsters\pck_content\animations\monsters\wither_boss";
Directory.CreateDirectory(wbDir);
string wbBaseFrame = Path.Combine(wbDir, "wither_boss_base.png");
string wbArt = Path.Combine(wbDir, "wither_boss_official.png");
if (!File.Exists(wbArt))
    throw new FileNotFoundException("缺少官方立绘 wither_boss_official.png：" + wbArt);
FitIntoCanvas(wbArt, wbBaseFrame, 300, 500, bottomMargin: 5);
Console.WriteLine($"USE {wbArt}（凋零BOSS官方立绘，自动适配300x500）");

string wbAtlasPng = Path.Combine(wbDir, "wither_boss.png");
ComposeTwoFrameAtlas(wbBaseFrame, wbAtlasPng);
Console.WriteLine($"OK {wbAtlasPng}（凋零BOSS双帧图集 600x500）");

var wbAtlas =
"""
wither_boss.png
size:600,500
format:RGBA8888
filter:Linear,Linear
repeat:none
full
bounds:0,0,300,500
full_flash
bounds:300,0,300,500
""";
File.WriteAllText(Path.Combine(wbDir, "wither_boss.atlas"), wbAtlas);
Console.WriteLine("OK wither_boss.atlas");

var wbAnims = new JsonObject
{
    ["idle_loop"] = new JsonObject
    {
        // 三头颅悬浮：缓慢升降（BOSS的压迫感）
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["translate"] = TransFrames((0, 0, 0), (0.9, 0, -14), (1.8, 0, 0)),
                ["rotate"] = RotFrames((0, 0), (0.45, 1), (0.9, 0), (1.35, -1), (1.8, 0))
            }
        }
    },
    ["cast"] = new JsonObject
    {
        // 蓄力/吐弹：后仰蓄力
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["rotate"] = RotFrames((0, 0), (0.25, 4), (0.45, 4), (0.7, 0)),
                ["translate"] = TransFrames((0, 0, 0), (0.25, 14, 4), (0.45, 14, 4), (0.7, 0, 0))
            }
        }
    },
    ["attack"] = new JsonObject
    {
        // 冲撞：前冲
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["rotate"] = RotFrames((0, 0), (0.1, -3), (0.24, 5), (0.4, -2), (0.6, 0)),
                ["translate"] = TransFrames((0, 0, 0), (0.1, -12, 0), (0.24, 42, 0), (0.4, -8, 0), (0.6, 0, 0))
            }
        }
    },
    ["hurt"] = new JsonObject
    {
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["translate"] = TransFrames((0, 0, 0), (0.08, 40, 0), (0.16, -16, 0), (0.26, 22, 0), (0.35, 0, 0)),
                ["rotate"] = RotFrames((0, 0), (0.08, -3), (0.2, 3), (0.35, 0))
            }
        }
    },
    ["die"] = new JsonObject
    {
        // BOSS死亡：爆炸式翻滚坠落
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["rotate"] = RotFrames((0, 0), (0.5, -20), (0.9, 30)),
                ["translate"] = TransFrames((0, 0, 0), (0.5, 15, 50), (0.9, 30, 140))
            }
        }
    }
};

var wbSkel = new JsonObject
{
    ["skeleton"] = new JsonObject
    {
        ["hash"] = "mcwitherboss001",
        ["spine"] = "4.2.43",
        ["x"] = -150, ["y"] = -260, ["width"] = 300, ["height"] = 520,
        ["images"] = "./",
        ["audio"] = "./"
    },
    ["bones"] = new JsonArray
    {
        new JsonObject { ["name"] = "root" }
    },
    ["slots"] = new JsonArray
    {
        new JsonObject { ["name"] = "full", ["bone"] = "root", ["attachment"] = "full", ["color"] = "ffffffff" }
    },
    ["skins"] = new JsonArray
    {
        new JsonObject
        {
            ["name"] = "default",
            ["attachments"] = new JsonObject
            {
                ["full"] = new JsonObject
                {
                    ["full"] = new JsonObject
                    {
                        ["type"] = "region", ["x"] = 0, ["y"] = 250,
                        ["width"] = 300, ["height"] = 500
                    },
                    ["full_flash"] = new JsonObject
                    {
                        ["type"] = "region", ["x"] = 0, ["y"] = 250,
                        ["width"] = 300, ["height"] = 500
                    }
                }
            }
        }
    },
    ["animations"] = wbAnims
};

File.WriteAllText(Path.Combine(wbDir, "wither_boss.json"), wbSkel.ToJsonString(jsonOpts));
Console.WriteLine("OK wither_boss.json");

var wbTres =
"""
[gd_resource type="SpineSkeletonDataResource" load_steps=3 format=3]

[ext_resource type="SpineAtlasResource" path="res://animations/monsters/wither_boss/wither_boss.atlas" id="1_atlas"]
[ext_resource type="SpineSkeletonFileResource" path="res://animations/monsters/wither_boss/wither_boss.json" id="2_skel"]

[resource]
atlas_res = ExtResource("1_atlas")
skeleton_file_res = ExtResource("2_skel")
default_mix = 0.02
""";
File.WriteAllText(Path.Combine(wbDir, "wither_boss_skel_data.tres"), wbTres);
Console.WriteLine("OK wither_boss_skel_data.tres");

// 视觉场景（自动锚点：凋零BOSS巨型目标可视高380px）
var wbTscn = BuildCreatureTscn(wbBaseFrame, "wither_boss", 460.0);
File.WriteAllText(@"D:\塔2MOD\MCMonsters\pck_content\scenes\creature_visuals\wither_boss.tscn", wbTscn);
Console.WriteLine("OK scenes/creature_visuals/wither_boss.tscn");

// ---- BOSS遭遇场景：凋零槽位（悬浮，y按IntentPos自动计算保证意图可见：意图落在内部坐标~115）----
// 注意：遭遇场景文件名必须是 <遭遇Id>.tscn = wither_boss_encounter.tscn（类名WitherBossEncounter），勿改名
{
    var tscnText = System.IO.File.ReadAllText(@"D:\塔2MOD\MCMonsters\pck_content\scenes\creature_visuals\wither_boss.tscn");
    var m = System.Text.RegularExpressions.Regex.Match(tscnText, @"name = ""IntentPos""[^}]*?position = Vector2\(0, (-?[\d.]+)\)");
    double intentOffset = m.Success ? double.Parse(m.Groups[1].Value) : -460.0;
    double markerY = 200.0 - intentOffset;   // 让意图落在内部坐标y≈200（用户要求BOSS整体放低）
    var encDir2 = @"D:\塔2MOD\MCMonsters\pck_content\scenes\encounters";
    Directory.CreateDirectory(encDir2);
    File.WriteAllText(Path.Combine(encDir2, "wither_boss_encounter.tscn"),
$"""
[gd_scene format=3]

[node name="Encounter" type="Control"]
layout_mode = 3
anchors_preset = 15
anchor_right = 1.0
anchor_bottom = 1.0
grow_horizontal = 2
grow_vertical = 2
mouse_filter = 2

[node name="wither" type="Marker2D" parent="."]
position = Vector2(1505, {markerY:F0})
""");
    Console.WriteLine($"OK scenes/encounters/wither_boss_encounter.tscn（凋零BOSS悬浮槽位 y={markerY:F0}，意图自动落在y≈200）");
}

// ============================ 1j2. 末影人（官方立绘 + 双帧图集，第3幕普通怪）===========================
var endermanDir = @"D:\塔2MOD\MCMonsters\pck_content\animations\monsters\enderman";
Directory.CreateDirectory(endermanDir);
string endermanBaseFrame = Path.Combine(endermanDir, "enderman_base.png");
string endermanArt = Path.Combine(endermanDir, "enderman_official.png");
if (!File.Exists(endermanArt))
    throw new FileNotFoundException("缺少官方立绘 enderman_official.png：" + endermanArt);
FitIntoCanvas(endermanArt, endermanBaseFrame, 300, 500, bottomMargin: 5);
Console.WriteLine($"USE {endermanArt}（末影人官方立绘，自动适配300x500）");

string endermanAtlasPng = Path.Combine(endermanDir, "enderman.png");
ComposeTwoFrameAtlas(endermanBaseFrame, endermanAtlasPng);
Console.WriteLine($"OK {endermanAtlasPng}（末影人双帧图集 600x500）");

var endermanAtlas =
"""
enderman.png
size:600,500
format:RGBA8888
filter:Linear,Linear
repeat:none
full
bounds:0,0,300,500
full_flash
bounds:300,0,300,500
""";
File.WriteAllText(Path.Combine(endermanDir, "enderman.atlas"), endermanAtlas);
Console.WriteLine("OK enderman.atlas");

var endermanAnims = new JsonObject
{
    ["idle_loop"] = new JsonObject
    {
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["translate"] = TransFrames((0, 0, 0), (0.9, 0, -10), (1.8, 0, 0)),
                ["rotate"] = RotFrames((0, 0), (0.45, 1), (0.9, 0), (1.35, -1), (1.8, 0))
            }
        }
    },
    ["cast"] = new JsonObject
    {
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["scale"] = new JsonArray(
                    new JsonObject { ["time"] = 0, ["x"] = 1, ["y"] = 1 },
                    new JsonObject { ["time"] = 0.12, ["x"] = 0.9, ["y"] = 1.05 },
                    new JsonObject { ["time"] = 0.24, ["x"] = 1, ["y"] = 1 }),
                ["rotate"] = RotFrames((0, 0), (0.1, 3), (0.2, -3), (0.3, 0))
            }
        }
    },
    ["attack"] = new JsonObject
    {
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["rotate"] = RotFrames((0, 0), (0.1, 4), (0.22, -10), (0.38, 3), (0.6, 0)),
                ["translate"] = TransFrames((0, 0, 0), (0.1, 14, 0), (0.22, -48, 0), (0.38, 14, 0), (0.6, 0, 0))
            }
        }
    },
    ["hurt"] = new JsonObject
    {
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["translate"] = TransFrames((0, 0, 0), (0.08, 45, 0), (0.16, -18, 0), (0.26, 25, 0), (0.35, 0, 0)),
                ["rotate"] = RotFrames((0, 0), (0.08, -4), (0.2, 3), (0.35, 0))
            }
        }
    },
    ["die"] = new JsonObject
    {
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["scale"] = new JsonArray(
                    new JsonObject { ["time"] = 0, ["x"] = 1, ["y"] = 1 },
                    new JsonObject { ["time"] = 0.5, ["x"] = 0.7, ["y"] = 0.6 },
                    new JsonObject { ["time"] = 0.9, ["x"] = 0.3, ["y"] = 0.2 }),
                ["translate"] = TransFrames((0, 0, 0), (0.5, 0, -30), (0.9, 0, -50))
            }
        }
    }
};

var endermanSkel = new JsonObject
{
    ["skeleton"] = new JsonObject
    {
        ["hash"] = "mcenderman001",
        ["spine"] = "4.2.43",
        ["x"] = -150, ["y"] = -260, ["width"] = 300, ["height"] = 520,
        ["images"] = "./",
        ["audio"] = "./"
    },
    ["bones"] = new JsonArray
    {
        new JsonObject { ["name"] = "root" }
    },
    ["slots"] = new JsonArray
    {
        new JsonObject { ["name"] = "full", ["bone"] = "root", ["attachment"] = "full", ["color"] = "ffffffff" }
    },
    ["skins"] = new JsonArray
    {
        new JsonObject
        {
            ["name"] = "default",
            ["attachments"] = new JsonObject
            {
                ["full"] = new JsonObject
                {
                    ["full"] = new JsonObject
                    {
                        ["type"] = "region", ["x"] = 0, ["y"] = 250,
                        ["width"] = 300, ["height"] = 500
                    },
                    ["full_flash"] = new JsonObject
                    {
                        ["type"] = "region", ["x"] = 0, ["y"] = 250,
                        ["width"] = 300, ["height"] = 500
                    }
                }
            }
        }
    },
    ["animations"] = endermanAnims
};

File.WriteAllText(Path.Combine(endermanDir, "enderman.json"), endermanSkel.ToJsonString(jsonOpts));
Console.WriteLine("OK enderman.json");

var endermanTres =
"""
[gd_resource type="SpineSkeletonDataResource" load_steps=3 format=3]

[ext_resource type="SpineAtlasResource" path="res://animations/monsters/enderman/enderman.atlas" id="1_atlas"]
[ext_resource type="SpineSkeletonFileResource" path="res://animations/monsters/enderman/enderman.json" id="2_skel"]

[resource]
atlas_res = ExtResource("1_atlas")
skeleton_file_res = ExtResource("2_skel")
default_mix = 0.02
""";
File.WriteAllText(Path.Combine(endermanDir, "enderman_skel_data.tres"), endermanTres);
Console.WriteLine("OK enderman_skel_data.tres");

// 视觉场景（自动锚点：末影人目标可视高300px——全场最高怪）
var endermanTscn = BuildCreatureTscn(endermanBaseFrame, "enderman", 300.0);
File.WriteAllText(@"D:\塔2MOD\MCMonsters\pck_content\scenes\creature_visuals\enderman.tscn", endermanTscn);
Console.WriteLine("OK scenes/creature_visuals/enderman.tscn");
// ============================ 1j3. 潜影贝（官方立绘 + 双帧图集，第3幕普通怪）===========================
var shulkerDir = @"D:\塔2MOD\MCMonsters\pck_content\animations\monsters\shulker";
Directory.CreateDirectory(shulkerDir);
string shulkerBaseFrame = Path.Combine(shulkerDir, "shulker_base.png");
string shulkerArt = Path.Combine(shulkerDir, "shulker_official.png");
if (!File.Exists(shulkerArt))
    throw new FileNotFoundException("缺少官方立绘 shulker_official.png：" + shulkerArt);
FitIntoCanvas(shulkerArt, shulkerBaseFrame, 300, 500, bottomMargin: 5);
Console.WriteLine($"USE {shulkerArt}（潜影贝官方立绘，自动适配300x500）");

string shulkerAtlasPng = Path.Combine(shulkerDir, "shulker.png");
ComposeShulkerAtlas(shulkerBaseFrame, Path.Combine(shulkerDir, "shulker_closed_source.png"), shulkerAtlasPng);
Console.WriteLine($"OK {shulkerAtlasPng}（潜影贝双帧图集 600x500）");

var shulkerAtlas =
"""
shulker.png
size:900,500
format:RGBA8888
filter:Linear,Linear
repeat:none
full
bounds:0,0,300,500
full_flash
bounds:300,0,300,500
full_closed
bounds:600,0,300,500
""";
File.WriteAllText(Path.Combine(shulkerDir, "shulker.atlas"), shulkerAtlas);
Console.WriteLine("OK shulker.atlas");

var shulkerAnims = new JsonObject
{
    ["idle_loop"] = new JsonObject
    {
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["translate"] = TransFrames((0, 0, 0), (0.9, 0, -6), (1.8, 0, 0)),
                ["rotate"] = RotFrames((0, 0), (0.45, 1), (0.9, 0), (1.35, -1), (1.8, 0))
            }
        }
    },
    ["closed_loop"] = new JsonObject
    {
        // 闭壳待机：卸下开壳贴图，挂上闭壳方块，轻微呼吸浮动
        ["slots"] = new JsonObject
        {
            ["full"] = new JsonObject
            {
                ["attachment"] = new JsonArray(
                    new JsonObject { ["time"] = 0, ["name"] = "" })
            },
            ["shell"] = new JsonObject
            {
                ["attachment"] = new JsonArray(
                    new JsonObject { ["time"] = 0, ["name"] = "full_closed" })
            }
        },
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["translate"] = TransFrames((0, 0, 0), (0.9, 0, -6), (1.8, 0, 0))
            }
        }
    },
    ["cast"] = new JsonObject
    {
        // 闭壳动画只做壳缩紧脉冲，状态由closed_loop接管
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["scale"] = new JsonArray(
                    new JsonObject { ["time"] = 0, ["x"] = 1, ["y"] = 1 },
                    new JsonObject { ["time"] = 0.3, ["x"] = 0.88, ["y"] = 1.06 },
                    new JsonObject { ["time"] = 0.6, ["x"] = 1, ["y"] = 1 })
            }
        }
    },
    ["attack"] = new JsonObject
    {
        // 开壳：恢复开壳贴图并卸下闭壳
        ["slots"] = new JsonObject
        {
            ["full"] = new JsonObject
            {
                ["attachment"] = new JsonArray(
                    new JsonObject { ["time"] = 0, ["name"] = "full" })
            },
            ["shell"] = new JsonObject
            {
                ["attachment"] = new JsonArray(
                    new JsonObject { ["time"] = 0, ["name"] = "" })
            }
        },
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["rotate"] = RotFrames((0, 0), (0.12, -3), (0.26, 4), (0.5, 0)),
                ["translate"] = TransFrames((0, 0, 0), (0.12, -10, -4), (0.26, 20, 0), (0.5, 0, 0))
            }
        }
    },
    ["hurt"] = new JsonObject
    {
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["translate"] = TransFrames((0, 0, 0), (0.08, 40, 0), (0.16, -16, 0), (0.26, 22, 0), (0.35, 0, 0)),
                ["rotate"] = RotFrames((0, 0), (0.08, -3), (0.2, 3), (0.35, 0))
            }
        }
    },
    ["die"] = new JsonObject
    {
        // 潜影贝死亡：壳翻倒
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["rotate"] = RotFrames((0, 0), (0.55, -80), (0.95, -80)),
                ["translate"] = TransFrames((0, 0, 0), (0.55, 40, -20), (0.95, 45, -22))
            }
        }
    }
};

var shulkerSkel = new JsonObject
{
    ["skeleton"] = new JsonObject
    {
        ["hash"] = "mcshulker001",
        ["spine"] = "4.2.43",
        ["x"] = -150, ["y"] = -260, ["width"] = 300, ["height"] = 520,
        ["images"] = "./",
        ["audio"] = "./"
    },
    ["bones"] = new JsonArray
    {
        new JsonObject { ["name"] = "root" }
    },
    ["slots"] = new JsonArray
    {
        new JsonObject { ["name"] = "full", ["bone"] = "root", ["attachment"] = "full", ["color"] = "ffffffff" },
        new JsonObject { ["name"] = "shell", ["bone"] = "root", ["color"] = "ffffffff" }
    },
    ["skins"] = new JsonArray
    {
        new JsonObject
        {
            ["name"] = "default",
            ["attachments"] = new JsonObject
            {
                ["full"] = new JsonObject
                {
                    ["full"] = new JsonObject
                    {
                        ["type"] = "region", ["x"] = 0, ["y"] = 250,
                        ["width"] = 300, ["height"] = 500
                    },
                    ["full_flash"] = new JsonObject
                    {
                        ["type"] = "region", ["x"] = 0, ["y"] = 250,
                        ["width"] = 300, ["height"] = 500
                    }
                },
                ["shell"] = new JsonObject
                {
                    ["full_closed"] = new JsonObject
                    {
                        ["type"] = "region", ["x"] = 0, ["y"] = 250,
                        ["width"] = 300, ["height"] = 500
                    }
                }
            }
        }
    },
    ["animations"] = shulkerAnims
};

File.WriteAllText(Path.Combine(shulkerDir, "shulker.json"), shulkerSkel.ToJsonString(jsonOpts));
Console.WriteLine("OK shulker.json");

var shulkerTres =
"""
[gd_resource type="SpineSkeletonDataResource" load_steps=3 format=3]

[ext_resource type="SpineAtlasResource" path="res://animations/monsters/shulker/shulker.atlas" id="1_atlas"]
[ext_resource type="SpineSkeletonFileResource" path="res://animations/monsters/shulker/shulker.json" id="2_skel"]

[resource]
atlas_res = ExtResource("1_atlas")
skeleton_file_res = ExtResource("2_skel")
default_mix = 0.02
""";
File.WriteAllText(Path.Combine(shulkerDir, "shulker_skel_data.tres"), shulkerTres);
Console.WriteLine("OK shulker_skel_data.tres");

// 视觉场景（自动锚点：潜影贝目标可视高230px——矮壮方盒）
var shulkerTscn = BuildCreatureTscn(shulkerBaseFrame, "shulker", 230.0);
File.WriteAllText(@"D:\塔2MOD\MCMonsters\pck_content\scenes\creature_visuals\shulker.tscn", shulkerTscn);
Console.WriteLine("OK scenes/creature_visuals/shulker.tscn");
// ============================ 1k2. 末影龙（官方立绘 + 双帧图集，第3幕BOSS）===========================
var dragonDir = @"D:\塔2MOD\MCMonsters\pck_content\animations\monsters\ender_dragon";
Directory.CreateDirectory(dragonDir);
string dragonBaseFrame = Path.Combine(dragonDir, "ender_dragon_base.png");
string dragonArt = Path.Combine(dragonDir, "ender_dragon_official.png");
if (!File.Exists(dragonArt))
    throw new FileNotFoundException("缺少官方立绘 ender_dragon_official.png：" + dragonArt);
FitIntoCanvas(dragonArt, dragonBaseFrame, 300, 500, bottomMargin: 5);
Console.WriteLine($"USE {dragonArt}（末影龙官方立绘，自动适配300x500）");

string dragonAtlasPng = Path.Combine(dragonDir, "ender_dragon.png");
ComposeTwoFrameAtlas(dragonBaseFrame, dragonAtlasPng);
Console.WriteLine($"OK {dragonAtlasPng}（末影龙双帧图集 600x500）");

var dragonAtlas =
"""
ender_dragon.png
size:600,500
format:RGBA8888
filter:Linear,Linear
repeat:none
full
bounds:0,0,300,500
full_flash
bounds:300,0,300,500
""";
File.WriteAllText(Path.Combine(dragonDir, "ender_dragon.atlas"), dragonAtlas);
Console.WriteLine("OK ender_dragon.atlas");

var dragonAnims = new JsonObject
{
    ["idle_loop"] = new JsonObject
    {
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["translate"] = TransFrames((0, 0, 0), (0.9, 0, -14), (1.8, 0, 0)),
                ["rotate"] = RotFrames((0, 0), (0.45, 1), (0.9, 0), (1.35, -1), (1.8, 0))
            }
        }
    },
    ["cast"] = new JsonObject
    {
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["rotate"] = RotFrames((0, 0), (0.25, 4), (0.45, 4), (0.7, 0)),
                ["translate"] = TransFrames((0, 0, 0), (0.25, 16, 4), (0.45, 16, 4), (0.7, 0, 0))
            }
        }
    },
    ["attack"] = new JsonObject
    {
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["rotate"] = RotFrames((0, 0), (0.1, -3), (0.24, 6), (0.4, -2), (0.6, 0)),
                ["translate"] = TransFrames((0, 0, 0), (0.1, -14, 0), (0.24, 40, 0), (0.4, -8, 0), (0.6, 0, 0))
            }
        }
    },
    ["hurt"] = new JsonObject
    {
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["translate"] = TransFrames((0, 0, 0), (0.08, 40, 0), (0.16, -16, 0), (0.26, 22, 0), (0.35, 0, 0)),
                ["rotate"] = RotFrames((0, 0), (0.08, -3), (0.2, 3), (0.35, 0))
            }
        }
    },
    ["die"] = new JsonObject
    {
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["rotate"] = RotFrames((0, 0), (0.55, -12), (0.95, 20)),
                ["translate"] = TransFrames((0, 0, 0), (0.55, 20, 60), (0.95, 30, 140))
            }
        }
    }
};

var dragonSkel = new JsonObject
{
    ["skeleton"] = new JsonObject
    {
        ["hash"] = "mcenderdragon001",
        ["spine"] = "4.2.43",
        ["x"] = -150, ["y"] = -260, ["width"] = 300, ["height"] = 520,
        ["images"] = "./",
        ["audio"] = "./"
    },
    ["bones"] = new JsonArray
    {
        new JsonObject { ["name"] = "root" }
    },
    ["slots"] = new JsonArray
    {
        new JsonObject { ["name"] = "full", ["bone"] = "root", ["attachment"] = "full", ["color"] = "ffffffff" }
    },
    ["skins"] = new JsonArray
    {
        new JsonObject
        {
            ["name"] = "default",
            ["attachments"] = new JsonObject
            {
                ["full"] = new JsonObject
                {
                    ["full"] = new JsonObject
                    {
                        ["type"] = "region", ["x"] = 0, ["y"] = 250,
                        ["width"] = 300, ["height"] = 500
                    },
                    ["full_flash"] = new JsonObject
                    {
                        ["type"] = "region", ["x"] = 0, ["y"] = 250,
                        ["width"] = 300, ["height"] = 500
                    }
                }
            }
        }
    },
    ["animations"] = dragonAnims
};

File.WriteAllText(Path.Combine(dragonDir, "ender_dragon.json"), dragonSkel.ToJsonString(jsonOpts));
Console.WriteLine("OK ender_dragon.json");

var dragonTres =
"""
[gd_resource type="SpineSkeletonDataResource" load_steps=3 format=3]

[ext_resource type="SpineAtlasResource" path="res://animations/monsters/ender_dragon/ender_dragon.atlas" id="1_atlas"]
[ext_resource type="SpineSkeletonFileResource" path="res://animations/monsters/ender_dragon/ender_dragon.json" id="2_skel"]

[resource]
atlas_res = ExtResource("1_atlas")
skeleton_file_res = ExtResource("2_skel")
default_mix = 0.02
""";
File.WriteAllText(Path.Combine(dragonDir, "ender_dragon_skel_data.tres"), dragonTres);
Console.WriteLine("OK ender_dragon_skel_data.tres");

// 视觉场景（自动锚点：末影龙巨型BOSS目标可视高420px）
var dragonTscn = BuildCreatureTscn(dragonBaseFrame, "ender_dragon", 560.0, 861.0);
File.WriteAllText(@"D:\塔2MOD\MCMonsters\pck_content\scenes\creature_visuals\ender_dragon.tscn", dragonTscn);
Console.WriteLine("OK scenes/creature_visuals/ender_dragon.tscn");

// ---- 末影龙遭遇场景：dragon槽位高抬（飞行在空中），意图自动落在y≈200 ----
{
    var dragonTscnText = System.IO.File.ReadAllText(@"D:\塔2MOD\MCMonsters\pck_content\scenes\creature_visuals\ender_dragon.tscn");
    var dm = System.Text.RegularExpressions.Regex.Match(dragonTscnText, @"name = ""IntentPos""[^}]*?position = Vector2\(0, (-?[\d.]+)\)");
    double dragonIntentOffset = dm.Success ? double.Parse(dm.Groups[1].Value) : -440.0;
    double dragonMarkerY = 200.0 - dragonIntentOffset;
    var dragonEncDir = @"D:\塔2MOD\MCMonsters\pck_content\scenes\encounters";
    Directory.CreateDirectory(dragonEncDir);
    File.WriteAllText(Path.Combine(dragonEncDir, "ender_dragon_boss.tscn"),
$"""
[gd_scene format=3]

[node name="Encounter" type="Control"]
layout_mode = 3
anchors_preset = 15
anchor_right = 1.0
anchor_bottom = 1.0
grow_horizontal = 2
grow_vertical = 2
mouse_filter = 2

[node name="dragon" type="Marker2D" parent="."]
position = Vector2(1505, {dragonMarkerY:F0})
""");
    Console.WriteLine($"OK scenes/encounters/ender_dragon_boss.tscn（末影龙悬浮槽位 y={dragonMarkerY:F0}，意图自动落在y≈200）");
}
// ============================ 1k3. 巨人（官方立绘 + 双帧图集，第3幕精英）===========================
// 巨型化设计：立绘放大到屏幕内只显示下半身，上半身探出屏幕外（经典巨物压迫感）
var giantDir = @"D:\塔2MOD\MCMonsters\pck_content\animations\monsters\giant";
Directory.CreateDirectory(giantDir);
string giantBaseFrame = Path.Combine(giantDir, "giant_base.png");
string giantArt = Path.Combine(giantDir, "giant_official.png");
if (!File.Exists(giantArt))
    throw new FileNotFoundException("缺少官方立绘 giant_official.png：" + giantArt);
FitIntoCanvas(giantArt, giantBaseFrame, 300, 500, bottomMargin: 5);
Console.WriteLine($"USE {giantArt}（巨人官方立绘，自动适配300x500）");

string giantAtlasPng = Path.Combine(giantDir, "giant.png");
ComposeTwoFrameAtlas(giantBaseFrame, giantAtlasPng);
Console.WriteLine($"OK {giantAtlasPng}（巨人双帧图集 600x500）");

var giantAtlas =
"""
giant.png
size:600,500
format:RGBA8888
filter:Linear,Linear
repeat:none
full
bounds:0,0,300,500
full_flash
bounds:300,0,300,500
""";
File.WriteAllText(Path.Combine(giantDir, "giant.atlas"), giantAtlas);
Console.WriteLine("OK giant.atlas");

var giantAnims = new JsonObject
{
    ["idle_loop"] = new JsonObject
    {
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["translate"] = TransFrames((0, 0, 0), (0.9, 0, -10), (1.8, 0, 0)),
                ["rotate"] = RotFrames((0, 0), (0.45, 1), (0.9, 0), (1.35, -1), (1.8, 0))
            }
        }
    },
    ["cast"] = new JsonObject
    {
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["rotate"] = RotFrames((0, 0), (0.2, 3), (0.45, 3), (0.7, 0)),
                ["translate"] = TransFrames((0, 0, 0), (0.2, 8, 4), (0.45, 8, 4), (0.7, 0, 0))
            }
        }
    },
    ["attack"] = new JsonObject
    {
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["rotate"] = RotFrames((0, 0), (0.14, 5), (0.26, -10), (0.42, 3), (0.68, 0)),
                ["translate"] = TransFrames((0, 0, 0), (0.14, 18, 6), (0.26, -45, 4), (0.42, 14, 0), (0.68, 0, 0))
            }
        }
    },
    ["hurt"] = new JsonObject
    {
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["translate"] = TransFrames((0, 0, 0), (0.08, 35, 0), (0.16, -14, 0), (0.26, 18, 0), (0.35, 0, 0)),
                ["rotate"] = RotFrames((0, 0), (0.08, -3), (0.2, 2), (0.35, 0))
            }
        }
    },
    ["die"] = new JsonObject
    {
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["rotate"] = RotFrames((0, 0), (0.6, -85), (0.95, -85)),
                ["translate"] = TransFrames((0, 0, 0), (0.6, 50, -20), (0.95, 55, -22))
            }
        }
    }
};

var giantSkel = new JsonObject
{
    ["skeleton"] = new JsonObject
    {
        ["hash"] = "mcgiant001",
        ["spine"] = "4.2.43",
        ["x"] = -150, ["y"] = -260, ["width"] = 300, ["height"] = 520,
        ["images"] = "./",
        ["audio"] = "./"
    },
    ["bones"] = new JsonArray
    {
        new JsonObject { ["name"] = "root" }
    },
    ["slots"] = new JsonArray
    {
        new JsonObject { ["name"] = "full", ["bone"] = "root", ["attachment"] = "full", ["color"] = "ffffffff" }
    },
    ["skins"] = new JsonArray
    {
        new JsonObject
        {
            ["name"] = "default",
            ["attachments"] = new JsonObject
            {
                ["full"] = new JsonObject
                {
                    ["full"] = new JsonObject
                    {
                        ["type"] = "region", ["x"] = 0, ["y"] = 250,
                        ["width"] = 300, ["height"] = 500
                    },
                    ["full_flash"] = new JsonObject
                    {
                        ["type"] = "region", ["x"] = 0, ["y"] = 250,
                        ["width"] = 300, ["height"] = 500
                    }
                }
            }
        }
    },
    ["animations"] = giantAnims
};

File.WriteAllText(Path.Combine(giantDir, "giant.json"), giantSkel.ToJsonString(jsonOpts));
Console.WriteLine("OK giant.json");

var giantTres =
"""
[gd_resource type="SpineSkeletonDataResource" load_steps=3 format=3]

[ext_resource type="SpineAtlasResource" path="res://animations/monsters/giant/giant.atlas" id="1_atlas"]
[ext_resource type="SpineSkeletonFileResource" path="res://animations/monsters/giant/giant.json" id="2_skel"]

[resource]
atlas_res = ExtResource("1_atlas")
skeleton_file_res = ExtResource("2_skel")
default_mix = 0.02
""";
File.WriteAllText(Path.Combine(giantDir, "giant_skel_data.tres"), giantTres);
Console.WriteLine("OK giant_skel_data.tres");

// 视觉场景（巨型化：可视高1100内部px——屏幕内只显示下半身）
// intentScreenY=200：意图固定在屏幕内上部（合理分配位置）
var giantTscn = BuildCreatureTscn(giantBaseFrame, "giant", 1100.0, 0, 715, 200.0);
File.WriteAllText(@"D:\塔2MOD\MCMonsters\pck_content\scenes\creature_visuals\giant.tscn", giantTscn);
Console.WriteLine("OK scenes/creature_visuals/giant.tscn");
// ============================ 1j4. 掠夺者（官方立绘 + 双帧图集，第3幕普通怪）===========================
var pillagerDir = @"D:\塔2MOD\MCMonsters\pck_content\animations\monsters\pillager";
Directory.CreateDirectory(pillagerDir);
string pillagerBaseFrame = Path.Combine(pillagerDir, "pillager_base.png");
string pillagerArt = Path.Combine(pillagerDir, "pillager_official.png");
if (!File.Exists(pillagerArt))
    throw new FileNotFoundException("缺少官方立绘 pillager_official.png：" + pillagerArt);
FitIntoCanvas(pillagerArt, pillagerBaseFrame, 300, 500, bottomMargin: 5);
Console.WriteLine($"USE {pillagerArt}（掠夺者官方立绘，自动适配300x500）");

string pillagerAtlasPng = Path.Combine(pillagerDir, "pillager.png");
ComposeTwoFrameAtlasWithArrow(pillagerBaseFrame, Path.Combine(pillagerDir, "arrow_source.png"), pillagerAtlasPng);
Console.WriteLine($"OK {pillagerAtlasPng}（掠夺者双帧图集 600x500）");

var pillagerAtlas =
"""
pillager.png
size:740,500
format:RGBA8888
filter:Linear,Linear
repeat:none
full
bounds:0,0,300,500
full_flash
bounds:300,0,300,500
arrow
bounds:600,100,140,140
""";
File.WriteAllText(Path.Combine(pillagerDir, "pillager.atlas"), pillagerAtlas);
Console.WriteLine("OK pillager.atlas");

var pillagerAnims = new JsonObject
{
    ["idle_loop"] = new JsonObject
    {
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["translate"] = TransFrames((0, 0, 0), (0.9, 0, -8), (1.8, 0, 0)),
                ["rotate"] = RotFrames((0, 0), (0.45, 1), (0.9, 0), (1.35, -1), (1.8, 0))
            }
        }
    },
    ["cast"] = new JsonObject
    {
        // 装填：举弩瞄准（微缩+侧转）
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["scale"] = new JsonArray(
                    new JsonObject { ["time"] = 0, ["x"] = 1, ["y"] = 1 },
                    new JsonObject { ["time"] = 0.3, ["x"] = 0.92, ["y"] = 1.04 },
                    new JsonObject { ["time"] = 0.6, ["x"] = 1, ["y"] = 1 }),
                ["rotate"] = RotFrames((0, 0), (0.3, 2), (0.6, 0))
            }
        }
    },
    ["attack"] = new JsonObject
    {
        // 射击：后坐力 + 箭矢飞出
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["rotate"] = RotFrames((0, 0), (0.1, 3), (0.22, -6), (0.4, 2), (0.6, 0)),
                ["translate"] = TransFrames((0, 0, 0), (0.1, 10, 0), (0.22, -28, 0), (0.4, 10, 0), (0.6, 0, 0))
            },
            ["arrowBone"] = new JsonObject
            {
                ["translate"] = new JsonArray(
                    new JsonObject { ["time"] = 0.15, ["x"] = 0, ["y"] = 0 },
                    new JsonObject { ["time"] = 0.26, ["x"] = -430, ["y"] = 45 },
                    new JsonObject { ["time"] = 0.38, ["x"] = -1000, ["y"] = 30 },
                    new JsonObject { ["time"] = 0.58, ["x"] = -1700, ["y"] = -160 }),
                ["rotate"] = new JsonArray(
                    new JsonObject { ["time"] = 0.15, ["angle"] = 140 },
                    new JsonObject { ["time"] = 0.58, ["angle"] = 158 })
            }
        },
        ["slots"] = new JsonObject
        {
            ["arrow"] = new JsonObject
            {
                ["attachment"] = new JsonArray(
                    new JsonObject { ["time"] = 0.15, ["name"] = "arrow" },
                    new JsonObject { ["time"] = 0.6 }),
                ["rgba"] = new JsonArray(
                    new JsonObject { ["time"] = 0.15, ["color"] = "ffffffff" },
                    new JsonObject { ["time"] = 0.5, ["color"] = "ffffffff" },
                    new JsonObject { ["time"] = 0.6, ["color"] = "ffffff00" })
            }
        }
    },
    ["hurt"] = new JsonObject
    {
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["translate"] = TransFrames((0, 0, 0), (0.08, 45, 0), (0.16, -18, 0), (0.26, 25, 0), (0.35, 0, 0)),
                ["rotate"] = RotFrames((0, 0), (0.08, -4), (0.2, 3), (0.35, 0))
            }
        }
    },
    ["die"] = new JsonObject
    {
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["rotate"] = RotFrames((0, 0), (0.55, -85), (0.95, -85)),
                ["translate"] = TransFrames((0, 0, 0), (0.55, 45, -20), (0.95, 50, -22))
            }
        }
    }
};

var pillagerSkel = new JsonObject
{
    ["skeleton"] = new JsonObject
    {
        ["hash"] = "mcpillager001",
        ["spine"] = "4.2.43",
        ["x"] = -150, ["y"] = -260, ["width"] = 300, ["height"] = 520,
        ["images"] = "./",
        ["audio"] = "./"
    },
    ["bones"] = new JsonArray
    {
        new JsonObject { ["name"] = "root" },
        new JsonObject { ["name"] = "arrowBone", ["parent"] = "root", ["x"] = 0, ["y"] = 300 }
    },
    ["slots"] = new JsonArray
    {
        new JsonObject { ["name"] = "full", ["bone"] = "root", ["attachment"] = "full", ["color"] = "ffffffff" },
        new JsonObject { ["name"] = "arrow", ["bone"] = "arrowBone" }
    },
    ["skins"] = new JsonArray
    {
        new JsonObject
        {
            ["name"] = "default",
            ["attachments"] = new JsonObject
            {
                ["arrow"] = new JsonObject
                {
                    ["arrow"] = new JsonObject
                    {
                        ["type"] = "region", ["x"] = 0, ["y"] = 0,
                        ["width"] = 140, ["height"] = 140
                    }
                },
                ["full"] = new JsonObject
                {
                    ["full"] = new JsonObject
                    {
                        ["type"] = "region", ["x"] = 0, ["y"] = 250,
                        ["width"] = 300, ["height"] = 500
                    },
                    ["full_flash"] = new JsonObject
                    {
                        ["type"] = "region", ["x"] = 0, ["y"] = 250,
                        ["width"] = 300, ["height"] = 500
                    }
                }
            }
        }
    },
    ["animations"] = pillagerAnims
};

File.WriteAllText(Path.Combine(pillagerDir, "pillager.json"), pillagerSkel.ToJsonString(jsonOpts));
Console.WriteLine("OK pillager.json");

var pillagerTres =
"""
[gd_resource type="SpineSkeletonDataResource" load_steps=3 format=3]

[ext_resource type="SpineAtlasResource" path="res://animations/monsters/pillager/pillager.atlas" id="1_atlas"]
[ext_resource type="SpineSkeletonFileResource" path="res://animations/monsters/pillager/pillager.json" id="2_skel"]

[resource]
atlas_res = ExtResource("1_atlas")
skeleton_file_res = ExtResource("2_skel")
default_mix = 0.02
""";
File.WriteAllText(Path.Combine(pillagerDir, "pillager_skel_data.tres"), pillagerTres);
Console.WriteLine("OK pillager_skel_data.tres");

// 视觉场景（自动锚点：掠夺者目标可视高250px）
var pillagerTscn = BuildCreatureTscn(pillagerBaseFrame, "pillager", 250.0);
File.WriteAllText(@"D:\塔2MOD\MCMonsters\pck_content\scenes\creature_visuals\pillager.tscn", pillagerTscn);
Console.WriteLine("OK scenes/creature_visuals/pillager.tscn");
// ============================ 1k4. 女巫（官方立绘 + 双帧图集，第3幕普通怪）===========================
var witchDir = @"D:\塔2MOD\MCMonsters\pck_content\animations\monsters\witch";
Directory.CreateDirectory(witchDir);
string witchBaseFrame = Path.Combine(witchDir, "witch_base.png");
string witchArt = Path.Combine(witchDir, "witch_official.png");
if (!File.Exists(witchArt))
    throw new FileNotFoundException("缺少官方立绘 witch_official.png：" + witchArt);
FitIntoCanvas(witchArt, witchBaseFrame, 300, 500, bottomMargin: 5);
Console.WriteLine($"USE {witchArt}（女巫官方立绘，自动适配300x500）");

string witchAtlasPng = Path.Combine(witchDir, "witch.png");
ComposeTwoFrameAtlasWithArrow(witchBaseFrame, Path.Combine(witchDir, "potion_source.png"), witchAtlasPng);
Console.WriteLine($"OK {witchAtlasPng}（女巫双帧图集 600x500）");

var witchAtlas =
"""
witch.png
size:740,500
format:RGBA8888
filter:Linear,Linear
repeat:none
full
bounds:0,0,300,500
full_flash
bounds:300,0,300,500
potion
bounds:600,100,140,140
""";
File.WriteAllText(Path.Combine(witchDir, "witch.atlas"), witchAtlas);
Console.WriteLine("OK witch.atlas");

var witchAnims = new JsonObject
{
    ["idle_loop"] = new JsonObject
    {
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["translate"] = TransFrames((0, 0, 0), (0.9, 0, -8), (1.8, 0, 0)),
                ["rotate"] = RotFrames((0, 0), (0.45, 1.5), (0.9, 0), (1.35, -1.5), (1.8, 0))
            }
        }
    },
    ["cast"] = new JsonObject
    {
        // 施法：抬手摇晃（施法前后摇摆）
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["rotate"] = RotFrames((0, 0), (0.15, -3), (0.3, 3), (0.45, -2), (0.6, 0)),
                ["translate"] = TransFrames((0, 0, 0), (0.15, 6, -6), (0.3, 6, -6), (0.45, 0, 0), (0.6, 0, 0))
            }
        }
    },
    ["attack"] = new JsonObject
    {
        // 投掷药水：抛物线飞出 + 旋转 + 命中爆开
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["rotate"] = RotFrames((0, 0), (0.1, 3), (0.22, -7), (0.4, 2), (0.6, 0)),
                ["translate"] = TransFrames((0, 0, 0), (0.1, 12, 0), (0.22, -30, 0), (0.4, 10, 0), (0.6, 0, 0))
            },
            ["potionBone"] = new JsonObject
            {
                ["translate"] = new JsonArray(
                    new JsonObject { ["time"] = 0.15, ["x"] = 0, ["y"] = 0 },
                    new JsonObject { ["time"] = 0.3, ["x"] = -480, ["y"] = 300 },
                    new JsonObject { ["time"] = 0.45, ["x"] = -950, ["y"] = 150 },
                    new JsonObject { ["time"] = 0.58, ["x"] = -1400, ["y"] = -200 }),
                ["rotate"] = new JsonArray(
                    new JsonObject { ["time"] = 0.15, ["angle"] = 0 },
                    new JsonObject { ["time"] = 0.58, ["angle"] = 250 }),
                ["scale"] = new JsonArray(
                    new JsonObject { ["time"] = 0.58, ["x"] = 1, ["y"] = 1 },
                    new JsonObject { ["time"] = 0.66, ["x"] = 1.6, ["y"] = 1.6 })
            }
        },
        ["slots"] = new JsonObject
        {
            ["potion"] = new JsonObject
            {
                ["attachment"] = new JsonArray(
                    new JsonObject { ["time"] = 0.15, ["name"] = "potion" },
                    new JsonObject { ["time"] = 0.66 }),
                ["rgba"] = new JsonArray(
                    new JsonObject { ["time"] = 0.15, ["color"] = "ffffffff" },
                    new JsonObject { ["time"] = 0.58, ["color"] = "ffffffff" },
                    new JsonObject { ["time"] = 0.66, ["color"] = "ffffff00" })
            }
        }
    },
    ["hurt"] = new JsonObject
    {
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["translate"] = TransFrames((0, 0, 0), (0.08, 40, 0), (0.16, -16, 0), (0.26, 22, 0), (0.35, 0, 0)),
                ["rotate"] = RotFrames((0, 0), (0.08, -3), (0.2, 3), (0.35, 0))
            }
        }
    },
    ["die"] = new JsonObject
    {
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["rotate"] = RotFrames((0, 0), (0.55, -80), (0.95, -80)),
                ["translate"] = TransFrames((0, 0, 0), (0.55, 45, -20), (0.95, 50, -22))
            }
        }
    }
};

var witchSkel = new JsonObject
{
    ["skeleton"] = new JsonObject
    {
        ["hash"] = "mcwitch001",
        ["spine"] = "4.2.43",
        ["x"] = -150, ["y"] = -260, ["width"] = 300, ["height"] = 520,
        ["images"] = "./",
        ["audio"] = "./"
    },
    ["bones"] = new JsonArray
    {
        new JsonObject { ["name"] = "root" },
        new JsonObject { ["name"] = "potionBone", ["parent"] = "root", ["x"] = 0, ["y"] = 300 }
    },
    ["slots"] = new JsonArray
    {
        new JsonObject { ["name"] = "full", ["bone"] = "root", ["attachment"] = "full", ["color"] = "ffffffff" },
        new JsonObject { ["name"] = "potion", ["bone"] = "potionBone" }
    },
    ["skins"] = new JsonArray
    {
        new JsonObject
        {
            ["name"] = "default",
            ["attachments"] = new JsonObject
            {
                ["potion"] = new JsonObject
                {
                    ["potion"] = new JsonObject
                    {
                        ["type"] = "region", ["x"] = 0, ["y"] = 0,
                        ["width"] = 140, ["height"] = 140
                    }
                },
                ["full"] = new JsonObject
                {
                    ["full"] = new JsonObject
                    {
                        ["type"] = "region", ["x"] = 0, ["y"] = 250,
                        ["width"] = 300, ["height"] = 500
                    },
                    ["full_flash"] = new JsonObject
                    {
                        ["type"] = "region", ["x"] = 0, ["y"] = 250,
                        ["width"] = 300, ["height"] = 500
                    }
                }
            }
        }
    },
    ["animations"] = witchAnims
};

File.WriteAllText(Path.Combine(witchDir, "witch.json"), witchSkel.ToJsonString(jsonOpts));
Console.WriteLine("OK witch.json");

var witchTres =
"""
[gd_resource type="SpineSkeletonDataResource" load_steps=3 format=3]

[ext_resource type="SpineAtlasResource" path="res://animations/monsters/witch/witch.atlas" id="1_atlas"]
[ext_resource type="SpineSkeletonFileResource" path="res://animations/monsters/witch/witch.json" id="2_skel"]

[resource]
atlas_res = ExtResource("1_atlas")
skeleton_file_res = ExtResource("2_skel")
default_mix = 0.02
""";
File.WriteAllText(Path.Combine(witchDir, "witch_skel_data.tres"), witchTres);
Console.WriteLine("OK witch_skel_data.tres");

// 视觉场景（自动锚点：女巫目标可视高280px——含尖帽）
var witchTscn = BuildCreatureTscn(witchBaseFrame, "witch", 280.0);
File.WriteAllText(@"D:\塔2MOD\MCMonsters\pck_content\scenes\creature_visuals\witch.tscn", witchTscn);
Console.WriteLine("OK scenes/creature_visuals/witch.tscn");
// ============================ 1k5. 监守者（官方立绘 + 双帧图集，第1幕BOSS）===========================
var wardenDir = @"D:\塔2MOD\MCMonsters\pck_content\animations\monsters\warden";
Directory.CreateDirectory(wardenDir);
string wardenBaseFrame = Path.Combine(wardenDir, "warden_base.png");
string wardenArt = Path.Combine(wardenDir, "warden_official.png");
if (!File.Exists(wardenArt))
    throw new FileNotFoundException("缺少官方立绘 warden_official.png：" + wardenArt);
FitIntoCanvas(wardenArt, wardenBaseFrame, 300, 500, bottomMargin: 5);
Console.WriteLine($"USE {wardenArt}（监守者官方立绘，自动适配300x500）");

string wardenAtlasPng = Path.Combine(wardenDir, "warden.png");
ComposeTwoFrameAtlas(wardenBaseFrame, wardenAtlasPng);
Console.WriteLine($"OK {wardenAtlasPng}（监守者双帧图集 600x500）");

var wardenAtlas =
"""
warden.png
size:600,500
format:RGBA8888
filter:Linear,Linear
repeat:none
full
bounds:0,0,300,500
full_flash
bounds:300,0,300,500
""";
File.WriteAllText(Path.Combine(wardenDir, "warden.atlas"), wardenAtlas);
Console.WriteLine("OK warden.atlas");

var wardenAnims = new JsonObject
{
    ["idle_loop"] = new JsonObject
    {
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["translate"] = TransFrames((0, 0, 0), (0.9, 0, -10), (1.8, 0, 0)),
                ["rotate"] = RotFrames((0, 0), (0.45, 1), (0.9, 0), (1.35, -1), (1.8, 0))
            }
        }
    },
    ["cast"] = new JsonObject
    {
        // 音波炮/施法：胸口蓄力（前倾）
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["rotate"] = RotFrames((0, 0), (0.25, 4), (0.5, 4), (0.75, 0)),
                ["translate"] = TransFrames((0, 0, 0), (0.25, 14, 5), (0.5, 14, 5), (0.75, 0, 0))
            }
        }
    },
    ["attack"] = new JsonObject
    {
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["rotate"] = RotFrames((0, 0), (0.14, 5), (0.28, -10), (0.44, 3), (0.66, 0)),
                ["translate"] = TransFrames((0, 0, 0), (0.14, 20, 6), (0.28, -50, 4), (0.44, 15, 0), (0.66, 0, 0))
            }
        }
    },
    ["hurt"] = new JsonObject
    {
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["translate"] = TransFrames((0, 0, 0), (0.08, 35, 0), (0.16, -14, 0), (0.26, 18, 0), (0.35, 0, 0)),
                ["rotate"] = RotFrames((0, 0), (0.08, -3), (0.2, 2), (0.35, 0))
            }
        }
    },
    ["die"] = new JsonObject
    {
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["rotate"] = RotFrames((0, 0), (0.6, -85), (0.95, -85)),
                ["translate"] = TransFrames((0, 0, 0), (0.6, 50, -20), (0.95, 55, -22))
            }
        }
    }
};

var wardenSkel = new JsonObject
{
    ["skeleton"] = new JsonObject
    {
        ["hash"] = "mcwarden001",
        ["spine"] = "4.2.43",
        ["x"] = -150, ["y"] = -260, ["width"] = 300, ["height"] = 520,
        ["images"] = "./",
        ["audio"] = "./"
    },
    ["bones"] = new JsonArray
    {
        new JsonObject { ["name"] = "root" }
    },
    ["slots"] = new JsonArray
    {
        new JsonObject { ["name"] = "full", ["bone"] = "root", ["attachment"] = "full", ["color"] = "ffffffff" }
    },
    ["skins"] = new JsonArray
    {
        new JsonObject
        {
            ["name"] = "default",
            ["attachments"] = new JsonObject
            {
                ["full"] = new JsonObject
                {
                    ["full"] = new JsonObject
                    {
                        ["type"] = "region", ["x"] = 0, ["y"] = 250,
                        ["width"] = 300, ["height"] = 500
                    },
                    ["full_flash"] = new JsonObject
                    {
                        ["type"] = "region", ["x"] = 0, ["y"] = 250,
                        ["width"] = 300, ["height"] = 500
                    }
                }
            }
        }
    },
    ["animations"] = wardenAnims
};

File.WriteAllText(Path.Combine(wardenDir, "warden.json"), wardenSkel.ToJsonString(jsonOpts));
Console.WriteLine("OK warden.json");

var wardenTres =
"""
[gd_resource type="SpineSkeletonDataResource" load_steps=3 format=3]

[ext_resource type="SpineAtlasResource" path="res://animations/monsters/warden/warden.atlas" id="1_atlas"]
[ext_resource type="SpineSkeletonFileResource" path="res://animations/monsters/warden/warden.json" id="2_skel"]

[resource]
atlas_res = ExtResource("1_atlas")
skeleton_file_res = ExtResource("2_skel")
default_mix = 0.02
""";
File.WriteAllText(Path.Combine(wardenDir, "warden_skel_data.tres"), wardenTres);
Console.WriteLine("OK warden_skel_data.tres");

// 视觉场景（自动锚点：监守者目标可视高520px——第1幕最终BOSS体量）
var wardenTscn = BuildCreatureTscn(wardenBaseFrame, "warden", 520.0);
File.WriteAllText(@"D:\塔2MOD\MCMonsters\pck_content\scenes\creature_visuals\warden.tscn", wardenTscn);
Console.WriteLine("OK scenes/creature_visuals/warden.tscn");

// ============================ 1k6. 袭击队长（掠夺者放大版，第3幕精英）===========================
// 复用掠夺者的骨架/图集，只生成更大的视觉场景（targetH 300 vs 掠夺者250）
var captainTscn = BuildCreatureTscn(@"D:\塔2MOD\MCMonsters\pck_content\animations\monsters\pillager\pillager_base.png", "raid_captain", 300.0);
File.WriteAllText(@"D:\塔2MOD\MCMonsters\pck_content\scenes\creature_visuals\raid_captain.tscn", captainTscn);
Console.WriteLine("OK scenes/creature_visuals/raid_captain.tscn（袭击队长·掠夺者放大版）");

// 说明：图集页面名统一在 ModPckAssembler.NormalizeAtlasPage 里规范成 "<怪物>.png"，
// 页名保持相对文件名 + spatlas 写入 source_path，Spine 才会走 res:// 的 .ctex import 链，
// 不再需要游戏根目录裸图（旧版在此处把页名改成 mc_textures/ 的做法已废弃）。

// ============================ 4. 组装 mod pck ============================
if (args.Length > 1 || Environment.GetEnvironmentVariable("ARTGEN_PACK") == "1" || packOnly)
{
    string contentDir = Path.Combine(repoRoot, "pck_content");
    string outPck = Path.Combine(repoRoot, "deploy", "MCMonsters", "MCMonsters.pck");
    Directory.CreateDirectory(Path.GetDirectoryName(outPck)!);
    Console.WriteLine("\n=== 组装 MCMonsters.pck ===");
    ModPckAssembler.Assemble(contentDir, outPck);
}














