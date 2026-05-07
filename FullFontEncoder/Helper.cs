using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Text;

namespace FullFontEncoder
{
    internal static class Helper
    {
        public static float GetLinearBrightness(this Rgba32 color) => ((float)color.R + color.G + color.B) * color.A / (4.0f * 255f);

    }
}
