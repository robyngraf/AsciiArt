namespace ConsoleEncoder
{
    internal class CheckerboardMask : IMask
    {
        public CheckerboardMask(int width, int height)
        {
            Width = width;
            Height = height;
        }
        public int Total => Width * Height / 2;

        public int Width { get; }
        public int Height { get; }

        public bool IsInArea(int x, int y)
        {
            return (x + y) % 2 == 1;
        }
    }
}
