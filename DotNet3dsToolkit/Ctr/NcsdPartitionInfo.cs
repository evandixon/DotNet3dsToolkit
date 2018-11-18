using System;
using System.Collections.Generic;
using System.Text;

namespace DotNet3dsToolkit.Ctr
{
    public class NcsdPartitionInfo
    {
        public byte CryptType { get; set; }

        /// <summary>
        /// Data offset in media units (1 media unit = 0x200 bytes)
        /// </summary>
        public int Offset { get; set; }

        /// <summary>
        /// Data length in media units (1 media unit = 0x200 bytes)
        /// </summary>
        public int Length { get; set; }
    }
}
