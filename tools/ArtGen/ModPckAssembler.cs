using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;

// ============================================================================
// ModPckAssembler - 组装 MCMonsters.pck
// 对每只怪物统一生成：贴图ctex+根级remap / atlas spatlas / 骨架 spjson
// 新增怪物时只需在 Monsters 数组加名字 + pck_content 放对应资源
// ============================================================================
public static class ModPckAssembler
{
    static readonly string[] Monsters = { "zombie", "skeleton", "creeper", "chicken_jockey", "spider", "blaze", "zombie_piglin", "wither_skeleton", "piglin_brute", "ghast", "wither_boss", "enderman", "shulker", "ender_dragon", "giant", "pillager", "witch", "warden", "raid_captain" };

    public static void Assemble(string contentDir, string outPck)
    {
        var entries = new List<PackPck.Entry>();

        // ---- 手工资源（tscn/tres/atlas/json/png/localization）----
        foreach (var file in Directory.EnumerateFiles(contentDir, "*", SearchOption.AllDirectories))
        {
            string rel = Path.GetRelativePath(contentDir, file).Replace('\\', '/');
            if (rel.EndsWith(".json") || rel.EndsWith(".atlas") || rel.EndsWith(".png") ||
                rel.EndsWith(".tres") || rel.EndsWith(".tscn") || rel.EndsWith(".ogg") || rel.EndsWith(".mp3") || rel.EndsWith(".wav") || rel.EndsWith(".tpsheet"))
            {
                entries.Add(new($"res://{rel}", File.ReadAllBytes(file)));
                Console.WriteLine($"  资源: res://{rel}");
            }
        }

        // ---- 自定义力量图标：images/powers/*.png 的完整 import 链 ----
        // 图标查找兜底路径 = res://images/powers/<力量ID>.png（AtlasResourceLoader的官方约定）
        string powersDir = Path.Combine(contentDir, "images/powers");
        if (Directory.Exists(powersDir))
        {
            foreach (var iconFile in Directory.EnumerateFiles(powersDir, "*.png"))
            {
                string name = Path.GetFileNameWithoutExtension(iconFile);
                string iconHash = Md5(name + ".png:mc");
                byte[] iconCtex = CtexBuilder.Build(iconFile);
                entries.Add(new($"res://images/powers/{name}.png-{iconHash}.ctex", iconCtex));
                entries.Add(new($"res://images/powers/{name}.png.import",
                    GodotImporterStub.TextureStub($"res://images/powers/{name}.png", name + ".png", iconHash, out _)));
                Console.WriteLine($"  缓存: images/powers/{name}.png → ctex ({iconCtex.Length} B)");
            }
        }

        foreach (var m in Monsters)
        {
            string mDir = Path.Combine(contentDir, "animations/monsters", m);

            // ---- 贴图双通道（zombie与skeleton完全一致）----
            // 通道1：根级remap → ctex（ResourceLoader路径）
            // 通道2：工作目录裸名文件（FileAccess fallback路径）
            string texHash = Md5(m + ".png:mc");
            byte[] ctex = CtexBuilder.Build(Path.Combine(mDir, m + ".png"));
            entries.Add(new($"res://{m}.png-{texHash}.ctex", ctex));
            entries.Add(new($"res://{m}.png.import",
                GodotImporterStub.TextureStub($"res://{m}.png", m + ".png", texHash, out _)));
            Console.WriteLine($"  缓存: {m}.png → ctex ({ctex.Length} B) [根级remap]");
            entries.Add(new($"res://.godot/imported/{m}.png", File.ReadAllBytes(Path.Combine(mDir, m + ".png"))));

            // ---- atlas → spatlas（JSON包装）----
            string aHash = Md5(m + ".atlas:mc");
            string atlasText = File.ReadAllText(Path.Combine(mDir, m + ".atlas"));
            var atlasWrapper = new JsonObject { ["atlas_data"] = atlasText };
            byte[] spatlas = System.Text.Encoding.UTF8.GetBytes(atlasWrapper.ToJsonString());
            entries.Add(new($"res://.godot/imported/{m}.atlas-{aHash}.spatlas", spatlas));
            entries.Add(new($"res://animations/monsters/{m}/{m}.atlas.import",
                GodotImporterStub.AtlasStub($"res://animations/monsters/{m}/{m}.atlas", m + ".atlas", aHash, out _)));
            Console.WriteLine($"  缓存: {m}.atlas → spatlas ({spatlas.Length} B)");

            // ---- 骨架 json → spjson（原样文本）----
            string jHash = Md5(m + ".json:mc");
            byte[] spjson = File.ReadAllBytes(Path.Combine(mDir, m + ".json"));
            entries.Add(new($"res://.godot/imported/{m}.json-{jHash}.spjson", spjson));
            entries.Add(new($"res://animations/monsters/{m}/{m}.json.import",
                GodotImporterStub.JsonSkelStub($"res://animations/monsters/{m}/{m}.json", m + ".json", jHash, out _)));
            Console.WriteLine($"  缓存: {m}.json → spjson ({spjson.Length} B)");
        }

        // ---- 自定义力量图标：tres直接引用官方power_atlas.png区域，无需额外ctex ----
        // （自定义ctex无法通过Godot的WebP校验，见ArtGen力量图标段注释）

        PackPck.Pack(outPck, entries);
    }

    static string Md5(string s) =>
        Convert.ToHexString(MD5.HashData(System.Text.Encoding.UTF8.GetBytes(s))).ToLowerInvariant()[..32];
}
















