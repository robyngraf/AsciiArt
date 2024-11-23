using System.Xml.Linq;
using ConsoleEncoder;
using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Png;
using System.Diagnostics;

var filename = "troll dentist.png";
Console.WriteLine("Hello, World!");
bool generateMaskImages = false;
bool generateMasks = true;
bool generateData = generateMasks || true;
bool useBaseMask = true;

const int accuracy = 8;
const int noMasks = 8;

Debug.Assert(GetScore(20, 0) == 0);
Debug.Assert(GetScore(20, 1) == 1);
Debug.Assert(GetScore(20, 20) == accuracy - 1);
Debug.Assert(GetScore(20, 19) == accuracy - 2);
var a = GetScore(20, 10);
Debug.Assert(GetScore(20, 10) == (accuracy - 1) / 2);

List<Task> tasks = new();

List<IMask>? masks = null;
if (!generateMasks)
{
    masks = File.ReadAllLines($@"D:\Temp\Masks.txt").Select(line =>
    {
        var p = line
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(int.Parse)
        .ToArray();
        return new RectangularMask(p[0], p[1], p[2], p[3]);
    }).ToList<IMask>();
}

if (generateMasks || generateData)
{
    List<Image<Rgba32>>? characterImages = [];
    Dictionary<int, Image<Rgba32>> characterImagesByCodepoint = [];
    Dictionary<int, Image<Rgba32>> characterImagesByCodepointInverted = [];
    {
        using var fontImage = Image.Load<Rgba32>(@"apple font_0.png");
        var fontData = XDocument.Load(@"apple font.fnt");
        var characterHashes = new HashSet<int>();

        foreach (var node in fontData.Descendants("char"))
        {
#pragma warning disable CS8604 // Possible null reference argument.
            var codepoint = checked((int)node.Attribute("id"));
            var characterX = (int)node.Attribute("x");
            var characterY = (int)node.Attribute("y");
            var width = (int)node.Attribute("width");
            var height = (int)node.Attribute("height");
#pragma warning restore CS8604 // Possible null reference argument.
            var rect = new Rectangle(characterX, characterY, width, height);
            var charImage = fontImage.Clone(i => i.Crop(rect));
            var charImageInverted = charImage.Clone(i => i.Invert());
            if (!characterHashes.Contains(charImage.HashPixels()))
            {
                characterHashes.Add(charImage.HashPixels());
                characterHashes.Add(charImageInverted.HashPixels());
                characterImages.Add(charImage);
                characterImagesByCodepoint.Add(codepoint, charImage);
                characterImages.Add(charImageInverted);
                characterImagesByCodepointInverted.Add(codepoint, charImageInverted);
            }
        }
    }

    if (generateMasks)
    {
        var baseMask = new RectangularMask(0, 0, 7, 8);
        var maskOptions = new List<IMask>
    {
        new RectangularMask(0, 0, 7, 8),
        new RectangularMask(0, 0, 5, 5),
        new RectangularMask(2, 0, 5, 5),
        new RectangularMask(0, 3, 5, 5),
        new RectangularMask(2, 3, 5, 5),
        new RectangularMask(1, 2, 5, 4),
        new RectangularMask(0, 2, 2, 4),
        new RectangularMask(5, 2, 2, 4),
        new RectangularMask(2, 0, 3, 3),
        new RectangularMask(2, 5, 3, 3),
        //new CheckerboardMask(7, 8),
        new RectangularMask(0, 0, 3, 3),
        new RectangularMask(4, 0, 3, 3),
        new RectangularMask(0, 5, 3, 3),
        new RectangularMask(4, 5, 3, 3)
    }.OrderByDescending(m => m.Total)
        .ToList();
        if (!useBaseMask)
            maskOptions.RemoveAt(0);

        var lockObject = new object();
        var bestMaskSelection = new List<IMask>();
        var bestSelectionCount = int.MinValue;

        var combinations = 1 << maskOptions.Count;

        Console.WriteLine("Optimising masks");
        Parallel.For(7, combinations, () =>
        {
            // Init thread
            return
            (
                maskSelection: new List<IMask>(noMasks),
                fingerprints: new HashSet<Fingerprint<int>>(1000),
                bestMaskSelection: new List<IMask>(),
                bestSelectionCount: int.MinValue,
                bestCoverage: int.MinValue
            );
        }, (c, state, thread) =>
        {
            // Iterate thread

            {
                // check number of masks in selection
                int maskCount = 0;
                if (useBaseMask)
                    maskCount += 1;
                for (int m = maskOptions.Count - 1; m >= 0; m--)
                {
                    if (((1 << m) & c) == 0)
                        continue;
                    if (maskCount >= noMasks)
                        return thread;
                    maskCount += 1;
                }
                if (maskCount < noMasks)
                    return thread;
            }

            // select masks
            thread.maskSelection.Clear();
            if (useBaseMask)
                thread.maskSelection.Add(baseMask);
            for (int m = 0; m < maskOptions.Count; m++)
                if (((1 << m) & c) != 0)
                    thread.maskSelection.Add(maskOptions[m]);

            if (!useBaseMask)
            {
                // check pixel coverage
                var allPixelsCovered = true;
                for (int y = 0; y < 8; y++)
                    for (int x = 0; x < 7; x++)
                    {
                        var thisPixelCovered = false;
                        for (int i = 0; i < thread.maskSelection.Count; i++)
                        {
                            var mask = thread.maskSelection[i];

                            if (mask.IsInArea(x, y))
                            {
                                thisPixelCovered = true;
                                break;
                            }
                        }
                        if (!thisPixelCovered)
                        {
                            allPixelsCovered = false;
                            goto exitFors;
                        }
                    }
                exitFors:
                if (!allPixelsCovered)
                    return thread;
            }

            thread.fingerprints.Clear();

            foreach (var pair in characterImagesByCodepoint)
            {
                var codepoint = pair.Key;
                var charImage = pair.Value;
                lock (charImage)
                {
                    var weights = new int[noMasks];
                    for (int i = 0; i < noMasks; i++)
                    {
                        var mask = thread.maskSelection[i];

                        var count = 0;

                        for (int y = 0; y < 8; y++)
                            for (int x = 0; x < 7; x++)
                                if (mask.IsInArea(x, y))
                                    if (charImage[x, y].R >= 127)
                                        count++;
                        weights[i] = GetScore(mask.Total, count);
                    }
                    if (weights[0] >= accuracy / 2)
                        for (int i = 0; i < weights.Length; i++)
                            weights[i] = accuracy - 1 - weights[i];
                    var fingerprint = new Fingerprint<int>(weights);

                    thread.fingerprints.Add(fingerprint);
                }
            }
            if (thread.fingerprints.Count == thread.bestSelectionCount)
            {
                var coverage = thread.maskSelection.Sum(m => m.Total);
                if (coverage > thread.bestCoverage)
                {
                    thread.bestMaskSelection = new List<IMask>(thread.maskSelection);
                    thread.bestCoverage = coverage;
                }
            }
            if (thread.fingerprints.Count > thread.bestSelectionCount)
            {
                thread.bestMaskSelection = new List<IMask>(thread.maskSelection);
                thread.bestSelectionCount = thread.fingerprints.Count;
                if (thread.bestSelectionCount == characterImages.Count)
                    state.Break();
            }

            return thread;
        }, threadResult =>
        {
            // Finalise calculation
            lock (lockObject)
            {
                if (bestSelectionCount < threadResult.bestSelectionCount)
                {
                    bestSelectionCount = threadResult.bestSelectionCount;
                    bestMaskSelection = threadResult.bestMaskSelection;
                }
            }
        });
        Console.WriteLine("Optimised masks");

        masks = bestMaskSelection;

        tasks.Add(File.WriteAllLinesAsync($@"D:\Temp\Masks.txt", masks.Cast<RectangularMask>().Select(mask => $"{mask.X},{mask.Y},{mask.Width},{mask.Height}").ToArray()));
    }


    if(generateData)
    {
        Debug.Assert(masks != null);
        Debug.Assert(masks.Count == noMasks);
        var characterImagesByVector = new Dictionary<Fingerprint<int>, List<Image<Rgba32>>>();
        var codepointsByVector = new Dictionary<Fingerprint<int>, List<int>>();

        foreach (var pair in characterImagesByCodepoint.ToList())
        {
            var codepoint = pair.Key;
            var charImage = pair.Value;

            var weights = new int[noMasks];
            for (int i = 0; i < noMasks; i++)
            {
                var mask = masks[i];

                var count = 0;

                for (int y = 0; y < 8; y++)
                    for (int x = 0; x < 7; x++)
                        if (mask.IsInArea(x, y))
                            if (charImage[x, y].GetLinearBrightness() >= 0.5)
                                count++;
                weights[i] = GetScore(mask.Total, count);
            }

            if (weights[0] >= accuracy / 2)
            {
                for (int i = 0; i < weights.Length; i++)
                    weights[i] = accuracy - 1 - weights[i];

                // Flip
                charImage = characterImagesByCodepointInverted[codepoint];
                characterImagesByCodepointInverted[codepoint] = characterImagesByCodepoint[codepoint];
                characterImagesByCodepoint[codepoint] = charImage;
            }

            var fingerprint = new Fingerprint<int>(weights);

            characterImagesByVector.Add(fingerprint, charImage);
            codepointsByVector.Add(fingerprint, codepoint);
        }

        {
            var sb = new StringBuilder();
            foreach (var pair in codepointsByVector.OrderBy(p => p.Key.ToString()))
            {
                var fingerprint = pair.Key;
                var codePoints = pair.Value;

                sb.Append(fingerprint);
                foreach (var codepoint in codePoints)
                    sb.Append(char.ConvertFromUtf32(codepoint));
                sb.AppendLine();
            }
            tasks.Add(File.WriteAllTextAsync($@"D:\Temp\CharWeights.txt", sb.ToString()));
        }

        var tree = new Tree<int, int>();
        {
            // put values in tree
            foreach (var pair in codepointsByVector.OrderBy(p => p.Key.ToString()))
            {
                var fingerprint = pair.Key;
                var codePoints = pair.Value;
                tree[fingerprint.ToArray()] = codePoints[0];
            }

            // trim solitary branches
            tasks.Add(File.WriteAllTextAsync($@"D:\Temp\Tree before.txt", tree.ToString()));
            tree.Trim();

            // pad leaves with self-reference loops
            tree.Cap(Enumerable.Range(0, accuracy));

            // pad tree with nearest-neighbour links
            tree.Pad(Enumerable.Range(0, accuracy), (a, b) => Math.Abs(a - b));

            // print tree
            tasks.Add(File.WriteAllTextAsync($@"D:\Temp\Tree.txt", tree.ToString(i => char.ConvertFromUtf32(i))));
        }

        {
            var nodeIndexes = new Dictionary<Tree<int, int>, int>();
            var nodes = tree.AllNodes().ToArray();
            var rootNode = nodes[0];
            nodes = nodes.OrderByDescending(n => n == rootNode).ThenByDescending(n => n.HasValue).ToArray();
            for (int i = 0; i < nodes.Length; i++)
                nodeIndexes.Add(nodes[i], i);

            {
                var sb = new StringBuilder();
                foreach (var node in nodes)
                {
                    sb.Append($"node {nodeIndexes[node]}");
                    if (node.HasValue)
                        sb.Append($" value {char.ConvertFromUtf32(node.Value)}");
                    foreach (var pair in node.OrderBy(p => p.Key))
                        sb.Append($" {pair.Key} -> {nodeIndexes[pair.Value]}");
                    sb.AppendLine();
                }

                tasks.Add(File.WriteAllTextAsync($@"D:\Temp\Tree compact.txt", sb.ToString()));
            }

            {
                var nodeData = nodes
                    .SelectMany(node => Enumerable.Range(0, accuracy).Select(i => nodeIndexes[node[i]]))
                    .Select(i => checked((ushort)i)).ToArray();

                nodeData.WriteToFile($@"D:\Temp\nodes.data");

                nodeData = nodeData.Select(n => (ushort)(n * accuracy)).ToArray();
                int sauceSize = 128;
                using var secretSauceImage = new Image<Rgba32>(sauceSize, sauceSize);
                for (int i = 0; i < nodeData.Length; i++)
                {
                    uint address = nodeData[i];
                    var x = i % sauceSize;
                    var y = i / sauceSize;
                    uint xNext = checked((byte)(address % sauceSize));
                    uint yNext = checked((byte)(address / sauceSize));
                    var pixel = 0xFF000000 | xNext | (yNext << 8);
                    secretSauceImage[x, y] = new Rgba32(pixel);
                }
                var enc = new PngEncoder
                {
                    BitDepth = PngBitDepth.Bit8,
                    CompressionLevel = PngCompressionLevel.BestCompression,
                    ColorType = PngColorType.Rgb
                };
                tasks.Add(secretSauceImage.SaveAsPngAsync($@"D:\Temp\secretsauce.png", enc));

                bool shrunk = false;
                do
                {
                    shrunk = false;
                    Dictionary<int, ushort> sequenceLocationsByHash = new();
                    for (ushort i = 0; i < nodeData.Length - accuracy + 1; i++)
                    {
                        var h = new HashCode();
                        for (int j = 0; j < accuracy; j++)
                            h.Add(nodeData[i + j]);
                        var hash = h.ToHashCode();
                        if (!sequenceLocationsByHash.TryGetValue(hash, out var firstLocation))
                        {
                            sequenceLocationsByHash[hash] = i;
                            continue;
                        }

                        for (ushort j = 0; j < nodeData.Length; j++)
                        {
                            if (nodeData[j] == i)
                            {
                                nodeData[j] = firstLocation;
                            }
                        }
                    }

                    HashSet<ushort> referredLocations = new(nodeData) { 0 };
                    List<ushort> newNodeData = new();
                    List<ushort> removedNodes = new();
                    for (ushort i = 0; i < nodeData.Length - accuracy + 1; i++)
                    {
                        if (referredLocations.Contains(i))
                        {
                            for (ushort j = i; j < i + accuracy; j++)
                            {
                                newNodeData.Add(nodeData[j]);
                            }
                            i += accuracy - 1;
                        }
                        else
                        {
                            removedNodes.Add(i);
                            shrunk = true;
                        }
                    }
                    if (shrunk)
                    {
                        nodeData = newNodeData
                            .Select(n => n - removedNodes.Where(x => x < n).Count())
                            .Select(n => (ushort)n)
                            .ToArray();
                    }

                } while(shrunk);


                using var secretSauceImage2 = new Image<Rgba32>(sauceSize, sauceSize);
                for (int i = 0; i < nodeData.Length; i++)
                {
                    uint address = nodeData[i];
                    var x = i % sauceSize;
                    var y = i / sauceSize;
                    uint xNext = checked((byte)(address % sauceSize));
                    uint yNext = checked((byte)(address / sauceSize));
                    var pixel = 0xFF000000 | xNext | (yNext << 8);
                    secretSauceImage2[x, y] = new Rgba32(pixel);
                }
                tasks.Add(secretSauceImage2.SaveAsPngAsync($@"D:\Temp\secretsauce2.png", enc));



                nodes
                    .SelectMany(node => Enumerable.Range(0, accuracy)
                    .Select(i => nodeIndexes[node[i]]))
                    .Select(i => checked((ushort)i));

                var codePoints = nodes
                    .TakeWhile((node, i) => i == 0 || node.HasValue)
                    .Select(node => node.Value)
                    .ToList();

                codePoints.WriteToFile($@"D:\Temp\codepoints.data");



                var size = 256;
                var newFontImage = new Image<Rgba32>(size, size);
                {
                    var images = codePoints.Skip(1).Select(codepoint => characterImagesByCodepoint[codepoint]).ToList();
                    for (int i = 0; i < images.Count; i++)
                    {
                        var pos = (i + 1) * 8;
                        var x = pos % size;
                        var y = pos / size * 8;
                        newFontImage.Mutate(c => c.DrawImage(images[i], new Point(x, y), 1f));
                    }
                }
                {
                    var images = codePoints.Skip(1).Select(codepoint => characterImagesByCodepointInverted[codepoint]).ToList();
                    for (int i = 0; i < images.Count; i++)
                    {
                        var pos = (i + 1) * 8;
                        var x = pos % size;
                        var y = pos / size * 8 + size / 2;
                        newFontImage.Mutate(c => c.DrawImage(images[i], new Point(x, y), 1f));
                    }
                }
                tasks.Add(newFontImage.SaveAsync($@"D:\Temp\compressedfont debug.png"));
                tasks.Add(newFontImage.SaveAs1BitPngAsync($@"D:\Temp\compressedfont.png"));
            }
        }
        {
            var nodeIndexes = new Dictionary<Tree<int, int>, int>();
            var nodes = tree.AllNodes().ToArray();
            var rootNode = nodes[0];
            var valueNodes = nodes.Skip(1).Where(n => n.HasValue).ToArray();
            var otherNodes = nodes.Where(n => !n.HasValue).Reverse().ToArray();


            const int sauceSize = 64;
            using var secretSauceImage = new Image<Rgba32>(sauceSize, sauceSize, new Rgba32(0xFF_FFAACC));

            secretSauceImage[0, 0] = new Rgba32(0xFF_FF0000);

            var sequences = new Dictionary<Fingerprint<int>, int>();
            var values = new List<int>();
            values.Add(0xFF0000);

            for (int i = 0; i < valueNodes.Length; i++)
            {
                var location = i + 1;
                nodeIndexes.Add(valueNodes[i], location);
                while (values.Count < location + accuracy)
                    values.Add(0);
                sequences.Add(new Fingerprint<int>(Enumerable.Range(location, accuracy)), 1);
            }
            int shrinkage = 0;
            foreach (var node in otherNodes)
            {
                if (nodeIndexes.ContainsKey(node))
                    continue;
                var location = values.Count;
                List<int> sequence = new();
                for (int i = 0; i < accuracy; i++)
                    sequence.Add(nodeIndexes[node[i]]);
                Debug.Assert(sequence.All(x => x >= 0));
                var fingerprint = new Fingerprint<int>(sequence);
                if (sequences.ContainsKey(fingerprint))
                {
                    shrinkage += accuracy;
                    nodeIndexes[node] = sequences[fingerprint];
                    continue;
                }
                
                for (int i = accuracy; i >= 1; i--)
                {
                    var adjacentSequence = values.GetRange(values.Count - i, i);
                    for (int j = 0; j < i; j++)
                    {
                        adjacentSequence[j] = i - adjacentSequence[j];
                    }
                    if (new Fingerprint<int>(adjacentSequence) == new Fingerprint<int>(sequence.GetRange(0, i)))
                    {
                        values.RemoveRange(values.Count - i, i);
                        location = values.Count;
                        shrinkage += i;
                        break;
                    }
                }
                nodeIndexes[node] = location;
                for (int i = 0; i < accuracy; i++)
                    values.Add(location - sequence[i]);
                for (int i = location - accuracy + 1; i <= location; i++)
                {
                    var adjacentSequence = values.GetRange(i, accuracy);
                    for (int j = 0; j < accuracy; j++)
                        adjacentSequence[j] = i - adjacentSequence[j];
                    var f = new Fingerprint<int>(adjacentSequence);
                    if(!sequences.ContainsKey(f))
                        sequences.Add(f, i);
                }
            }
            values[0] = nodeIndexes[rootNode];
            int highestAddress = int.MinValue;
            for (int i = 0; i < values.Count; i++)
            {
                var address = values[i];
                if(i > 0 && address > highestAddress)
                    highestAddress = address;
                var x = i % sauceSize;
                var y = i / sauceSize;
                uint xNext = checked((byte)(address % 256));
                uint yNext = checked((byte)(address / 256));
                var pixel = 0xFF_000000 | xNext | (yNext << 8);
                secretSauceImage[x, y] = new Rgba32(pixel);
            }
            var enc = new PngEncoder
            {
                BitDepth = PngBitDepth.Bit8,
                CompressionLevel = PngCompressionLevel.BestCompression,
                ColorType = PngColorType.Rgb
            };
            tasks.Add(secretSauceImage.SaveAsPngAsync($@"D:\Temp\secretsauce3.png", enc));

        }
    }

    foreach (var task in tasks)
        await task;
    tasks.Clear();

    foreach (var characterImage in characterImages)
        characterImage.Dispose();
}




