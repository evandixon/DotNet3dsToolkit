using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DotNet3dsToolkit
{
    public class RomFs
    {
        public async Task<RomFs> Load(NcchPartition parentPartition)
        {
            var header = new RomFsHeader(await parentPartition.ReadRomFsAsync(0, 0x5B));
            return new RomFs(parentPartition, header);
        }

        public RomFs(NcchPartition parentPartition, RomFsHeader header)
        {
            ParentPartition = parentPartition ?? throw new ArgumentNullException(nameof(parentPartition));
            Header = header ?? throw new ArgumentNullException(nameof(parentPartition));
        }

        private NcchPartition ParentPartition { get; }

        public RomFsHeader Header { get; }

        public async Task<byte[]> ReadAsync()
        {
            return await ParentPartition.ReadRomFsAsync();
        }

        public async Task<byte> ReadAsync(long index)
        {
            return await ParentPartition.ReadRomFsAsync(index);
        }

        public async Task<byte[]> ReadAsync(long index, int length)
        {
            return await ParentPartition.ReadRomFsAsync(index, length);
        }

        #region Child Classes
        public class RomFsHeader
        {
            public RomFsHeader(byte[] header)
            {
                if (header == null)
                {
                    throw new ArgumentNullException(nameof(header));
                }

                if (header.Length < 0x6B)
                {
                    throw new ArgumentException(Properties.Resources.RomFsHeader_ConstructorDataTooSmall, nameof(header));
                }

                Magic = Encoding.ASCII.GetString(header, 0, 4);
                MagicNumber = BitConverter.ToInt32(header, 0x4);
                MasterHashSize = BitConverter.ToInt32(header, 0x8);
                Level1LogicalOffset = BitConverter.ToInt64(header, 0xC);
                Level1HashDataSize = BitConverter.ToInt64(header, 0x14);
                Level1BlockSize = BitConverter.ToInt32(header, 0x1C);
                Reserved1 = BitConverter.ToInt32(header, 0x20);
                Level2LogicalOffset = BitConverter.ToInt64(header, 0x24);
                Level2HashDataSize = BitConverter.ToInt64(header, 0x2C);
                Level2BlockSize = BitConverter.ToInt32(header, 0x34);
                Reserved2 = BitConverter.ToInt32(header, 0x38);
                Level3LogicalOffset = BitConverter.ToInt64(header, 0x3C);
                Level3HashDataSize = BitConverter.ToInt64(header, 0x44);
                Level3BlockSize = BitConverter.ToInt32(header, 0x4C);
                Reserved3 = BitConverter.ToInt32(header, 0x50);
                Reserved4 = BitConverter.ToInt32(header, 0x54);
                OptionalInfoSize = BitConverter.ToInt32(header, 0x58);
            }

            /// <summary>
            /// Magic "IVFC"
            /// </summary>
            public string Magic { get; set; } // Offset: 0, size: 4

            /// <summary>
            /// Magic number 0x10000
            /// </summary>
            public int MagicNumber { get; set; } // Offset: 0x04, size: 4

            public int MasterHashSize { get; set; } // Offset: 0x08, size: 4

            public long Level1LogicalOffset { get; set; } // Offset: 0x0C, size: 8

            public long Level1HashDataSize { get; set; } // Offset: 0x14, size: 8

            /// <summary>
            /// Level 1 block size, in log2
            /// </summary>
            public int Level1BlockSize { get; set; } // Offset: 0x1C, size: 4

            public int Reserved1 { get; set; } // Offset: 0x20, size: 4

            public long Level2LogicalOffset { get; set; } // Offset: 0x24, size: 8

            public long Level2HashDataSize { get; set; } // Offset: 0x2C, size: 8

            /// <summary>
            /// Level 2 block size, in log2
            /// </summary>
            public int Level2BlockSize { get; set; } // Offset: 0x34, size: 4

            public int Reserved2 { get; set; } // Offset: 0x38, size: 4

            public long Level3LogicalOffset { get; set; } // Offset: 0x3C, size: 8

            public long Level3HashDataSize { get; set; } // Offset: 0x44, size: 8

            /// <summary>
            /// Level 3 block size, in log2
            /// </summary>
            public int Level3BlockSize { get; set; } // Offset: 0x4C, size: 4

            public int Reserved3 { get; set; } // Offset: 0x50, size: 4

            public int Reserved4 { get; set; } // Offset: 0x54, size: 4

            public int OptionalInfoSize { get; set; } // Offset: 0x58, size: 4
        }
        #endregion
    }
}
