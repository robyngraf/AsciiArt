namespace ConsoleEncoder
{
    internal class CheckerboardMask(int width, int height) : IMask
    {
        public int Total => Width * Height / 2;

        public int Width { get; } = width;
        public int Height { get; } = height;

        public bool IsInArea(int x, int y)
        {
            return (x + y) % 2 == 1;
        }

        public override string ToString() => $"CheckerboardMask {Width},{Height}";
    }
}
