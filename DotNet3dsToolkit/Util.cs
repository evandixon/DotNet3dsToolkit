using System;
using System.Collections.Generic;
using System.Text;

namespace DotNet3dsToolkit
{
    public static class Util
    {
        public static int Align(int offset, int alignment)
        {
            int mask = ~(alignment - 1);
            return (offset + (alignment - 1)) & mask;
        }

        public static long Align64(long offset, int alignment)
        {
            long mask = ~(alignment - 1);
            return (offset + (alignment - 1)) & mask;
        }
    }
}
