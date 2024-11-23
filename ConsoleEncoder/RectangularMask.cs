namespace ConsoleEncoder
{
    public class RectangularMask : IMask
    {
        public RectangularMask(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public int X { get; }
        public int Y { get; }
        public int Width { get; }
        public int Height { get; }

        public int Total => Width * Height;

        public bool IsInArea(int x, int y)
        {
            return x >= X && y >= Y && x < X + Width && y < Y + Height;
        }
    }
}
