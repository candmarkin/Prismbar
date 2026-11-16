using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

// Renders the Prismbar logo into a multi-size PNG-compressed .ico file.
internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        string dest = args.Length > 0 ? args[0] : "Prismbar.ico";
        int[] sizes = [16, 24, 32, 48, 64, 128, 256];
        var pngs = sizes.Select(Render).ToList();

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(dest))!);
        using var fs = File.Create(dest);
        using var w = new BinaryWriter(fs);
        w.Write((ushort)0); w.Write((ushort)1); w.Write((ushort)sizes.Length);
        int offset = 6 + 16 * sizes.Length;
        for (int i = 0; i < sizes.Length; i++)
        {
            byte d = sizes[i] >= 256 ? (byte)0 : (byte)sizes[i];
            w.Write(d); w.Write(d); w.Write((byte)0); w.Write((byte)0);
            w.Write((ushort)1); w.Write((ushort)32);
            w.Write(pngs[i].Length); w.Write(offset);
            offset += pngs[i].Length;
        }
        foreach (var p in pngs) w.Write(p);
        Console.WriteLine($"Wrote {dest}");
        return 0;
    }

    private static byte[] Render(int size)
    {
        var fill = new LinearGradientBrush(Color.FromRgb(0x7C, 0x5C, 0xFF), Color.FromRgb(0x00, 0xD4, 0xFF), new Point(0, 0), new Point(1, 1));
        var group = new DrawingGroup();
        group.Children.Add(new GeometryDrawing(fill, null, Geometry.Parse("M 32,6 L 58,54 L 6,54 Z")));
        group.Children.Add(new GeometryDrawing(null, new Pen(new SolidColorBrush(Color.FromArgb(0x80, 0xFF, 0xFF, 0xFF)), 2), Geometry.Parse("M 32,6 L 32,54")));
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            dc.PushTransform(new ScaleTransform(size / 64.0, size / 64.0));
            dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, 64, 64));
            dc.DrawDrawing(group);
        }
        var bmp = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
        bmp.Render(visual);
        var enc = new PngBitmapEncoder();
        enc.Frames.Add(BitmapFrame.Create(bmp));
        using var ms = new MemoryStream();
        enc.Save(ms);
        return ms.ToArray();
    }
}
