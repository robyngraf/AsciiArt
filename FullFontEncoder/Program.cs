using FullFontEncoder;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;

bool outlineEdges = false;
bool outputRenderedAsciiArt = true;
bool randomNoise = false;
bool resizeIfTooBig = false;
bool generate = true;
bool limitedCharacterSet = generate & false;
bool useOnlyColourBlocks = true;
bool addExtraColourBlocks = true;
bool generateFontMap = true;

var filename = @"C:\Users\Robyn\Downloads\doom uncolourized.gif";
var outputFilename = Path.ChangeExtension(filename, ".txt");
var encodedFilename = Path.ChangeExtension(filename, ".encoded.txt");

const string invertToken = "​"; // zero-width space

Console.WriteLine("Reading font");

Dictionary<int, Image<Rgba32>> LoadImages()
{
    using var fontImage = Image.Load<Rgba32>(@"apple font_0.png");
    var fontData = XDocument.Load(@"apple font.fnt");
    Dictionary<int, Image<Rgba32>> imagesByCodePoint = [];
    Dictionary<string, Image<Rgba32>> imagesBySignature = [];

    foreach (var node in fontData.Descendants("char"))
    {
#pragma warning disable CS8604 // Possible null reference argument.
        var codepoint = checked((int)node.Attribute("id"));
        var characterX = (int)node.Attribute("x");
        var characterY = (int)node.Attribute("y");
        var width = (int)node.Attribute("width");
        var height = (int)node.Attribute("height");
#pragma warning restore CS8604 // Possible null reference argument.
        if (width != 7 || height != 8) continue;
        var bits = GetSignature(fontImage.Frames[0], new(characterX, characterY));
        var signature = string.Join("", bits.Select(b => b ? "1" : "0"));
        Image<Rgba32> charImage;
        if (imagesBySignature.TryGetValue(signature, out Image<Rgba32>? value))
        {
            charImage = value;
        }
        else
        {
            var rect = new Rectangle(characterX, characterY, width, height);
            charImage = fontImage.Clone(i => i.Crop(rect));
            imagesBySignature[signature] = charImage;
        }
        imagesByCodePoint.Add(codepoint, charImage);
        if (limitedCharacterSet && imagesByCodePoint.Count > 255) break;
    }

    if (addExtraColourBlocks)
    {
        var ethiopicCodepointBlockStart = 0x1200;
        for (int i = 0; i < 32; i++)
        {
            Image<Rgba32> newImage = new(7, 8);
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 7; x++)
                {
                    var j = (x + 3 * y) % 6;
                    bool isWhite = (i & (1 << j)) != 0;
                    newImage[x, y] = isWhite ? new Rgba32(255, 255, 255) : new Rgba32(0, 0, 0);
                }
            }
            var bits = GetSignature(newImage.Frames[0], new(0, 0));
            var signature = string.Join("", bits.Select(b => b ? "1" : "0"));
            Image<Rgba32> charImage;
            if (imagesBySignature.TryGetValue(signature, out Image<Rgba32>? value))
            {
                charImage = value;
            }
            else
            {
                charImage = newImage;
                imagesBySignature[signature] = charImage;
            }
            imagesByCodePoint.Add(ethiopicCodepointBlockStart + i, charImage);
        }
    }
    return imagesByCodePoint;
}

List<Rectangle> rects =
    [
        new(0, 0, 7, 8),
        new(2, 2, 3, 4),
        new(4, 4, 3, 4),
        new(0, 4, 3, 4),
        new(4, 0, 3, 4),
        new(0, 0, 3, 4),
    ];

Dictionary<int, Image<Rgba32>> characterImagesByCodepoint = LoadImages();

Console.WriteLine("Indexing font");

