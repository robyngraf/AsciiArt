using System.Xml.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;
using FullFontEncoder;
using System.Text;


var filename = @"D:\Temp\moon_screen.jpg";
var outputFilename = Path.ChangeExtension(filename, ".txt");

const string invertToken = "​"; // zero-width space

Console.WriteLine("Reading font");

Dictionary<int, Image<Rgba32>> LoadImages()
{
    using var fontImage = Image.Load<Rgba32>(@"apple font_0.png");
    var fontData = XDocument.Load(@"apple font.fnt");
    var images = new Dictionary<int, Image<Rgba32>>();

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
        var rect = new Rectangle(characterX, characterY, width, height);
        var charImage = fontImage.Clone(i => i.Crop(rect));
        images.Add(codepoint, charImage);
    }
    return images;
}

Dictionary<int, Image<Rgba32>> characterImagesByCodepoint = LoadImages();

Console.WriteLine("Indexing font");

List<Rectangle> rects =
    [
        new(0, 0, 7, 8),
        new(2, 2, 3, 4),
        new(4, 4, 3, 4),
        new(0, 4, 3, 4),
        new(4, 0, 3, 4),
        new(0, 0, 3, 4),
    ];

List<bool> GetSignature(ImageFrame<Rgba32> image, Point location)
{
    List<bool> bits = [];

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

        bool isWhite = brightness / count >= 0.5;
        bits.Add(isWhite);
    }

    for (int y = 0; y < 8; y++)
    {
        var y1 = y + location.Y;
        for (int x = 0; x < 7; x++)
        {
            bool isWhite = image[x + location.X, y1].GetLinearBrightness() >= 0.5;
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

Console.WriteLine("Processing image.");

string[] GenerateAsciiFromFrame(ImageFrame<Rgba32> image, Tree<string> index)
{
    var lines = new string[image.Height / 8];
    for (int imageY = 0; imageY < image.Height - 7; imageY += 8)
    {
        var line = new StringBuilder(image.Width / 7);
        for (int imageX = 0; imageX < image.Width - 6; imageX += 7)
        {
            var bits = GetSignature(image, new(imageX, imageY));
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
    string[][] frameLines = new string[sourceImage.Frames.Count][];
    Parallel.ForEach(
        sourceImage.Frames,
        (ImageFrame<Rgba32> sourceImageFrame, ParallelLoopState _, long frameIndex) =>
        {
            var thisFrameLines = GenerateAsciiFromFrame(sourceImageFrame, index);
            frameLines[frameIndex] = thisFrameLines;
        }
    );
    return [.. frameLines.SelectMany(x => x)];
}

File.WriteAllLines(outputFilename, ProcessImage(filename, index));