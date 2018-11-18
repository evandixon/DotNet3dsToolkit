using System;
using System.Collections.Generic;
using System.Text;

namespace DotNet3dsToolkit.Ctr
{

    /// <summary>
    /// A NCSD Header
    /// </summary>
    /// <remarks>
    /// Documentation from 3dbrew: https://www.3dbrew.org/wiki/NCSD
    /// </remarks>
    public abstract class NcsdHeader
    {
        public NcsdHeader(byte[] header)
        {
            if (header == null)
            {
                throw new ArgumentNullException(nameof(header));
            }

            if (header.Length < 0x1500)
            {
                throw new ArgumentException(Properties.Resources.NcsdHeader_ConstructorDataTooSmall, nameof(header));
            }

            Signature = new byte[0x100];
            Array.Copy(header, 0, Signature, 0, 0x100);

            Magic = Encoding.ASCII.GetString(header, 0x100, 4);
            ImageSize = BitConverter.ToInt32(header, 0x104);
            MediaId = BitConverter.ToInt64(header, 0x108);
            PartitionsFsType = BitConverter.ToInt64(header, 0x110);

            var partitions = new List<NcsdPartitionInfo>();
            for (int i = 0; i < 8; i++)
            {
                partitions.Add(new NcsdPartitionInfo
                {
                    CryptType = header[0x118 + i],
                    Offset = BitConverter.ToInt32(header, 0x120 + (i * 2) * 4),
                    Length = BitConverter.ToInt32(header, 0x120 + (i * 2 + 1) * 4)
                });
            }
            Partitions = partitions.ToArray();
        }

        /// <summary>
        /// RSA-2048 SHA-256 signature of the NCSD header
        /// </summary>
        public byte[] Signature { get; private set; } // Offset: 0, Size: 0x100

        /// <summary>
        /// Magic Number 'NCSD'
        /// </summary>
        public string Magic { get; private set; } // Offset: 0x100, Size: 0x4

        /// <summary>
        /// Size of the NCSD image, in media units (1 media unit = 0x200 bytes)
        /// </summary>
        public int ImageSize { get; private set; } // Offset: 0x104, Size: 0x4

        public long MediaId { get; private set; } // Offset: 0x108, Size: 0x8

        /// <summary>
        /// Partitions FS type (0=None, 1=Normal, 3=FIRM, 4=AGB_FIRM save)
        /// </summary>
        public long PartitionsFsType { get; private set; } // Offset: 0x110, Size: 0x8

        // Crypt type offset 0x118, size of each entry: 1 byte
        // Offset and length in media units offset: 0x120, length 4 bytes each

        public NcsdPartitionInfo[] Partitions { get; private set; }

    }
}
