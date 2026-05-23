using SkiaSharp;
using Svg.Skia;

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

static byte[] RenderSvgFrame(string svgPath, int size)
{
    var svg = new SKSvg();
    if (svg.Load(svgPath) is null || svg.Picture is null)
        throw new InvalidOperationException($"Failed to load SVG: {svgPath}");

    var bounds = svg.Picture.CullRect;
    if (bounds.Width <= 0 || bounds.Height <= 0)
        throw new InvalidOperationException("SVG has invalid bounds.");

    var scale = Math.Min(size / bounds.Width, size / bounds.Height);
    var dx = (size - bounds.Width * scale) / 2f - bounds.Left * scale;
    var dy = (size - bounds.Height * scale) / 2f - bounds.Top * scale;

    var info = new SKImageInfo(size, size, SKColorType.Rgba8888, SKAlphaType.Premul);
    using var surface = SKSurface.Create(info);
    var canvas = surface.Canvas;
    canvas.Clear(SKColors.Transparent);
    canvas.Save();
    canvas.Translate(dx, dy);
    canvas.Scale(scale);
    canvas.DrawPicture(svg.Picture);
    canvas.Restore();

    using var image = surface.Snapshot();
    using var data = image.Encode(SKEncodedImageFormat.Png, 100);
    return data.ToArray();
}

var repoRoot = FindRepoRoot();
var svgPath = Path.Combine(repoRoot, "LeadGen", "Assets", "app-icon.svg");
var icoPath = Path.Combine(repoRoot, "LeadGen", "Assets", "app.ico");

if (!File.Exists(svgPath))
{
    Console.Error.WriteLine($"SVG not found: {svgPath}");
    return 1;
}

var sizes = new[] { 16, 32, 48, 256 };
var frames = sizes.Select(s => RenderSvgFrame(svgPath, s)).ToList();

using var ms = new MemoryStream();
using var writer = new BinaryWriter(ms);

writer.Write((short)0);
writer.Write((short)1);
writer.Write((short)frames.Count);

var offset = 6 + frames.Count * 16;
foreach (var (frame, size) in frames.Zip(sizes))
{
    writer.Write((byte)(size >= 256 ? 0 : size));
    writer.Write((byte)(size >= 256 ? 0 : size));
    writer.Write((byte)0);
    writer.Write((byte)0);
    writer.Write((short)1);
    writer.Write((short)32);
    writer.Write(frame.Length);
    writer.Write(offset);
    offset += frame.Length;
}

foreach (var frame in frames)
    writer.Write(frame);

await File.WriteAllBytesAsync(icoPath, ms.ToArray());
Console.WriteLine($"Wrote {icoPath} from SVG ({frames.Count} sizes, square, transparent)");
return 0;
