// ============================================================================
// GodotImporterStub - 生成 .import 存根（remap文本，指向我们自造的缓存路径）
// 模板逆向自官方 cultists.{skel,atlas,png}.import
// ============================================================================
public static class GodotImporterStub
{
    static string Uid() => "uid://" + Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(13)).ToLowerInvariant()[..13];

    public static byte[] SkelStub(string srcResPath, string cacheBasename, string hash32, out string cachePath)
    {
        cachePath = $"res://.godot/imported/{cacheBasename}-{hash32}.spskel";
        var text = $"[remap]\n\nimporter=\"spine.skel\"\ntype=\"SpineSkeletonFileResource\"\nuid=\"{Uid()}\"\npath=\"{cachePath}\"\n";
        return System.Text.Encoding.UTF8.GetBytes(text);
    }

    public static byte[] JsonSkelStub(string srcResPath, string cacheBasename, string hash32, out string cachePath)
    {
        cachePath = $"res://.godot/imported/{cacheBasename}-{hash32}.spjson";
        var text = $"[remap]\n\nimporter=\"spine.json\"\ntype=\"SpineSkeletonFileResource\"\nuid=\"{Uid()}\"\npath=\"{cachePath}\"\n";
        return System.Text.Encoding.UTF8.GetBytes(text);
    }

    public static byte[] AtlasStub(string srcResPath, string cacheBasename, string hash32, out string cachePath)
    {
        cachePath = $"res://.godot/imported/{cacheBasename}-{hash32}.spatlas";
        var text = $"[remap]\n\nimporter=\"spine.atlas\"\ntype=\"SpineAtlasResource\"\nuid=\"{Uid()}\"\npath=\"{cachePath}\"\n";
        return System.Text.Encoding.UTF8.GetBytes(text);
    }

    public static byte[] TextureStub(string srcResPath, string cacheBasename, string hash32, out string cachePath)
    {
        cachePath = $"res://.godot/imported/{cacheBasename}-{hash32}.ctex";
        var text = $"[remap]\n\nimporter=\"texture\"\ntype=\"CompressedTexture2D\"\nuid=\"{Uid()}\"\npath=\"{cachePath}\"\nmetadata={{\n\"vram_texture\": false\n}}\n";
        return System.Text.Encoding.UTF8.GetBytes(text);
    }
}
