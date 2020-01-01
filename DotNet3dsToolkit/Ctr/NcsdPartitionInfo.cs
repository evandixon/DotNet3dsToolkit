using System;
using System.Collections.Generic;
using System.Text;

namespace DotNet3dsToolkit.Ctr
{
    public struct NcsdPartitionInfo
    {
        /// <summary>
        /// Partitions FS type (0=None, 1=Normal, 3=FIRM, 4=AGB_FIRM save)
        /// </summary>
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
