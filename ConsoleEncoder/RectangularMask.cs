using System.Threading.Tasks;

namespace ConsoleEncoder
{
    public class RectangularMask(int x, int y, int width, int height) : IMask
    {
        public int X { get; } = x;
        public int Y { get; } = y;
        public int Width { get; } = width;
        public int Height { get; } = height;

        public int Total => Width * Height;

        public bool IsInArea(int x, int y)
        {
            return x >= X && y >= Y && x < X + Width && y < Y + Height;
        }

        public override string ToString() => $"{X},{Y},{Width},{Height}";
    }
}
