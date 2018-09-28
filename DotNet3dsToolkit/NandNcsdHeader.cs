using System;
using System.Collections.Generic;
using System.Text;

namespace DotNet3dsToolkit
{
    public class NandNcsdHeader : NcsdHeader
    {
        public NandNcsdHeader(byte[] header) : base(header)
        {
        }

        public byte[] Unknown { get; private set; } // Offset: 0x160, Size: 0x5E
        public byte[] EncryptedMbrPartitionTable { get; private set; } // Offset: 0x1BE, Size: 0x42
    }
}
