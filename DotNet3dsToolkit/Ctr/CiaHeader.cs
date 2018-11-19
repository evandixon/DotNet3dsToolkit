using System;
using System.Collections.Generic;
using System.Text;

namespace DotNet3dsToolkit.Ctr
{
    public class CiaHeader
    {
        public CiaHeader(byte[] data)
        {
            ArchiveHeaderSize = BitConverter.ToInt32(data, 0);
            Type = BitConverter.ToInt16(data, 4);
            Version = BitConverter.ToInt16(data, 6);
            CertificateChainSize = BitConverter.ToInt32(data, 8);
            TicketSize = BitConverter.ToInt32(data, 0xC);
            TmdFileSize = BitConverter.ToInt32(data, 0x10);
            MetaSize = BitConverter.ToInt32(data, 0x14);
            ContentSize = BitConverter.ToInt32(data, 0x18);

            ContentIndex = new byte[ArchiveHeaderSize - 0x20];
            Array.Copy(data, 0x20, ContentIndex, 0, ContentIndex.Length);
        }

        /// <summary>
        /// Archive Header Size (Usually = 0x2020 bytes)
        /// </summary>
        public int ArchiveHeaderSize { get; set; } // Offset: 0x0, Size: 4

        public short Type { get; set; } // Offset: 0x4, Size: 2

        public short Version { get; set; } // Offset: 0x6, Size: 2

        public int CertificateChainSize { get; set; } // Offset: 0x8, Size: 4

        public int TicketSize { get; set; } // Offset: 0xC, Size: 4

        public int TmdFileSize { get; set; } // Offset: 0x10, Size: 4

        /// <summary>
        /// Meta size (0 if no Meta data is present)
        /// </summary>
        public int MetaSize { get; set; } // Offset: 0x14, Size: 4

        public long ContentSize { get; set; } // Offset: 0x18, Size: 8

        public byte[] ContentIndex { get; set; } // Ofset: 0x20, Size: 0x2000
    }
}
