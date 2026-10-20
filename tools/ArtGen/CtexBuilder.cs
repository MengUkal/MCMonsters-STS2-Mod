using ImageMagick;

// ============================================================================
// CtexBuilder - 构造 Godot CompressedTexture2D (.ctex) 无损WebP缓存
// 头部布局逆向自游戏官方 cultists.png-xxx.ctex (lossless WebP):
//   "GST2" | ver=1(u32) | w(u32) | h(u32) | flags=0x0D000000(u32) | 0xFFFFFFFF
//   | 0(u32)x3 | mode=2(u32) | 0(u32) | w(u16) | h(u16) | 0(u32)
//   | fmt=5(u32) | dataSize(u32) | WebP(RIFF...)数据
// ============================================================================
public static class CtexBuilder
{
    public static byte[] Build(string pngPath)
    {
        using var image = new MagickImage(pngPath);
        image.Format = MagickFormat.WebP;
        image.Settings.SetDefine(MagickFormat.WebP, "lossless", "true");
        image.Quality = 100;
        byte[] webp;
        using (var ms = new MemoryStream())
        {
            image.Write(ms);
            webp = ms.ToArray();
        }

        int w = (int)image.Width, h = (int)image.Height;

        using var outMs = new MemoryStream();
        using var bw = new BinaryWriter(outMs);

        bw.Write("GST2"u8);            // magic
        bw.Write((uint)1);             // version
        bw.Write((uint)w);             // width
        bw.Write((uint)h);             // height
        bw.Write(0x0D000000u);         // flags (mipmaps|...)
        bw.Write(0xFFFFFFFFu);         // dummy
        bw.Write((uint)0);             // reserved
        bw.Write((uint)0);             // reserved
        bw.Write((uint)0);             // reserved
        bw.Write((uint)2);             // compress/storage mode (与官方样本一致)
        bw.Write((uint)0);             // priv
        bw.Write((ushort)w);           // width repeat
        bw.Write((ushort)h);           // height repeat
        bw.Write((uint)0);             // flags repeat
        bw.Write((uint)5);             // format id (lossless webp, 与官方样本一致)
        bw.Write((uint)webp.Length);   // data size
        bw.Write(webp);                // RIFF....WebP
        bw.Flush();
        return outMs.ToArray();
    }
}
