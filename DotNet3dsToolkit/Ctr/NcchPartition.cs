using SkyEditor.IO;
using SkyEditor.IO.Binary;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DotNet3dsToolkit.Ctr
{
    public class NcchPartition : IDisposable
    {
        private const int MediaUnitSize = 0x200;

        public static async Task<bool> IsNcch(IReadOnlyBinaryDataAccessor file)
        {
            try
            {
                if (file.Length < 0x104)
                {
                    return false;
                }

                return await file.ReadStringAsync(0x100, 4, Encoding.ASCII) == "NCCH";
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static async Task<NcchPartition> Load(IReadOnlyBinaryDataAccessor data)
        {
            NcchHeader header = null;
            if (data.Length > 0)
            {
                header = new NcchHeader(await data.ReadArrayAsync(0, 0x200));
            }            

            var partition = new NcchPartition(header);
            await partition.Initialize(data);
            return partition;
        }

        public NcchPartition(NcchHeader header)
        {
            Header = header;
        }

        public NcchPartition(RomFs romfs = null, ExeFs exefs = null, IReadOnlyBinaryDataAccessor exheader = null)
        {
            RomFs = romfs;
            ExeFs = exefs;
            ExHeader = exheader;
        }

        public async Task Initialize(IReadOnlyBinaryDataAccessor data)
        {
            if (Header != null && Header.RomFsSize > 0)
            {
                if (Header.ExeFsOffset > 0 && Header.ExeFsSize > 0)
                {
                    ExeFs = await ExeFs.Load(data.GetReadOnlyDataReference((long)Header.ExeFsOffset * MediaUnitSize, (long)Header.ExeFsSize * MediaUnitSize));
                }
                if (Header.RomFsOffset > 0 && Header.RomFsOffset > 0)
                {
                    RomFs = await RomFs.Load(data.GetReadOnlyDataReference((long)Header.RomFsOffset * MediaUnitSize, (long)Header.RomFsSize * MediaUnitSize));
                }                    
                if (Header.ExHeaderSize > 0)
                {
                    ExHeader = data.GetReadOnlyDataReference(0x200, Header.ExHeaderSize);
                }
            }
        }

        public NcchHeader Header { get; } // Could be null if this is an empty partition

        public ExeFs ExeFs { get; private set; } // Could be null if not applicable
        public RomFs RomFs { get; private set; } // Could be null if not applicable
        public IReadOnlyBinaryDataAccessor ExHeader { get; private set; } // Could be null if not applicable


        public void Dispose()
        {
            RomFs?.Dispose();
        }

        #region Child Classes
        public class NcchHeader
        {
            public NcchHeader(byte[] header)
            {
                if (header == null)
                {
                    throw new ArgumentNullException(nameof(header));
                }

                if (header.Length < 0x200)
                {
                    throw new ArgumentException(Properties.Resources.NcchHeader_ConstructorDataTooSmall, nameof(header));
                }

                Signature = new byte[0x100];
                Array.Copy(header, 0, Signature, 0, 0x100);

                Magic = Encoding.ASCII.GetString(header, 0x100, 4);
                ContentSize = BitConverter.ToInt32(header, 0x104);
                PartitionId = BitConverter.ToInt32(header, 0x108);
                MakerCode = BitConverter.ToInt16(header, 0x110);
                Version = BitConverter.ToInt16(header, 0x112);
                ContentLockSeedHash = BitConverter.ToInt32(header, 0x114);
                ProgramId = BitConverter.ToInt64(header, 0x118);

                Reserved1 = new byte[0x10];
                Array.Copy(header, 0x120, Reserved1, 0, 0x10);

                LogoRegionHash = new byte[0x20];
                Array.Copy(header, 0x130, LogoRegionHash, 0, 0x20);

                ProductCode = Encoding.ASCII.GetString(header, 0x150, 0x10).TrimEnd('\0');

                ExHeaderHash = new byte[0x20];
                Array.Copy(header, 0x160, ExHeaderHash, 0, 0x20);

                ExHeaderSize = BitConverter.ToInt32(header, 0x180);
                Reserved2 = BitConverter.ToInt32(header, 0x184);

                Flags = new byte[8];
                Array.Copy(header, 0x188, Flags, 0, 8);

                PlainRegionOffset = BitConverter.ToInt32(header, 0x190);
                PlainRegionSize = BitConverter.ToInt32(header, 0x194);
                LogoRegionOffset = BitConverter.ToInt32(header, 0x198);
                LogoRegionSize = BitConverter.ToInt32(header, 0x19C);
                ExeFsOffset = BitConverter.ToInt32(header, 0x1A0);
                ExeFsSize = BitConverter.ToInt32(header, 0x1A4);
                ExeFsHashRegionSize = BitConverter.ToInt32(header, 0x1A8);
                Reserved3 = BitConverter.ToInt32(header, 0x1AC);
                RomFsOffset = BitConverter.ToInt32(header, 0x1B0);
                RomFsSize = BitConverter.ToInt32(header, 0x1B4);
                RomFsHashRegionSize = BitConverter.ToInt32(header, 0x1B8);
                Reserved4 = BitConverter.ToInt32(header, 0x1BC);

                ExeFsSuperblockHash = new byte[0x20];
                Array.Copy(header, 0x1C0, ExeFsSuperblockHash, 0, 0x20);

                RomFsSuperblockHash = new byte[0x20];
                Array.Copy(header, 0x1E0, RomFsSuperblockHash, 0, 0x20);
            }

            /// <summary>
            /// RSA-2048 signature of the NCCH header, using SHA-256.
            /// </summary>
            public byte[] Signature { get; set; } // Offset: 0x0, size: 0x100

            /// <summary>
            /// Magic ID, always 'NCCH'
            /// </summary>
            public string Magic { get; set; } // Offset: 0x100, size: : 0x4

            /// <summary>
            /// Content size, in media units (1 media unit = 0x200 bytes)
            /// </summary>
            public int ContentSize { get; set; } // Offset: 0x104, size: 0x4

            public long PartitionId { get; set; } // Offset: 0x108, size: 0x8

            public short MakerCode { get; set; } // Offset: 0x110, size: 0x2

            public short Version { get; set; } // Offset: 0x112, size: 0x2

            /// <summary>
            /// When ncchflag[7] = 0x20 starting with FIRM 9.6.0-X, this is compared with the first output u32 from a SHA256 hash.
            /// The data used for that hash is 0x18-bytes: (0x10-long title-unique content lock seed) (programID from NCCH+0x118). 
            /// This hash is only used for verification of the content lock seed, and is not the actual keyY.
            /// </summary>
            public int ContentLockSeedHash { get; set; } // Offset: 0x114, size: 4

            /// <summary>
            /// The Program ID, also known as the Title ID
            /// </summary>
            public long ProgramId { get; set; } // Offset: 0x118, size: 8

            public byte[] Reserved1 { get; set; } // Offset: 0x120, size: 0x10

            /// <summary>
            /// Logo Region SHA-256 hash. (For applications built with SDK 5+) (Supported from firmware: 5.0.0-11)
            /// </summary>
            public byte[] LogoRegionHash { get; set; } // Offset: 0x130, size: 0x20

            public string ProductCode { get; set; } // Offset: 0x150, size: 0x10

            /// <summary>
            /// Extended header SHA-256 hash (SHA256 of 2x Alignment Size, beginning at 0x0 of ExHeader)
            /// </summary>
            public byte[] ExHeaderHash { get; set; } // Offset: 0x160, size: 0x20

            /// <summary>
            /// Extended header size, in bytes
            /// </summary>
            public int ExHeaderSize { get; set; } // Offset: 0x180, size: 4

            public int Reserved2 { get; set; } // Offset: 0x184, size: 4

            /// <summary>
            /// 3	Crypto Method: When this is non-zero, a NCCH crypto method using two keyslots is used(see above).
            /// 4	Content Platform: 1 = CTR, 2 = snake (New 3DS).
            /// 5	Content Type Bit-masks: Data = 0x1, Executable = 0x2, SystemUpdate = 0x4, Manual = 0x8, Child = (0x4|0x8), Trial = 0x10. When 'Data' is set, but not 'Executable', NCCH is a CFA.Otherwise when 'Executable' is set, NCCH is a CXI.
            /// 6	Content Unit Size i.e.u32 ContentUnitSize = 0x200 * 2 ^ flags[6];
            /// 7	Bit-masks: FixedCryptoKey = 0x1, NoMountRomFs = 0x2, NoCrypto = 0x4, using a new keyY generator = 0x20(starting with FIRM 9.6.0 - X).
            /// </summary>
            public byte[] Flags { get; set; } // Offset: 0x188, size: 8

            /// <summary>
            /// Plain region offset, in media units
            /// </summary>
            public int PlainRegionOffset { get; set; } // Offset: 0x190, size: 4

            /// <summary>
            /// Plain region size, in media units
            /// </summary>
            public int PlainRegionSize { get; set; } // Offset: 0x194, size: 4

            /// <summary>
            /// Logo Region offset, in media units (For applications built with SDK 5+) (Supported from firmware: 5.0.0-11)
            /// </summary>
            public int LogoRegionOffset { get; set; } // Offset: 0x198, size: 4

            /// <summary>
            /// Logo Region size, in media units (For applications built with SDK 5+) (Supported from firmware: 5.0.0-11)
            /// </summary>
            public int LogoRegionSize { get; set; } // Offset: 0x19C, size: 4

            /// <summary>
            /// ExeFS offset, in media units
            /// </summary>
            public int ExeFsOffset { get; set; } // Offset: 0x1A0, size: 4

            /// <summary>
            /// ExeFS size, in media units
            /// </summary>
            public int ExeFsSize { get; set; } // Offset: 0x1A4, size: 4

            /// <summary>
            /// ExeFS hash region size, in media units
            /// </summary>
            public int ExeFsHashRegionSize { get; set; } // Offset: 0x1A8, size: 4

            public int Reserved3 { get; set; } // Offset: 0x1AC, size: 4

            /// <summary>
            /// RomFS offset, in media units
            /// </summary>
            public int RomFsOffset { get; set; } // Offset: 0x1B0, size: 4

            /// <summary>
            /// RomFS size, in media units
            /// </summary>
            public int RomFsSize { get; set; } // Offset: 0x1B4, size: 4

            /// <summary>
            /// RomFS hash region size, in media units
            /// </summary>
            public int RomFsHashRegionSize { get; set; } // Offset: 0x1B8, size: 4

            public int Reserved4 { get; set; } // Offset: 0x1BC, size: 4

            /// <summary>
            /// ExeFS superblock SHA-256 hash - (SHA-256 hash, starting at 0x0 of the ExeFS over the number of media units specified in the ExeFS hash region size)
            /// </summary>
            public byte[] ExeFsSuperblockHash { get; set; } // Offset: 0x1C0, size: 0x20

            /// <summary>
            /// RomFS superblock SHA-256 hash - (SHA-256 hash, starting at 0x0 of the RomFS over the number of media units specified in the RomFS hash region size)
            /// </summary>
            public byte[] RomFsSuperblockHash { get; set; } // Offset: 0x1E0, size: 0x20
        }
        #endregion
    }
}
