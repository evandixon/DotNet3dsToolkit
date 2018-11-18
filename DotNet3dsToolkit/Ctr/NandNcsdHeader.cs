using System;
using System.Collections.Generic;
using System.Text;

namespace DotNet3dsToolkit.Ctr
{
    public class NandNcsdHeader : NcsdHeader
    {
        public NandNcsdHeader(byte[] header) : base(header)
        {
            Unknown = new byte[0x5e];
            Array.Copy(header, 0x160, Unknown, 0, 0x5e);

            EncryptedMbrPartitionTable = new byte[0x42];
            Array.Copy(header, 0x1BE, Unknown, 0, 0x42);
        }

        public byte[] Unknown { get; private set; } // Offset: 0x160, Size: 0x5E
        public byte[] EncryptedMbrPartitionTable { get; private set; } // Offset: 0x1BE, Size: 0x42
    }
}
