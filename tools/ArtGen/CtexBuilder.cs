using ImageMagick;

// ============================================================================
// CtexBuilder - 构造 Godot CompressedTexture2D (.ctex) 无损WebP缓存
//
// 头部布局逐字节对齐游戏官方 ctex（例：animations/monsters/aeonglass/
// aeonglass.png-*.ctex，官方文件共 56 字节头 + RIFF/WEBP 数据）：
//   0x00 "GST2"           magic
//   0x04 u32=1            ctex 格式版本
//   0x08 u32              width
//   0x0C u32              height
//   0x10 u32=0x0D000000   flags
//   0x14 u32=0xFFFFFFFF   mipmap_limit
//   0x18 u32 x3           保留
//   0x24 u32=2            storage mode
//   0x28 u16              width（再次）
//   0x2A u16              height（再次）
//   0x2C u32=0            flags（再次）
//   0x30 u32=5            image format（5 = lossless WebP）
//   0x34 u32              payload size
//   0x38 ...              RIFF/WEBP payload
//
// 旧实现多写了一个 u32 再写 w/h，导致 payload 偏移 4 字节，
// 引擎按 WebP 解析时读到 "FF 52 49 46 46" → r[0]!='R' 报错，贴图加载失败。
// ============================================================================
public static class CtexBuilder
{
    /// <summary>官方 .ctex 固定头部长度（不含 payload）。</summary>
    public const int HeaderSize = 56;

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
        if (w <= 0 || h <= 0)
            throw new InvalidDataException($"贴图尺寸非法: {pngPath} {w}x{h}");
        if (w > ushort.MaxValue || h > ushort.MaxValue)
            throw new InvalidDataException($"贴图尺寸超出 ctex u16 字段范围: {pngPath} {w}x{h}");

        // WebP 必须是 RIFF....WEBP 容器，否则引擎会判为脏数据
        if (webp.Length < 16 || webp[0] != (byte)'R' || webp[1] != (byte)'I' ||
            webp[2] != (byte)'F' || webp[3] != (byte)'F' ||
            webp[8] != (byte)'W' || webp[9] != (byte)'E' || webp[10] != (byte)'B' || webp[11] != (byte)'P')
            throw new InvalidDataException($"WebP 编码结果无效（不是 RIFF/WEBP 容器）: {pngPath}");

        using var outMs = new MemoryStream(HeaderSize + webp.Length);
        using var bw = new BinaryWriter(outMs);

        bw.Write("GST2"u8);            // 0x00 magic
        bw.Write((uint)1);             // 0x04 格式版本
        bw.Write((uint)w);             // 0x08 width
        bw.Write((uint)h);             // 0x0C height
        bw.Write(0x0D000000u);         // 0x10 flags（与官方样本一致）
        bw.Write(0xFFFFFFFFu);         // 0x14 mipmap_limit（与官方样本一致）
        bw.Write((uint)0);             // 0x18 保留
        bw.Write((uint)0);             // 0x1C 保留
        bw.Write((uint)0);             // 0x20 保留
        bw.Write((uint)2);             // 0x24 storage mode（与官方样本一致）
        bw.Write((ushort)w);           // 0x28 width
        bw.Write((ushort)h);           // 0x2A height
        bw.Write((uint)0);             // 0x2C flags
        bw.Write((uint)5);             // 0x30 image format = lossless WebP
        bw.Write((uint)webp.Length);   // 0x34 payload size
        bw.Write(webp);                // 0x38 RIFF....WebP

        bw.Flush();
        byte[] ctex = outMs.ToArray();
        if (ctex.Length != HeaderSize + webp.Length)
            throw new InvalidDataException("ctex 头部长度异常");
        return ctex;
    }
}
