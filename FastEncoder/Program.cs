using ConsoleEncoder;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

var filename = "troll dentist bw.png";

var nodes = Helper.ReadUInt16FromFile($@"D:\Temp\nodes.data").ToArray();
using var fontImage = Image.Load<Rgba32>($@"D:\Temp\compressedfont.png");
using var sourceImage = Image.Load<Rgba32>(@$"D:\Temp\{filename}");
var width = sourceImage.Width / 7 * 7;
var height = sourceImage.Height / 8 * 8;
using var destinationImage = new Image<Rgba32>(width, height);

for (int y = 0; y < height - 7; y += 8)
{
    for (int x = 0; x < width - 6; x += 7)
    {
        float count0 = 0; // Region 0: X = 0, Y = 0, Width = 7, Height = 8, Size = 56
        float count1 = 0; // Region 1: X = 1, Y = 2, Width = 5, Height = 4, Size = 20
        float count2 = 0; // Region 2: X = 2, Y = 0, Width = 3, Height = 3, Size = 9
        float count3 = 0; // Region 3: X = 2, Y = 5, Width = 3, Height = 3, Size = 9
        float count4 = 0; // Region 4: X = 0, Y = 0, Width = 3, Height = 3, Size = 9
        float count5 = 0; // Region 5: X = 4, Y = 0, Width = 3, Height = 3, Size = 9
        float count6 = 0; // Region 6: X = 0, Y = 2, Width = 2, Height = 4, Size = 8
        float count7 = 0; // Region 7: X = 5, Y = 2, Width = 2, Height = 4, Size = 8
        float brightness;
        brightness = sourceImage[x, y].GetLinearBrightness();
        count0 += brightness; // Pixel 0, 0 is in region 0.
        count4 += brightness; // Pixel 0, 0 is in region 4.
        brightness = sourceImage[x + 1, y].GetLinearBrightness();
        count0 += brightness; // Pixel 1, 0 is in region 0.
        count4 += brightness; // Pixel 1, 0 is in region 4.
        brightness = sourceImage[x + 2, y].GetLinearBrightness();
        count0 += brightness; // Pixel 2, 0 is in region 0.
        count2 += brightness; // Pixel 2, 0 is in region 2.
        count4 += brightness; // Pixel 2, 0 is in region 4.
        brightness = sourceImage[x + 3, y].GetLinearBrightness();
        count0 += brightness; // Pixel 3, 0 is in region 0.
        count2 += brightness; // Pixel 3, 0 is in region 2.
        brightness = sourceImage[x + 4, y].GetLinearBrightness();
        count0 += brightness; // Pixel 4, 0 is in region 0.
        count2 += brightness; // Pixel 4, 0 is in region 2.
        count5 += brightness; // Pixel 4, 0 is in region 5.
        brightness = sourceImage[x + 5, y].GetLinearBrightness();
        count0 += brightness; // Pixel 5, 0 is in region 0.
        count5 += brightness; // Pixel 5, 0 is in region 5.
        brightness = sourceImage[x + 6, y].GetLinearBrightness();
        count0 += brightness; // Pixel 6, 0 is in region 0.
        count5 += brightness; // Pixel 6, 0 is in region 5.
        brightness = sourceImage[x, y + 1].GetLinearBrightness();
        count0 += brightness; // Pixel 0, 1 is in region 0.
        count4 += brightness; // Pixel 0, 1 is in region 4.
        brightness = sourceImage[x + 1, y + 1].GetLinearBrightness();
        count0 += brightness; // Pixel 1, 1 is in region 0.
        count4 += brightness; // Pixel 1, 1 is in region 4.
        brightness = sourceImage[x + 2, y + 1].GetLinearBrightness();
        count0 += brightness; // Pixel 2, 1 is in region 0.
        count2 += brightness; // Pixel 2, 1 is in region 2.
        count4 += brightness; // Pixel 2, 1 is in region 4.
        brightness = sourceImage[x + 3, y + 1].GetLinearBrightness();
        count0 += brightness; // Pixel 3, 1 is in region 0.
        count2 += brightness; // Pixel 3, 1 is in region 2.
        brightness = sourceImage[x + 4, y + 1].GetLinearBrightness();
        count0 += brightness; // Pixel 4, 1 is in region 0.
        count2 += brightness; // Pixel 4, 1 is in region 2.
        count5 += brightness; // Pixel 4, 1 is in region 5.
        brightness = sourceImage[x + 5, y + 1].GetLinearBrightness();
        count0 += brightness; // Pixel 5, 1 is in region 0.
        count5 += brightness; // Pixel 5, 1 is in region 5.
        brightness = sourceImage[x + 6, y + 1].GetLinearBrightness();
        count0 += brightness; // Pixel 6, 1 is in region 0.
        count5 += brightness; // Pixel 6, 1 is in region 5.
        brightness = sourceImage[x, y + 2].GetLinearBrightness();
        count0 += brightness; // Pixel 0, 2 is in region 0.
        count4 += brightness; // Pixel 0, 2 is in region 4.
        count6 += brightness; // Pixel 0, 2 is in region 6.
        brightness = sourceImage[x + 1, y + 2].GetLinearBrightness();
        count0 += brightness; // Pixel 1, 2 is in region 0.
        count1 += brightness; // Pixel 1, 2 is in region 1.
        count4 += brightness; // Pixel 1, 2 is in region 4.
        count6 += brightness; // Pixel 1, 2 is in region 6.
        brightness = sourceImage[x + 2, y + 2].GetLinearBrightness();
        count0 += brightness; // Pixel 2, 2 is in region 0.
        count1 += brightness; // Pixel 2, 2 is in region 1.
        count2 += brightness; // Pixel 2, 2 is in region 2.
        count4 += brightness; // Pixel 2, 2 is in region 4.
        brightness = sourceImage[x + 3, y + 2].GetLinearBrightness();
        count0 += brightness; // Pixel 3, 2 is in region 0.
        count1 += brightness; // Pixel 3, 2 is in region 1.
        count2 += brightness; // Pixel 3, 2 is in region 2.
        brightness = sourceImage[x + 4, y + 2].GetLinearBrightness();
        count0 += brightness; // Pixel 4, 2 is in region 0.
        count1 += brightness; // Pixel 4, 2 is in region 1.
        count2 += brightness; // Pixel 4, 2 is in region 2.
        count5 += brightness; // Pixel 4, 2 is in region 5.
        brightness = sourceImage[x + 5, y + 2].GetLinearBrightness();
        count0 += brightness; // Pixel 5, 2 is in region 0.
        count1 += brightness; // Pixel 5, 2 is in region 1.
        count5 += brightness; // Pixel 5, 2 is in region 5.
        count7 += brightness; // Pixel 5, 2 is in region 7.
        brightness = sourceImage[x + 6, y + 2].GetLinearBrightness();
        count0 += brightness; // Pixel 6, 2 is in region 0.
        count5 += brightness; // Pixel 6, 2 is in region 5.
        count7 += brightness; // Pixel 6, 2 is in region 7.
        brightness = sourceImage[x, y + 3].GetLinearBrightness();
        count0 += brightness; // Pixel 0, 3 is in region 0.
        count6 += brightness; // Pixel 0, 3 is in region 6.
        brightness = sourceImage[x + 1, y + 3].GetLinearBrightness();
        count0 += brightness; // Pixel 1, 3 is in region 0.
        count1 += brightness; // Pixel 1, 3 is in region 1.
        count6 += brightness; // Pixel 1, 3 is in region 6.
        brightness = sourceImage[x + 2, y + 3].GetLinearBrightness();
        count0 += brightness; // Pixel 2, 3 is in region 0.
        count1 += brightness; // Pixel 2, 3 is in region 1.
        brightness = sourceImage[x + 3, y + 3].GetLinearBrightness();
        count0 += brightness; // Pixel 3, 3 is in region 0.
        count1 += brightness; // Pixel 3, 3 is in region 1.
        brightness = sourceImage[x + 4, y + 3].GetLinearBrightness();
        count0 += brightness; // Pixel 4, 3 is in region 0.
        count1 += brightness; // Pixel 4, 3 is in region 1.
        brightness = sourceImage[x + 5, y + 3].GetLinearBrightness();
        count0 += brightness; // Pixel 5, 3 is in region 0.
        count1 += brightness; // Pixel 5, 3 is in region 1.
        count7 += brightness; // Pixel 5, 3 is in region 7.
        brightness = sourceImage[x + 6, y + 3].GetLinearBrightness();
        count0 += brightness; // Pixel 6, 3 is in region 0.
        count7 += brightness; // Pixel 6, 3 is in region 7.
        brightness = sourceImage[x, y + 4].GetLinearBrightness();
        count0 += brightness; // Pixel 0, 4 is in region 0.
        count6 += brightness; // Pixel 0, 4 is in region 6.
        brightness = sourceImage[x + 1, y + 4].GetLinearBrightness();
        count0 += brightness; // Pixel 1, 4 is in region 0.
        count1 += brightness; // Pixel 1, 4 is in region 1.
        count6 += brightness; // Pixel 1, 4 is in region 6.
        brightness = sourceImage[x + 2, y + 4].GetLinearBrightness();
        count0 += brightness; // Pixel 2, 4 is in region 0.
        count1 += brightness; // Pixel 2, 4 is in region 1.
        brightness = sourceImage[x + 3, y + 4].GetLinearBrightness();
        count0 += brightness; // Pixel 3, 4 is in region 0.
        count1 += brightness; // Pixel 3, 4 is in region 1.
        brightness = sourceImage[x + 4, y + 4].GetLinearBrightness();
        count0 += brightness; // Pixel 4, 4 is in region 0.
        count1 += brightness; // Pixel 4, 4 is in region 1.
        brightness = sourceImage[x + 5, y + 4].GetLinearBrightness();
        count0 += brightness; // Pixel 5, 4 is in region 0.
        count1 += brightness; // Pixel 5, 4 is in region 1.
        count7 += brightness; // Pixel 5, 4 is in region 7.
        brightness = sourceImage[x + 6, y + 4].GetLinearBrightness();
        count0 += brightness; // Pixel 6, 4 is in region 0.
        count7 += brightness; // Pixel 6, 4 is in region 7.
        brightness = sourceImage[x, y + 5].GetLinearBrightness();
        count0 += brightness; // Pixel 0, 5 is in region 0.
        count6 += brightness; // Pixel 0, 5 is in region 6.
        brightness = sourceImage[x + 1, y + 5].GetLinearBrightness();
        count0 += brightness; // Pixel 1, 5 is in region 0.
        count1 += brightness; // Pixel 1, 5 is in region 1.
        count6 += brightness; // Pixel 1, 5 is in region 6.
        brightness = sourceImage[x + 2, y + 5].GetLinearBrightness();
        count0 += brightness; // Pixel 2, 5 is in region 0.
        count1 += brightness; // Pixel 2, 5 is in region 1.
        count3 += brightness; // Pixel 2, 5 is in region 3.
        brightness = sourceImage[x + 3, y + 5].GetLinearBrightness();
        count0 += brightness; // Pixel 3, 5 is in region 0.
        count1 += brightness; // Pixel 3, 5 is in region 1.
        count3 += brightness; // Pixel 3, 5 is in region 3.
        brightness = sourceImage[x + 4, y + 5].GetLinearBrightness();
        count0 += brightness; // Pixel 4, 5 is in region 0.
        count1 += brightness; // Pixel 4, 5 is in region 1.
        count3 += brightness; // Pixel 4, 5 is in region 3.
        brightness = sourceImage[x + 5, y + 5].GetLinearBrightness();
        count0 += brightness; // Pixel 5, 5 is in region 0.
        count1 += brightness; // Pixel 5, 5 is in region 1.
        count7 += brightness; // Pixel 5, 5 is in region 7.
        brightness = sourceImage[x + 6, y + 5].GetLinearBrightness();
        count0 += brightness; // Pixel 6, 5 is in region 0.
        count7 += brightness; // Pixel 6, 5 is in region 7.
        brightness = sourceImage[x, y + 6].GetLinearBrightness();
        count0 += brightness; // Pixel 0, 6 is in region 0.
        brightness = sourceImage[x + 1, y + 6].GetLinearBrightness();
        count0 += brightness; // Pixel 1, 6 is in region 0.
        brightness = sourceImage[x + 2, y + 6].GetLinearBrightness();
        count0 += brightness; // Pixel 2, 6 is in region 0.
        count3 += brightness; // Pixel 2, 6 is in region 3.
        brightness = sourceImage[x + 3, y + 6].GetLinearBrightness();
        count0 += brightness; // Pixel 3, 6 is in region 0.
        count3 += brightness; // Pixel 3, 6 is in region 3.
        brightness = sourceImage[x + 4, y + 6].GetLinearBrightness();
        count0 += brightness; // Pixel 4, 6 is in region 0.
        count3 += brightness; // Pixel 4, 6 is in region 3.
        brightness = sourceImage[x + 5, y + 6].GetLinearBrightness();
        count0 += brightness; // Pixel 5, 6 is in region 0.
        brightness = sourceImage[x + 6, y + 6].GetLinearBrightness();
        count0 += brightness; // Pixel 6, 6 is in region 0.
        brightness = sourceImage[x, y + 7].GetLinearBrightness();
        count0 += brightness; // Pixel 0, 7 is in region 0.
        brightness = sourceImage[x + 1, y + 7].GetLinearBrightness();
        count0 += brightness; // Pixel 1, 7 is in region 0.
        brightness = sourceImage[x + 2, y + 7].GetLinearBrightness();
        count0 += brightness; // Pixel 2, 7 is in region 0.
        count3 += brightness; // Pixel 2, 7 is in region 3.
        brightness = sourceImage[x + 3, y + 7].GetLinearBrightness();
        count0 += brightness; // Pixel 3, 7 is in region 0.
        count3 += brightness; // Pixel 3, 7 is in region 3.
        brightness = sourceImage[x + 4, y + 7].GetLinearBrightness();
        count0 += brightness; // Pixel 4, 7 is in region 0.
        count3 += brightness; // Pixel 4, 7 is in region 3.
        brightness = sourceImage[x + 5, y + 7].GetLinearBrightness();
        count0 += brightness; // Pixel 5, 7 is in region 0.
        brightness = sourceImage[x + 6, y + 7].GetLinearBrightness();
        count0 += brightness; // Pixel 6, 7 is in region 0.

        int weight = (int)((count0 - 1) * 6f / 55f + 1);
        int flipImage = weight / 4;
        int xor = flipImage * 7;
        weight ^= xor;
        int nodeIndex = nodes[weight];
        weight = (int)((count1 - 1) * 6f / 19f + 1);
        weight ^= xor;
        nodeIndex = nodes[nodeIndex * 8 + weight];
        weight = (int)((count2 - 1) * 6f / 8f + 1);
        weight ^= xor;
        nodeIndex = nodes[nodeIndex * 8 + weight];
        weight = (int)((count3 - 1) * 6f / 8f + 1);
        weight ^= xor;
        nodeIndex = nodes[nodeIndex * 8 + weight];
        weight = (int)((count4 - 1) * 6f / 8f + 1);
        weight ^= xor;
        nodeIndex = nodes[nodeIndex * 8 + weight];
        weight = (int)((count5 - 1) * 6f / 8f + 1);
        weight ^= xor;
        nodeIndex = nodes[nodeIndex * 8 + weight];
        weight = (int)((count6 - 1) * 6f / 7f + 1);
        weight ^= xor;
        nodeIndex = nodes[nodeIndex * 8 + weight];
        weight = (int)((count7 - 1) * 6f / 7f + 1);
        weight ^= xor;
        nodeIndex = nodes[nodeIndex * 8 + weight];
        var location = new Point(x, y);
        var pos = nodeIndex * 8;
        var sourceX = pos % fontImage.Width;
        var sourceY = pos / fontImage.Width * 8;
        var sourceLocation = new Point(sourceX, sourceY);
        destinationImage.DrawImage7By8(fontImage, location, sourceLocation, flipImage > 0);
    }
}
    
destinationImage.Save(@$"D:\Temp\ascii fast {Path.GetFileName(filename)}");