List<bool> GetSignature(ImageFrame<Rgba32> image, Point location, bool addNoise = false)
{
    List<bool> bits = [];
    var r = addNoise ? new Random() : null;

    foreach (var rect in rects)
    {
        var count = rect.Width * rect.Height;
        float brightness = 0;
        for (int y = rect.Top; y < rect.Bottom; y++)
        {
            var y1 = y + location.Y;
            for (int x = rect.Left; x < rect.Right; x++)
            {
                brightness += image[x + location.X, y1].GetLinearBrightness();
            }
        }

        bool isWhite = brightness / count >= (addNoise ? (r!.NextSingle() * 0.8 + 0.1) : 0.5);
        bits.Add(isWhite);
    }
    for (int y = 0; y < 8; y++)
    {
        var y1 = y + location.Y;
        for (int x = 0; x < 7; x++)
        {
            bool isWhite = image[x + location.X, y1].GetLinearBrightness() >= (addNoise ? (r!.NextSingle() * 0.8 + 0.1) : 0.5);
            bits.Add(isWhite);
        }
    }
    return bits;
}

Tree<string> GenerateCharacterIndex(IEnumerable<KeyValuePair<int, Image<Rgba32>>> images)
{
    var tree = new Tree<string>();
    foreach (var pair in images)
    {
        var codepoint = pair.Key;
        if (useOnlyColourBlocks)
        {
            if (codepoint < 0x1200 || codepoint > 0x137F)
            {
                continue;
            }
        }
        var charImage = pair.Value;
        var invertedImage = charImage.Clone(i => i.Invert());
        var bits = GetSignature(charImage.Frames[0], Point.Empty);
        var stringValue = char.ConvertFromUtf32(codepoint);
        tree.AddIfNotPresent(bits, stringValue);
        tree.AddIfNotPresent(bits.Select(b => !b), invertToken + stringValue);
    }
    return tree;
}

var index = GenerateCharacterIndex(characterImagesByCodepoint.OrderBy(p => p.Key));


static void SaveAs1BitPng(Image image, string path)
{
    var enc = new PngEncoder
    {
        CompressionLevel = image.Width * image.Height < 200 ? PngCompressionLevel.NoCompression : PngCompressionLevel.BestCompression,
        ColorType = PngColorType.Grayscale,
        BitDepth = PngBitDepth.Bit1
    };
    image.SaveAsPng(path, enc);
}

if (generateFontMap)
{
    var size = 256;
    var newFontImage = new Image<Rgba32>(size, size);
    var pairs = characterImagesByCodepoint.OrderBy(p => p.Key).ToList();
    Dictionary<Image, List<int>> codepointsByImage = [];
    foreach (var pair in pairs)
    {
        if (!codepointsByImage.TryGetValue(pair.Value, out var codepointList)) codepointsByImage[pair.Value] = codepointList = [];
        codepointList.Add(pair.Key);
    }
    var uniqueCodepoints = codepointsByImage.Select(p => p.Value[0]);
    var images = codepointsByImage.Select(p => p.Key).ToList();
    for (int i = 0; i < images.Count; i++)
    {
        var pos = (i + 1) * 8;
        var x = pos % size;
        var y = pos / size * 8;
        newFontImage.Mutate(c => c.DrawImage(images[i], new Point(x, y), 1f));
    }
    SaveAs1BitPng(newFontImage, $@"F:\Metagame\dev\compressedfont full.png");
    var codePointString = string.Join("", uniqueCodepoints.Prepend(0).Select(char.ConvertFromUtf32));
    codePointString = codePointString.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\0", "\\0");

    StringBuilder sb = new();
    foreach (var list in codepointsByImage.Values)
    {
        if (list.Count < 2) continue;
        var uniqueChar = char.ConvertFromUtf32(list[0]);
        sb.Append(uniqueChar);
        foreach (var nonUniqueChar in list.Skip(1).Select(char.ConvertFromUtf32))
        {
            sb.Append(nonUniqueChar);
        }
    }

    var codePointString2 = sb.ToString().Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\0", "\\0");

    File.WriteAllText($@"F:\Metagame\dev\font codepoints code.txt", "const uniqueCodepointString = \"" + codePointString + "\";\r\n" + "const nonUniqueCodepointString = \"" + codePointString2 + "\";\r\n");
}

