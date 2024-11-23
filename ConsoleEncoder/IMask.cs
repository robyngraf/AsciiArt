using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleEncoder
{
    public interface IMask
    {
        bool IsInArea(int x, int y);
        int Total { get; }
    }
}
