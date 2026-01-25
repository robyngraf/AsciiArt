namespace ConsoleEncoder
{
    public interface IMask
    {
        bool IsInArea(int x, int y);
        int Total { get; }
    }
}
