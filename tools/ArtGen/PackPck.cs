using System.Security.Cryptography;

// ============================================================================
// PackPck - Godot PCK v3 打包器（格式逆向自 SlayTheSpire2.pck）
// 布局: 头部(112字节, fileBase=0x70) + 文件数据区 + 目录索引(文件尾)
// ============================================================================
public static class PackPck
{
    public sealed record Entry(string ResPath, byte[] Data);

    public static void Pack(string outputPath, IEnumerable<Entry> entriesIn)
    {
        // Godot的PCK读取器按路径二分查找目录条目——必须排序，否则部分文件随机加载失败！
        var entries = entriesIn.OrderBy(e => e.ResPath, StringComparer.Ordinal).ToList();
        using var fs = new FileStream(outputPath, FileMode.Create);
        using var bw = new BinaryWriter(fs);

        // ---- 头部 ----
        bw.Write(0x43504447u);          // "GDPC"
        bw.Write(3u);                   // pack format v3
        bw.Write(4u);                   // godot major
        bw.Write(5u);                   // godot minor
        bw.Write(1u);                   // godot patch
        bw.Write(2u);                   // flags = PACK_REL_FILEBASE
        long fileBasePos = bw.BaseStream.Position;
        bw.Write(0ul);                  // file_base 占位
        bw.Write(0ul);                  // directory_offset 占位（回填）
        bw.Write(new byte[72]);         // 保留区（0x28..0x6F）
        long fileBase = bw.BaseStream.Position;  // = 112 = 0x70
        bw.Seek(0x18, SeekOrigin.Begin);
        bw.Write((ulong)fileBase);      // 回填 file_base 到头部偏移0x18
        bw.Seek((int)fileBase, SeekOrigin.Begin);  // 回到数据区起点

        // ---- 文件数据区 ----
        var dirEntries = new List<(string path, ulong offset, uint size, byte[] md5)>();
        foreach (var e in entries)
        {
            long offset = bw.BaseStream.Position;
            bw.Write(e.Data);
            // 4字节对齐填充
            int pad = (int)(4 - (bw.BaseStream.Position % 4)) % 4;
            if (pad > 0) bw.Write(new byte[pad]);
            byte[] md5 = MD5.HashData(e.Data);
            dirEntries.Add((e.ResPath, (ulong)(offset - fileBase), (uint)e.Data.Length, md5));
        }

        // ---- 目录索引（文件尾）----
        long dirOffset = bw.BaseStream.Position;
        bw.Write((uint)dirEntries.Count);
        foreach (var (path, offset, size, md5) in dirEntries)
        {
            byte[] pathBytes = System.Text.Encoding.UTF8.GetBytes(path);
            int paddedLen = (pathBytes.Length + 3) & ~3;
            bw.Write((uint)paddedLen);
            bw.Write(pathBytes);
            for (int i = pathBytes.Length; i < paddedLen; i++) bw.Write((byte)0);
            bw.Write(offset);
            bw.Write((ulong)size);
            bw.Write(md5);
            bw.Write(0u);               // entry flags
        }

        // 回填 directory_offset（头部偏移0x20）
        bw.Seek(0x20, SeekOrigin.Begin);
        bw.Write((ulong)dirOffset);

        bw.Flush();
        Console.WriteLine($"打包完成: {outputPath} ({fs.Length:N0} 字节, {dirEntries.Count} 文件)");
        foreach (var (path, offset, size, _) in dirEntries)
            Console.WriteLine($"  {path} ({size} B)");
    }
}
