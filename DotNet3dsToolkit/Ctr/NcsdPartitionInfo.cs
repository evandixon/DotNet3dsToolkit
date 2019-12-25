using System;
using System.Collections.Generic;
using System.Text;

namespace DotNet3dsToolkit.Ctr
{
    public struct NcsdPartitionInfo
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

        public byte[] ToByteArray()
        {
            var buffer = new byte[9];
            buffer[0] = CryptType;
            BitConverter.GetBytes(Offset).CopyTo(buffer, 1);
            BitConverter.GetBytes(Length).CopyTo(buffer, 5);
            return buffer;
        }
    }
}