if (generateMaskImages)
{
    Debug.Assert(masks != null);
    for (int i = 0; i < noMasks; i++)
    {
        var mask = masks[i];
        using var image = new Image<Rgba32>(7, 8);

        for (int y = 0; y < 8; y++)
            for (int x = 0; x < 7; x++)
                if (mask.IsInArea(x, y))
                    image[x, y] = Color.White;
                else
                    image[x, y] = Color.Black;

        image.Save($@"D:\Temp\mask{i++}.png");
    }
}

{
    Debug.Assert(masks != null);
    Debug.Assert(masks.Count == noMasks);
    var nodes = Helper.ReadUInt16FromFile($@"D:\Temp\nodes.data").ToArray();
    var fontImage = Image.Load<Rgba32>($@"D:\Temp\compressedfont.png");
    var sauce = Image.Load<Rgba32>($@"D:\Temp\secretsauce3.png");
    var sauceSize = sauce.Width;

    var format = Image.DetectFormat(@$"D:\Temp\{filename}");
    using var sourceImage = Image.Load<Rgba32>(@$"D:\Temp\{filename}");
    var numberOfFrames = sourceImage.Frames.Count;
    var width = sourceImage.Width / 7 * 7;
    var height = sourceImage.Height / 8 * 8;
    var newFrames = new Image<Rgba32>[sourceImage.Frames.Count];
    Console.WriteLine("Processing image.");

    int sauceStartIndex = GetNextAddressFromSauce(0);

    Parallel.ForEach(sourceImage.Frames, (ImageFrame<Rgba32> sourceImageFrame, ParallelLoopState _, long frameIndex) =>
    {
        var destinationImage = new Image<Rgba32>(width, height);

        for (int imageY = 0; imageY < height - 7; imageY += 8)
        {
            for (int imageX = 0; imageX < width - 6; imageX += 7)
            {
                var nodeIndex = 0;
                bool flipImage = false;

                var nodeIndex2 = sauceStartIndex;
                for (int i = 0; i < noMasks; i++)
                {
                    var mask = masks[i];
                    float count = 0;

                    for (int y = 0; y < 8; y++)
                        for (int x = 0; x < 7; x++)
                            if (mask.IsInArea(x, y))
                                count += sourceImageFrame[imageX + x, imageY + y].GetLinearBrightness();

                    var weight = GetScore(mask.Total, (int)MathF.Round(count));
                    if (i == 0 && weight >= accuracy - accuracy / 2)
                        flipImage = true;
                    if (flipImage)
                        weight = accuracy - 1 - weight;

                    nodeIndex = nodes[nodeIndex * accuracy + weight];
                    nodeIndex2 = nodeIndex2 - GetNextAddressFromSauce(nodeIndex2 + weight);
                }
                Debug.Assert(nodeIndex == nodeIndex2);
                var destLocation = new Point(imageX, imageY);

                var pos = nodeIndex * 8;
                var sourceX = pos % fontImage.Width;
                var sourceY = pos / fontImage.Width * 8;
                var sourceLocation = new Point(sourceX, sourceY);

                destinationImage.DrawImage7By8(fontImage, destLocation, sourceLocation, flipImage);
            }
        }
        var frameMetadata = destinationImage.Frames.RootFrame.Metadata.GetFormatMetadata(GifFormat.Instance);
        frameMetadata.FrameDelay = sourceImageFrame.Metadata.GetFormatMetadata(GifFormat.Instance).FrameDelay;

        newFrames[frameIndex] = destinationImage;
    });

    Console.WriteLine("Compiling image.");
    using var destinationImage = new Image<Rgba32>(width, height);
    for (int frameIndex = 0; frameIndex < newFrames.Length; frameIndex++)
    {
        var frame = newFrames[frameIndex];
        destinationImage.Frames.AddFrame(frame.Frames.RootFrame);
        if (frameIndex == 0)
            destinationImage.Frames.RemoveFrame(0);
    }
    Console.WriteLine("Saving image.");
    switch (format)
    {
        case GifFormat:
            {
                var imageMetadata = destinationImage.Metadata.GetGifMetadata();
                imageMetadata.ColorTableMode = GifColorTableMode.Global;
                Color[] colorTable = [Color.Black, Color.White];
                imageMetadata.GlobalColorTable = colorTable;
                if (destinationImage.Frames.Count > 1)
                    imageMetadata.RepeatCount = 0;
                tasks.Add(destinationImage.SaveAsGifAsync(@$"D:\Temp\ascii {Path.GetFileName(filename)}"));
                break;
            }
        case PngFormat:
            {
                var imageMetadata = destinationImage.Metadata.GetPngMetadata();
                imageMetadata.ColorType = PngColorType.Grayscale;
                imageMetadata.BitDepth = PngBitDepth.Bit1;
                tasks.Add(destinationImage.SaveAsPngAsync(@$"D:\Temp\ascii {Path.GetFileName(filename)}"));
                break;
            }
        default:
            {
                tasks.Add(destinationImage.SaveAsync(@$"D:\Temp\ascii {Path.GetFileName(filename)}"));
                break;
            }
    }

    foreach (var frame in newFrames)
        frame.Dispose();

    int GetNextAddressFromSauce(int address)
    {
        int x = address % sauceSize;
        int y = address / sauceSize;
        return (int)(0x00_007FFF & sauce[x, y].PackedValue);
    }
}

foreach (var task in tasks)
    await task;

static int GetScore(int maskTotal, float count)
{
    //return (int)MathF.Round(count * (accuracy - 1) / maskTotal);
    ///*
    return (int)((count - 1) * (accuracy - 2) / (maskTotal - 1) + 1);
    //*/
}