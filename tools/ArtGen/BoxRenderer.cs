// ============================================================================
// BoxRenderer v2 - MC盒子模型等距渲染器（带光影+镂空暗底）
// - 面着色：顶面1.12x / 正面1.0x / 侧面0.68x（模拟左上光源）
// - 镂空处理：贴图透明像素填充暗底色（骨骼间隙=暗色实心，与官方渲染一致）
// - 画面朝左的怪物：正面朝左下，侧面朝右后
// ============================================================================
public static class BoxRenderer
{
    const double KX = 0.35;   // 侧向偏移系数（与骷髅立绘的侧面比例一致，过大像兜帽）
    const double KY = 0.18;   // 侧向抬升系数

    public sealed class Box
    {
        public int Cx, BottomY, W, H, D;
        public (int x, int y) Front, Right, Top;
        public bool Mirror;
        public bool SimpleStick;   // 细杆模式：只画正面（用于骷髅等2px细手臂/腿）
        public Box(int cx, int bottomY, int w, int h, int d,
                   (int, int) front, (int, int) right, (int, int) top, bool mirror = false, bool simpleStick = false)
        { Cx = cx; BottomY = bottomY; W = w; H = h; D = d; Front = front; Right = right; Top = top; Mirror = mirror; SimpleStick = simpleStick; }
    }

    // 颜色乘法着色
    static System.Drawing.Color Tint(System.Drawing.Color c, double f) =>
        System.Drawing.Color.FromArgb(c.A,
            (int)Math.Min(255, c.R * f), (int)Math.Min(255, c.G * f), (int)Math.Min(255, c.B * f));

    // 把UV面片画到目标平行四边形（逐像素扫描，支持着色+镂空暗底）
    static void DrawFaceMapped(System.Drawing.Graphics g, System.Drawing.Bitmap tex,
                               int ux, int uy, int uw, int uh,
                               System.Drawing.Point p0, System.Drawing.Point p1, System.Drawing.Point p3,
                               double brightness, System.Drawing.Color shadowBase, bool mirror)
    {
        // p0=左上 p1=右上 p3=左下（仿射基）
        // 逐像素映射：face(x,y) → p0 + x/uw*(p1-p0) + y/uh*(p3-p0)
        using var face = tex.Clone(new System.Drawing.Rectangle(ux, uy, uw, uh), tex.PixelFormat);

        // 逐像素贴图（只画非透明像素——镂空处保持透明，与原版一致）
        for (int y = 0; y < uh; y++)
        for (int x = 0; x < uw; x++)
        {
            var c = mirror ? face.GetPixel(uw - 1 - x, y) : face.GetPixel(x, y);
            if (c.A < 40) continue;
            var tinted = Tint(c, brightness);
            int dx = p0.X + (int)Math.Round((double)x / uw * (p1.X - p0.X) + (double)y / uh * (p3.X - p0.X));
            int dy = p0.Y + (int)Math.Round((double)x / uw * (p1.Y - p0.Y) + (double)y / uh * (p3.Y - p0.Y));
            int bw = Math.Max(1, Math.Abs(p1.X - p0.X) / uw);
            int bh = Math.Max(1, Math.Abs(p3.Y - p0.Y) / uh);
            g.FillRectangle(new System.Drawing.SolidBrush(tinted), dx, dy, bw + 1, bh + 1);
        }
    }

    public static System.Drawing.Bitmap Render(string texturePath, Box[] boxes, int canvasW, int canvasH, int ox, int oy, int scale)
    {
        using var tex = new System.Drawing.Bitmap(texturePath);
        var canvas = new System.Drawing.Bitmap(canvasW, canvasH, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using var g = System.Drawing.Graphics.FromImage(canvas);
        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
        g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;

        var shadow = System.Drawing.Color.FromArgb(0x33, 0x33, 0x3A);   // 骨骼间隙暗底

        foreach (var b in boxes)
        {
            int px = (int)((ox + b.Cx) * scale);
            int py = (int)(b.BottomY * scale);
            int w = b.W * scale, h = b.H * scale, dd = (int)(b.D * scale);
            // Mirror盒子（画面左侧的肢体）：侧向偏移翻转，保证与右侧肢体对称
            int sgn = b.Mirror ? -1 : 1;
            int sx = (int)(dd * KX) * sgn, sy = (int)(dd * KY);

            // ---- 细杆模式：只画正面细条（骷髅的2px细臂/腿）----
            if (b.SimpleStick)
            {
                var stickPts = new[]
                {
                    new System.Drawing.Point(px - w / 2, py - h),
                    new System.Drawing.Point(px + w / 2, py - h),
                    new System.Drawing.Point(px - w / 2, py)
                };
                DrawFaceMapped(g, tex, b.Front.x, b.Front.y, b.W, b.H,
                    stickPts[0], stickPts[1], stickPts[2], b.Mirror ? 0.88 : 1.0, shadow, b.Mirror);
                continue;
            }

            // ---- 顶面（最先画，被其他面遮）----
            var topPts = new[]
            {
                new System.Drawing.Point(px - w / 2, py - h),
                new System.Drawing.Point(px + w / 2, py - h),
                new System.Drawing.Point(px - w / 2 + sx, py - h - sy)
            };
            // top面片：右向面在左上（布局：top在texOffs+(d,0)宽w）
            DrawFaceMapped(g, tex, b.Top.x, b.Top.y, b.W, b.D,
                topPts[0], topPts[1], topPts[2], 1.18, shadow, false);

            // ---- 侧面（右后侧，受光比正面亮，贴近原版渲染：左前暗、右后亮）----
            var sidePts = new[]
            {
                new System.Drawing.Point(px + w / 2, py - h),
                new System.Drawing.Point(px + w / 2 + sx, py - h - sy),
                new System.Drawing.Point(px + w / 2, py)
            };
            // side面片：组装时传入的Right坐标已是面片左上角
            DrawFaceMapped(g, tex, b.Right.x, b.Right.y, b.D, b.H,
                sidePts[0], sidePts[1], sidePts[2], 1.0, shadow, b.Mirror);

            // ---- 正面（最后画，最暗——原版渲染中朝向观众的左前面处于阴影）----
            var frontPts = new[]
            {
                new System.Drawing.Point(px - w / 2, py - h),
                new System.Drawing.Point(px + w / 2, py - h),
                new System.Drawing.Point(px - w / 2, py)
            };
            // front面片：在texOffs+(d, d)（top行之后）……MC布局：front在(d+w? , d)。
            // 标准布局行2：[right d][front w][left d][back d]，y从 d 开始
            DrawFaceMapped(g, tex, b.Front.x, b.Front.y, b.W, b.H,
                frontPts[0], frontPts[1], frontPts[2], 0.82, shadow, false);
        }
        return canvas;
    }
}
