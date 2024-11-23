using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Drawing;

namespace ConsoleEncoder
{
    public static class Helper
    {
        public static Tout GetOrNew<Tin, Tout>(this Dictionary<Tin, Tout> dictionary, Tin key) where Tin : notnull where Tout : new()
        {
            if (!dictionary.TryGetValue(key, out var result))
                dictionary[key] = result = new Tout();
            return result;
        }
        public static void Add<Tin, Tout, TList>(this Dictionary<Tin, TList> dictionary, Tin key, Tout value) where Tin : notnull where TList : ICollection<Tout>, new()
        {
            dictionary.GetOrNew(key).Add(value);
        }

        public static void AppendLine(this StringBuilder sb, int depth, string s)
        {
            sb.Append(' ', depth * 2);
            sb.AppendLine(s);
        }

        public static int HashPixels(this Image<Rgba32> image)
        {
            int width = image.Width;
            int height = image.Height;
            var hash = new HashCode();
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    hash.Add(image[x, y].PackedValue);
            return hash.ToHashCode();
        }

        public static void WriteToFile(this IEnumerable<int> values, string path)
        {
            using var stream = File.Create(path);
            using BinaryWriter bw = new(stream);
            foreach (var number in values)
                bw.Write(number);
        }
        public static void WriteToFile(this IEnumerable<ushort> values, string path)
        {
            using var stream = File.Create(path);
            using BinaryWriter bw = new(stream);
            foreach (var number in values)
                bw.Write(number);
        }

        public static IEnumerable<int> ReadUInt16FromFile(string path)
        {
            using var stream = File.OpenRead(path);
            using BinaryReader br = new(stream);
            var fileLength = stream.Length;
            while (stream.Position < fileLength)
                yield return br.ReadUInt16();
        }
        public static IEnumerable<int> ReadIntFromFile(string path)
        {
            using var stream = File.OpenRead(path);
            using BinaryReader br = new(stream);
            var fileLength = stream.Length;
            while (stream.Position < fileLength)
                yield return br.ReadInt32();
        }

        public static float GetLinearBrightness(this Rgba32 color) => ((float)color.R + color.G + color.B) * color.A / (3.0f * 255f * 255f);

        public static Rgba32 Invert(this Rgba32 colour) => new(colour.PackedValue ^ 0x00FFFFFFu);

        public static IImageProcessingContext CropAndCopyAndInvert(this IImageProcessingContext context, Image source, Point destLocation, Rectangle sourceRectangle, bool invert)
        {
            Action<IImageProcessingContext> op;
            if (invert)
                op = image => image.Crop(sourceRectangle).Invert();
            else
                op = image => image.Crop(sourceRectangle);

            using var tempImage = source.Clone(op);

            return context.DrawImage(tempImage, destLocation, 1.0f);
        }

        public static void DrawImage7By8(this Image<Rgba32> dest, Image<Rgba32> source, Point destLocation, Point sourceLocation, bool invert)
        {
            const int height = 8;
            const int width = 7;
            int sourceStart = sourceLocation.X;
            int sourceEnd = sourceStart + width;
            int destStart = destLocation.X;
            int destEnd = destStart + width;
            source.ProcessPixelRows(dest, (sourceAccessor, destAccessor) =>
            {
                if (invert)
                {
                    for (int i = 0; i < height; i++)
                    {
                        var sourceRow = sourceAccessor.GetRowSpan(sourceLocation.Y + i)[sourceStart..sourceEnd];
                        var destRow = destAccessor.GetRowSpan(destLocation.Y + i)[destStart..destEnd];
                        var sourceRowLong = MemoryMarshal.Cast<Rgba32, ulong>(sourceRow);
                        var destRowLong = MemoryMarshal.Cast<Rgba32, ulong>(destRow);
                        destRowLong[0] = sourceRowLong[0] ^ 0x00_FFFFFF_00_FFFFFFu;
                        destRowLong[1] = sourceRowLong[1] ^ 0x00_FFFFFF_00_FFFFFFu;
                        destRowLong[2] = sourceRowLong[2] ^ 0x00_FFFFFF_00_FFFFFFu;
                        destRow[6].PackedValue = sourceRow[6].PackedValue ^ 0x00_FFFFFFu;
                    }
                }
                else
                {
                    for (int i = 0; i < height; i++)
                    {
                        var sourceRow = sourceAccessor.GetRowSpan(sourceLocation.Y + i)[sourceStart..sourceEnd];
                        var destRow = destAccessor.GetRowSpan(destLocation.Y + i)[destStart..destEnd];
                        sourceRow.CopyTo(destRow);
                    }
                }
            });
        }
        public static void DrawImage(this Image<Rgba32> dest, Image<Rgba32> source, Point destLocation, Rectangle sourceRectangle)
        {
            int height = sourceRectangle.Height;
            int width = sourceRectangle.Width;
            source.ProcessPixelRows(dest, (sourceAccessor, destAccessor) =>
            {
                for (int i = 0; i < height; i++)
                {
                    var sourceRow = sourceAccessor
                        .GetRowSpan(sourceRectangle.Y + i)
                        .Slice(sourceRectangle.X, sourceRectangle.Width);
                    var destRow = destAccessor
                        .GetRowSpan(destLocation.Y + i)
                        .Slice(destLocation.X);

                    sourceRow.CopyTo(destRow);
                }
            });
        }

        public static Task SaveAs1BitPngAsync(this Image image, string path)
        {
            var enc = new PngEncoder
            {
                CompressionLevel = image.Width * image.Height < 200 ? PngCompressionLevel.NoCompression : PngCompressionLevel.BestCompression,
                ColorType = PngColorType.Grayscale,
                BitDepth = PngBitDepth.Bit1
            };
            return image.SaveAsPngAsync(path, enc);
        }
    }
}
