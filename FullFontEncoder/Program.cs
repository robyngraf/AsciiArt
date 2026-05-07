using System.Xml.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Png;
using FullFontEncoder;
using System.Collections;

const string invertToken = "​"; // zero-width space

Console.WriteLine("Reading font");

Dictionary<int, Image<Rgba32>> characterImagesByCodepoint = [];

{
    using var fontImage = Image.Load<Rgba32>(@"apple font_0.png");
    var fontData = XDocument.Load(@"apple font.fnt");

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
        characterImagesByCodepoint.Add(codepoint, charImage);
    }
}

Console.WriteLine("Indexing font");

Tree<string> tree = new();

void AddImagesToTree(IEnumerable<KeyValuePair<int, Image<Rgba32>>> images)
{
    foreach (var pair in images)
    {
        var codepoint = pair.Key;
        var charImage = pair.Value;
        var invertedImage = charImage.Clone(i => i.Invert());
        List<bool> bits = [];

        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 7; x++)
            {
                bool isWhite = charImage[x, y].GetLinearBrightness() >= 0.5;
                bits.Add(isWhite);
            }
        }
        var stringValue = char.ConvertFromUtf32(codepoint);
        tree.AddIfNotPresent(bits, stringValue);
        tree.AddIfNotPresent(bits.Select(b => !b), invertToken + stringValue);
    }
}

AddImagesToTree(characterImagesByCodepoint.OrderBy(p => p.Key));

var fred = "fred";