string[] GenerateAsciiFromFrame(ImageFrame<Rgba32> image, Tree<string> index)
{
    var lines = new string[image.Height / 8];
    for (int imageY = 0; imageY < image.Height - 7; imageY += 8)
    {
        var line = new StringBuilder(image.Width / 7);
        for (int imageX = 0; imageX < image.Width - 6; imageX += 7)
        {
            var bits = GetSignature(image, new(imageX, imageY), randomNoise);
            var character = index.GetSimilarTo(bits) ?? "?";
            line.Append(character);
        }
        lines[imageY / 8] = line.ToString();
    }
    return lines;
}

string[] ProcessImage(string fileName, Tree<string> index)
{
    using var sourceImage = Image.Load<Rgba32>(filename);
    if (resizeIfTooBig && (sourceImage.Width > 420 || sourceImage.Height > 672))
    {
        var newWidth = Math.Min(sourceImage.Width * 672 / sourceImage.Height, 420);
        var newHeight = Math.Min(sourceImage.Height * 420 / sourceImage.Width, 672);
        Console.WriteLine($"Resizing image from {sourceImage.Width}x{sourceImage.Height} to {newWidth}x{newHeight}.");
        sourceImage.Mutate(i => i.Resize(new ResizeOptions
        {
            Mode = ResizeMode.Stretch,
            Size = new Size(newWidth, newHeight),
            Sampler = KnownResamplers.Hermite
        }));
    }
    if (outlineEdges)
    {
        Console.WriteLine("Outlining edges.");
        var edgeImage = sourceImage.Clone(i => i.DetectEdges());
        sourceImage.Mutate(i => i.DrawImage(edgeImage, new GraphicsOptions
        {
            AlphaCompositionMode = PixelAlphaCompositionMode.SrcOver,
            ColorBlendingMode = PixelColorBlendingMode.Normal,
            BlendPercentage = 1
        }));
    }
    string[][] frameLines = new string[sourceImage.Frames.Count][];
    Parallel.ForEach(
        sourceImage.Frames,
        (ImageFrame<Rgba32> sourceImageFrame, ParallelLoopState _, long frameIndex) =>
        {
            var thisFrameLines = GenerateAsciiFromFrame(sourceImageFrame, index);
            frameLines[frameIndex] = thisFrameLines;
        }
    );

    var width = sourceImage.Width / 7;
    var height = sourceImage.Height / 8;

    var format = Image.DetectFormat(filename);
    if (format is GifFormat)
    {
        var frameDelays = sourceImage.Frames.Select(f => f.Metadata.GetGifMetadata().FrameDelay * 10).ToArray();
        for (int i = 0; i < sourceImage.Frames.Count; i++)
        {
            if (frameDelays[i] > 0)
            {
                frameLines[i] = ["Delay: " + frameDelays[0], .. frameLines[i]];
            }
        }
    }
    return ["Width: " + width, "Height: " + height , ..frameLines.SelectMany(f => f)];
}

string[] lines;
if (generate)
{
    Console.WriteLine("Processing image.");
    lines = ProcessImage(filename, index);

    Console.WriteLine("Writing txt file.");
    File.WriteAllLines(outputFilename, lines);
}
else
{
    lines = File.ReadAllLines(outputFilename);
}

Console.WriteLine("Testing encoding/decoding.");
const string testString = "Hello World";
Console.WriteLine(DecompressBase64GZip(GZipAndBase64Encode(testString)) == testString ? "Test passed." : "Test failed.");
Console.WriteLine(DecompressBase64GZip(GZipAndBase64Encode(testString)));

