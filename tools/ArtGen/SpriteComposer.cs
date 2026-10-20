// ============================================================================
// SpriteComposer - 把MC皮肤UV展开图拼合成2D正面立绘
// 原理：MC角色 = 6个3D盒子（头8x8x8/身8x12x4/臂x2/腿x2 各4x12x4）
//        正面视图 = 每个盒子取"正面面片"按模型比例拼合
// 布局（1x MC像素）：
//        [头 8x8]          ← head front (8,8)
//     [臂L][身 8x12][臂R]   ← 臂顶与身顶对齐
//     [臂L][身    ][臂R]
//        [腿L 4x12][腿R]   ← 腿在身下
// 总尺寸：16 x 32（1x）
// ============================================================================
public static class SpriteComposer
{
    // MC 64x64 皮肤标准UV（各盒子的正面面片）
    static readonly (int x, int y, int w, int h) HeadF  = (8, 8, 8, 8);
    static readonly (int x, int y, int w, int h) BodyF  = (20, 20, 8, 12);
    static readonly (int x, int y, int w, int h) ArmRF  = (44, 20, 4, 12);
    static readonly (int x, int y, int w, int h) ArmLF  = (36, 52, 4, 12);
    static readonly (int x, int y, int w, int h) LegRF  = (4, 16, 4, 12);
    static readonly (int x, int y, int w, int h) LegLF  = (20, 52, 4, 12);

    public static void Compose(string skinPath, string outPath, int scale = 1)
    {
        using var skin = new System.Drawing.Bitmap(skinPath);

        // 立绘画布（1x = 16x32）
        int W = 16, H = 32;
        using var canvas = new System.Drawing.Bitmap(W, H, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        for (int y = 0; y < H; y++)
        for (int x = 0; x < W; x++)
            canvas.SetPixel(x, y, System.Drawing.Color.FromArgb(0, 0, 0, 0));

        void Blit((int x, int y, int w, int h) uv, int dx, int dy)
        {
            for (int y = 0; y < uv.h; y++)
            for (int x = 0; x < uv.w; x++)
                canvas.SetPixel(dx + x, dy + y, skin.GetPixel(uv.x + x, uv.y + y));
        }

        Blit(HeadF, 4, 0);    // 头：居中(4..12)，顶对齐
        Blit(BodyF, 4, 8);    // 身：紧接头下
        Blit(ArmLF, 0, 8);    // 左臂：身左侧，顶部对齐
        Blit(ArmRF, 12, 8);   // 右臂：身右侧
        Blit(LegLF, 4, 20);   // 左腿：身下左半
        Blit(LegRF, 8, 20);   // 右腿：身下右半

        // 放大输出（Nearest保持像素硬边）
        using var big = new System.Drawing.Bitmap(W * scale, H * scale, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using var g = System.Drawing.Graphics.FromImage(big);
        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
        g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
        g.DrawImage(canvas, new System.Drawing.Rectangle(0, 0, W * scale, H * scale),
                    new System.Drawing.Rectangle(0, 0, W, H), System.Drawing.GraphicsUnit.Pixel);
        big.Save(outPath, System.Drawing.Imaging.ImageFormat.Png);
        System.Console.WriteLine($"OK 立绘已拼合: {outPath} ({W}x{H} @{scale}x = {W * scale}x{H * scale}px)");
    }
}
