using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;

// ============================================================================
// ModPckAssembler - 组装 MCMonsters.pck
//
// 关键：完全复刻官方 pck 的「贴图 import 链」，Spine 纹理不再需要游戏根目录裸图。
//
// 官方布局（例 animations/monsters/aeonglass/）：
//   aeonglass.png.import                 ← remap：[remap] path="res://.godot/imported/<名>.png-<hash>.ctex"
//   .godot/imported/<名>.png-<hash>.ctex ← 真实贴图（CompressedTexture2D 缓存，56 字节头 + WebP）
//   .godot/imported/<名>.atlas-<hash>.spatlas
//        ↳ JSON：{"atlas_data":"<atlas文本>","source_path":"res://..../<名>.atlas",
//                  "normal_texture_prefix":"n","specular_texture_prefix":"s"}
//   <名>.atlas.import / <名>.json.import  ← 指向 spathlas / spjson
//
// spine-godot 运行时的解析链（见 spine-godot/SpineAtlasResource.cpp）：
//   load_from_file(.spatlas) → source_path.get_base_dir() 作为 atlas 纹理基准目录
//   → 页面名 "zombie.png" 拼成 "res://animations/monsters/zombie/zombie.png"
//   → fix_path() 判定为 res:// 资源 → ResourceLoader.load() → 命中 .png.import → ctex
// 所以 source_path 缺失时页面会退化为「相对工作目录的裸文件」——旧版本必须把
// 19 张 PNG 丢进游戏根目录的根本原因。本文件补上 source_path 后彻底摆脱散图。
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
        // 图标查找兜底路径 = res://images/powers/<力量ID>.png（AtlasResourceLoader 的官方约定）
        string powersDir = Path.Combine(contentDir, "images/powers");
        if (Directory.Exists(powersDir))
        {
            foreach (var iconFile in Directory.EnumerateFiles(powersDir, "*.png"))
            {
                string name = Path.GetFileNameWithoutExtension(iconFile);
                string iconHash = Md5("images/powers/" + name + ".png:mc");
                byte[] iconCtex = CtexBuilder.Build(iconFile);
                entries.Add(new($"res://.godot/imported/{name}.png-{iconHash}.ctex", iconCtex));
                entries.Add(new($"res://images/powers/{name}.png.import",
                    GodotImporterStub.TextureStub($"res://images/powers/{name}.png", name + ".png", iconHash, out _)));
                Console.WriteLine($"  缓存: images/powers/{name}.png → ctex ({iconCtex.Length} B)");
            }
        }

        foreach (var m in Monsters)
        {
            string mDir = Path.Combine(contentDir, "animations/monsters", m);
            string pngPath = Path.Combine(mDir, m + ".png");
            if (!File.Exists(pngPath))
                throw new FileNotFoundException($"缺少怪物贴图: {pngPath}");

            // ---- 贴图 import 链：源 png → .png.import → .godot/imported/*.ctex ----
            string texHash = Md5("animations/monsters/" + m + "/" + m + ".png:mc");
            byte[] ctex = CtexBuilder.Build(pngPath);
            entries.Add(new($"res://.godot/imported/{m}.png-{texHash}.ctex", ctex));
            entries.Add(new($"res://animations/monsters/{m}/{m}.png.import",
                GodotImporterStub.TextureStub($"res://animations/monsters/{m}/{m}.png", m + ".png", texHash, out _)));
            Console.WriteLine($"  缓存: {m}.png → ctex ({ctex.Length} B, 头 {CtexBuilder.HeaderSize} B)");

            // ---- atlas → spatlas（JSON 包装，必须带 source_path）----
            string atlasPath = Path.Combine(mDir, m + ".atlas");
            if (!File.Exists(atlasPath))
                throw new FileNotFoundException($"缺少 atlas: {atlasPath}");
            string aHash = Md5("animations/monsters/" + m + "/" + m + ".atlas:mc");
            string atlasText = NormalizeAtlasPage(File.ReadAllText(atlasPath), m + ".png");
            var atlasWrapper = new JsonObject
            {
                ["atlas_data"] = atlasText,
                ["source_path"] = $"res://animations/monsters/{m}/{m}.atlas",
                ["normal_texture_prefix"] = "n",
                ["specular_texture_prefix"] = "s"
            };
            byte[] spatlas = System.Text.Encoding.UTF8.GetBytes(atlasWrapper.ToJsonString());
            entries.Add(new($"res://.godot/imported/{m}.atlas-{aHash}.spatlas", spatlas));
            entries.Add(new($"res://animations/monsters/{m}/{m}.atlas.import",
                GodotImporterStub.AtlasStub($"res://animations/monsters/{m}/{m}.atlas", m + ".atlas", aHash, out _)));
            Console.WriteLine($"  缓存: {m}.atlas → spatlas ({spatlas.Length} B, source_path 已写入)");

            // ---- 骨架 json → spjson（原样文本）----
            string jHash = Md5("animations/monsters/" + m + "/" + m + ".json:mc");
            byte[] spjson = File.ReadAllBytes(Path.Combine(mDir, m + ".json"));
            entries.Add(new($"res://.godot/imported/{m}.json-{jHash}.spjson", spjson));
            entries.Add(new($"res://animations/monsters/{m}/{m}.json.import",
                GodotImporterStub.JsonSkelStub($"res://animations/monsters/{m}/{m}.json", m + ".json", jHash, out _)));
            Console.WriteLine($"  缓存: {m}.json → spjson ({spjson.Length} B)");
        }

        PackPck.Pack(outPck, entries);
    }

    /// <summary>
    /// 把 atlas 第一行（页面贴图名）规范成相对文件名，例如 "mc_textures/mc_textures/zombie.png"
    /// → "zombie.png"。残留的绝对/带目录页名会导致纹理按工作目录解析，必须清掉。
    /// </summary>
    static string NormalizeAtlasPage(string atlasText, string pageName)
    {
        string normalized = atlasText.Replace("\r\n", "\n").Replace("\r", "\n");
        var lines = normalized.Split('\n').ToList();
        if (lines.Count == 0 || string.IsNullOrWhiteSpace(lines[0]))
            throw new InvalidDataException($"atlas 缺少页面行: expected {pageName}");
        lines[0] = pageName;
        // 保持文件原有结尾换行习惯
        string result = string.Join("\n", lines);
        if (!normalized.EndsWith("\n") && result.EndsWith("\n"))
            result = result.TrimEnd('\n');
        return result;
    }

    static string Md5(string s) =>
        Convert.ToHexString(MD5.HashData(System.Text.Encoding.UTF8.GetBytes(s))).ToLowerInvariant()[..32];
}