Console.WriteLine("Encoding file.");
string GZipAndBase64Encode(string s)
{
    // 1. Read the data into a byte array
    var bytes = Encoding.UTF8.GetBytes(s);

    using var outputStream = new MemoryStream();
    // 2. Compress the data using GZip
    using (GZipStream gZipStream = new(outputStream, CompressionMode.Compress))
    {
        gZipStream.Write(bytes, 0, bytes.Length);
    }

    // 3. Convert compressed bytes to Base64 string
    byte[] compressedBytes = outputStream.ToArray();
    return Convert.ToBase64String(compressedBytes);
}

string DecompressBase64GZip(string base64String)
{
    // 1. Decode Base64 to byte array
    byte[] gZipBuffer = Convert.FromBase64String(base64String);

    using var memoryStream = new MemoryStream(gZipBuffer);
    using var gZipStream = new GZipStream(memoryStream, CompressionMode.Decompress);
    using var outputStream = new MemoryStream();
    // 2. Decompress GZip stream into another memory stream
    gZipStream.CopyTo(outputStream);
    byte[] outputBytes = outputStream.ToArray();

    // 3. Convert decompressed bytes back to UTF8 string
    var result = Encoding.UTF8.GetString(outputBytes);
    return result;
}
File.WriteAllText(encodedFilename, GZipAndBase64Encode(string.Join('\n', lines)));


if (outputRenderedAsciiArt)
{
    Console.WriteLine("Writing lines to image file.");
    var outputImageFilename = Path.ChangeExtension(filename, ".ascii" + Path.GetExtension(filename));
    RenderAsciiLinesToImage(lines, characterImagesByCodepoint, outputImageFilename);
}

static void RenderAsciiLinesToImage(string[] lines, Dictionary<int, Image<Rgba32>> characterImagesByCodepoint, string outputImageFilename)
{
    if (lines == null || lines.Length == 0) return;

    const int charWidth = 7;
    const int charHeight = 8;
    int lineCount = lines.Length;

    int GetColumns(string s)
    {
        int cols = 0;
        int i = 0;
        while (i < s.Length)
        {
            Rune.DecodeFromUtf16(s.AsSpan(i), out var rune, out var consumed);
            if (rune.Value == 0x200B) // invert token (zero-width space)
            {
                i += consumed;
                if (i >= s.Length) break;
                Rune.DecodeFromUtf16(s.AsSpan(i), out _, out var consumed2);
                i += consumed2;
                cols++;
            }
            else
            {
                i += consumed;
                cols++;
            }
        }
        return cols;
    }

    int maxCols = 0;
    foreach (var line in lines) maxCols = Math.Max(maxCols, GetColumns(line));
    if (maxCols == 0) maxCols = 1;

    using var outImage = new Image<Rgba32>(maxCols * charWidth, lineCount * charHeight, Color.White);

    for (int row = 0; row < lines.Length; row++)
    {
        var line = lines[row];
        int col = 0;
        int i = 0;
        while (i < line.Length)
        {
            Rune.DecodeFromUtf16(line.AsSpan(i), out var rune, out var consumed);
            bool invert = false;
            if (rune.Value == 0x200B) // invert token
            {
                invert = true;
                i += consumed;
                if (i >= line.Length) break;
                Rune.DecodeFromUtf16(line.AsSpan(i), out rune, out consumed);
            }

            i += consumed;

            // lookup glyph by codepoint
            if (!characterImagesByCodepoint.TryGetValue(rune.Value, out var glyph))
                throw new Exception($"Character {rune} at codepoint {rune.Value} not found");

            bool glyphWasCloned = false;
            if (invert)
            {
                glyph = glyph.Clone();
                glyphWasCloned = true;
                glyph.Mutate(g => g.Invert());
            }

            var destPoint = new Point(col * charWidth, row * charHeight);
            outImage.Mutate(ctx => ctx.DrawImage(glyph, destPoint, 1f));
            if (glyphWasCloned) glyph.Dispose();

            col++;
        }
    }

    outImage.Save(outputImageFilename);
    Console.WriteLine($"Ascii image written to {outputImageFilename}");
}

Console.WriteLine("Done.");