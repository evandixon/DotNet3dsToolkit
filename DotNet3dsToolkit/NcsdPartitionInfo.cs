using System;
using System.Collections.Generic;
using System.Text;

namespace DotNet3dsToolkit
{
    public class NcsdPartitionInfo
    {
        public byte CryptType { get; set; }

        public int Offset { get; set; }

        public int Length { get; set; }
    }
}
