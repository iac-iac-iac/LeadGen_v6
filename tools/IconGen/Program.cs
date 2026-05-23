using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

static string FindRepoRoot()
{
    var dir = AppContext.BaseDirectory;
    while (!string.IsNullOrEmpty(dir))
    {
        if (File.Exists(Path.Combine(dir, "LeadGen.sln")))
            return dir;
        dir = Directory.GetParent(dir)?.FullName ?? string.Empty;
    }

    throw new InvalidOperationException("LeadGen.sln not found.");
}

static void RemoveWhiteBackground(Image<Rgba32> image)
{
    image.ProcessPixelRows(accessor =>
    {
        for (var y = 0; y < accessor.Height; y++)
        {
            var row = accessor.GetRowSpan(y);
            for (var x = 0; x < row.Length; x++)
            {
                ref var px = ref row[x];
                if (px.A > 0 && px.R >= 245 && px.G >= 245 && px.B >= 245)
                    px.A = 0;
            }
        }
    });
}

var repoRoot = FindRepoRoot();
var pngPath = Path.Combine(repoRoot, "LeadGen", "Assets", "app-icon-512.png");
var icoPath = Path.Combine(repoRoot, "LeadGen", "Assets", "app.ico");

if (!File.Exists(pngPath))
{
    Console.Error.WriteLine($"PNG not found: {pngPath}");
    return 1;
}

using var source = Image.Load<Rgba32>(pngPath);
RemoveWhiteBackground(source);

// ICO: несколько размеров в одном файле (ручная сборка через PNG-кадры)
using var ms = new MemoryStream();
using var writer = new BinaryWriter(ms);

var sizes = new[] { 16, 32, 48, 256 };
var frames = new List<byte[]>();

foreach (var size in sizes)
{
    using var frame = source.Clone(ctx => ctx.Resize(size, size));
    using var frameMs = new MemoryStream();
    frame.Save(frameMs, new PngEncoder { ColorType = PngColorType.RgbWithAlpha });
    frames.Add(frameMs.ToArray());
}

// ICONDIR
writer.Write((short)0); // reserved
writer.Write((short)1); // type = icon
writer.Write((short)frames.Count);

var offset = 6 + frames.Count * 16;
foreach (var (frame, size) in frames.Zip(sizes))
{
    writer.Write((byte)(size >= 256 ? 0 : size)); // width (0 = 256)
    writer.Write((byte)(size >= 256 ? 0 : size)); // height
    writer.Write((byte)0); // colors
    writer.Write((byte)0); // reserved
    writer.Write((short)1); // planes
    writer.Write((short)32); // bpp
    writer.Write(frame.Length);
    writer.Write(offset);
    offset += frame.Length;
}

foreach (var frame in frames)
    writer.Write(frame);

await File.WriteAllBytesAsync(icoPath, ms.ToArray());
Console.WriteLine($"Wrote {icoPath} ({frames.Count} sizes, transparent)");
return 0;
