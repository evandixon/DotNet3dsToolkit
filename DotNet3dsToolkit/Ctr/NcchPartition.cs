using SkyEditor.IO;
using SkyEditor.IO.Binary;
using SkyEditor.IO.FileSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
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

        /// <summary>
        /// Builds a new NCCH partition from the given directory
        /// </summary>
        /// <param name="fileSystem">File system from which to load the files</param>
        /// <returns>A newly built NCCH partition</returns>
        public static async Task<NcchPartition> Build(string headerFilename, string exHeaderFilename, string? exeFsDirectory, string? romFsDiretory, string? plainRegionFilename, string? logoFilename, IFileSystem fileSystem, ProcessingProgressedToken progressToken = null)
        {
            ProcessingProgressedToken exefsToken = null;
            ProcessingProgressedToken romfsToken = null;
            void ReportProgress()
            {
                if (progressToken != null)
                {
                    progressToken.TotalFileCount = (exefsToken?.TotalFileCount + romfsToken?.TotalFileCount).GetValueOrDefault();
                    progressToken.ProcessedFileCount = (exefsToken?.ProcessedFileCount + romfsToken?.ProcessedFileCount).GetValueOrDefault();
                }
            };

            Task<ExeFs> exeFsTask;
            if (!string.IsNullOrEmpty(exeFsDirectory))
            {
                if (progressToken != null)
                {
                    exefsToken = new ProcessingProgressedToken();
                    exefsToken.FileCountChanged += (sender, e) => ReportProgress();
                }
                exeFsTask = Task.Run(() => ExeFs.Build(exeFsDirectory, fileSystem, exefsToken));
            }
            else
            {
                exeFsTask = Task.FromResult<ExeFs>(null);
            }

            Task<RomFs> romFsTask;
            if (!string.IsNullOrEmpty(romFsDiretory))
            {
                if (progressToken != null)
                {
                    romfsToken = new ProcessingProgressedToken();
                    romfsToken.FileCountChanged += (sender, e) => ReportProgress();
                }
                romFsTask = Task.Run(() => RomFs.Build(romFsDiretory, fileSystem, romfsToken));
            }
            else
            {
                romFsTask = Task.FromResult<RomFs>(null);
            }

            var header = new NcchHeader(fileSystem.ReadAllBytes(headerFilename));

            NcchExtendedHeader exHeader = null;
            if (!string.IsNullOrEmpty(exHeaderFilename))
            {
                using var exHeaderData = new BinaryFile(fileSystem.ReadAllBytes(exHeaderFilename));
                exHeader = await NcchExtendedHeader.Load(exHeaderData);
            }

            string plainRegion = null;
            if (!string.IsNullOrEmpty(plainRegionFilename))
            {
                plainRegion = fileSystem.ReadAllText(plainRegionFilename);
            }

            byte[] logo = null;
            if (!string.IsNullOrEmpty(logoFilename))
            {
                logo = fileSystem.ReadAllBytes(logoFilename);
            }

            return new NcchPartition(await romFsTask, await exeFsTask, header, exHeader, plainRegion, logo);
        }

        public NcchPartition(NcchHeader header)
        {
            Header = header;
        }

        public NcchPartition(RomFs romfs = null, ExeFs exefs = null, NcchHeader header = null, NcchExtendedHeader exheader = null, string plainRegion = null, byte[] logo = null)
        {
            RomFs = romfs;
            ExeFs = exefs;
            Header = header;
            ExHeader = exheader;
            PlainRegion = plainRegion;
            Logo = logo;
        }

        public async Task Initialize(IReadOnlyBinaryDataAccessor data)
        {
            this.RawData = data;
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
                    ExHeader = await NcchExtendedHeader.Load(data.GetReadOnlyDataReference(0x200, Header.ExHeaderSize));
                }

                PlainRegion = await data.ReadStringAsync(Header.PlainRegionOffset * MediaUnitSize, Header.PlainRegionSize * MediaUnitSize, Encoding.ASCII);
                Logo = await data.ReadArrayAsync(Header.LogoRegionOffset * MediaUnitSize, Header.LogoRegionSize * MediaUnitSize);
            }
        }

        public IReadOnlyBinaryDataAccessor RawData { get; set; }

        public NcchHeader Header { get; } // Could be null if this is an empty partition

        public ExeFs ExeFs { get; private set; } // Could be null if not applicable
        public RomFs RomFs { get; private set; } // Could be null if not applicable
        public NcchExtendedHeader ExHeader { get; private set; } // Could be null if not applicable

        public string PlainRegion { get; private set; }

        public byte[] Logo { get; private set; }

        /// <summary>
        /// Writes the current state of the NCCH partition to the given binary data accessor
        /// </summary>
        /// <param name="data">Data accessor to receive the binary data</param>
        /// <returns>A long representing the total length of data written</returns>
        public async Task<long> WriteBinary(IWriteOnlyBinaryDataAccessor data)
        {
            // Get the data
            var exheader = ExHeader?.ToByteArray();

            var plainRegion = !string.IsNullOrEmpty(PlainRegion) ? Encoding.ASCII.GetBytes(PlainRegion) : null;
            var plainRegionOffset = 0;
            var logoRegionOffset = 0;

            var exeFs = ExeFs?.ToByteArray();
            var exeFsOffset = 0;

            var romFs = RomFs?.Data;
            var romFsOffset = 0;

            // Write the data
            var offset = 0x200; // Skip the header, write it last
            if (exheader != null)
            {
                await data.WriteAsync(offset, exheader);
                offset += exheader.Length;
            }
            if (plainRegion != null)
            {
                plainRegionOffset = offset;
                await data.WriteAsync(offset, plainRegion);
                offset += plainRegion.Length;

                var padding = new byte[0x200 - plainRegion.Length % 0x200];
                await data.WriteAsync(offset, padding);
                offset += padding.Length;
            }
            if (Logo != null)
            {
                logoRegionOffset = offset;
                await data.WriteAsync(offset, Logo);
                offset += Logo.Length;

                var padding = new byte[0x200 - Logo.Length % 0x200];
                await data.WriteAsync(offset, padding);
                offset += padding.Length;
            }
            if (exeFs != null)
            {
                exeFsOffset = offset;
                await data.WriteAsync(offset, exeFs);
                offset += exeFs.Length;

                var padding = new byte[0x200 - exeFs.Length % 0x200];
                await data.WriteAsync(offset, padding);
                offset += padding.Length;
            }
            if (romFs != null)
            {
                romFsOffset = offset;
                const int bufferSize = 1024 * 1024;
                for (int i = 0; i < romFs.Length; i += bufferSize)
                {
                    int length = (int)Math.Min(bufferSize, romFs.Length - i);
                    var block = await romFs.ReadArrayAsync(i, length);
                    await data.WriteAsync(offset, block);
                    offset += length;
                }

                var padding = new byte[0x200 - romFs.Length % 0x200];
                await data.WriteAsync(offset, padding);
                offset += padding.Length;
            }

            // Create a new header
            using var sha = SHA256.Create();

            var header = NcchHeader.Copy(this.Header);
            header.Signature = new byte[0x100]; // We lack the 3DS's private key, so leave out the signature
            header.ContentSize = (offset + MediaUnitSize - 1) / MediaUnitSize; // offset/MediaUnitSize, but rounding up
            header.ContentLockSeedHash = 0; // Unknown, left blank by SciresM's 3DS Builder
            if (Logo != null)
            {
                header.LogoRegionHash = sha.ComputeHash(Logo);
            }
            else
            {
                header.LogoRegionHash = new byte[0x20];
            }

            if (exheader != null)
            {
                header.ExHeaderHash = NcchExtendedHeader.GetSuperblockHash(sha, exheader);
                header.ExHeaderSize = NcchExtendedHeader.ExHeaderDataSize;
            }
            else
            {
                header.ExHeaderHash = new byte[0x20];
                header.ExHeaderSize = 0;
            }

            header.PlainRegionOffset = (plainRegionOffset + MediaUnitSize - 1) / MediaUnitSize;
            header.PlainRegionSize = ((plainRegion?.Length ?? 0) + MediaUnitSize - 1) / MediaUnitSize;
            header.LogoRegionOffset = (logoRegionOffset + MediaUnitSize - 1) / MediaUnitSize;
            header.LogoRegionSize = ((Logo?.Length ?? 0) + MediaUnitSize - 1) / MediaUnitSize;
            header.ExeFsOffset = (exeFsOffset + MediaUnitSize - 1) / MediaUnitSize;
            header.ExeFsSize = ((exeFs?.Length ?? 0) + MediaUnitSize - 1) / MediaUnitSize;
            header.ExeFsHashRegionSize = 1; // Static 0x200 for exefs superblock
            header.RomFsOffset = (romFsOffset + MediaUnitSize - 1) / MediaUnitSize;
            header.RomFsSize = ((int)(romFs?.Length ?? 0) + MediaUnitSize - 1) / MediaUnitSize;
            header.RomFsHashRegionSize = ((RomFs?.Header?.MasterHashSize ?? 0) + MediaUnitSize - 1) / MediaUnitSize;
            header.ExeFsSuperblockHash = ExeFs?.GetSuperblockHash() ?? new byte[0x20];
            header.RomFsSuperblockHash = RomFs != null ? await RomFs.GetSuperblockHash(sha, romFs, RomFs.Header) : new byte[0x20];

            var headerData = await header.ToBinary().ReadArrayAsync();
            await data.WriteAsync(0, headerData);

            return offset;
        }

        public void Dispose()
        {
            RomFs?.Dispose();
        }

        #region Child Classes
        public class NcchHeader
        {
            private NcchHeader()
            {
            }

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
                PartitionId = BitConverter.ToInt64(header, 0x108);
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
            /// Creates a new <see cref="NcchHeader"/> based on the given header
            /// </summary>
            /// <remarks>
            /// The following properties need to be updated manually:
            /// - <see cref="Signature" />
            /// - <see cref="ContentSize" />
            /// - <see cref="ContentLockSeedHash" />
            /// - <see cref="LogoRegionHash" />
            /// - <see cref="ExHeaderHash" />
            /// - <see cref="ExHeaderSize" />
            /// - <see cref="PlainRegionOffset" />
            /// - <see cref="PlainRegionSize" />
            /// - <see cref="LogoRegionOffset" />
            /// - <see cref="LogoRegionSize" />
            /// - <see cref="ExeFsOffset" />
            /// - <see cref="ExeFsSize" />
            /// - <see cref="ExeFsHashRegionSize" />
            /// - <see cref="RomFsOffset" />
            /// - <see cref="RomFsSize" />
            /// - <see cref="RomFsHashRegionSize" />
            /// - <see cref="ExeFsSuperblockHash" />
            /// - <see cref="RomFsSuperblockHash" />
            /// </remarks>
            public static NcchHeader Copy(NcchHeader other)
            {
                if (other == null)
                {
                    throw new ArgumentNullException(nameof(other));
                }

                return new NcchHeader
                {
                    Magic = "NCCH",
                    PartitionId = other.PartitionId,
                    MakerCode = other.MakerCode,
                    Version = other.Version,
                    ProgramId = other.ProgramId,
                    Reserved1 = other.Reserved1,
                    ProductCode = other.ProductCode,
                    Reserved2 = other.Reserved2,
                    Flags = other.Flags,
                    Reserved3 = other.Reserved3,
                    Reserved4 = other.Reserved4
                };
            }

            public BinaryFile ToBinary()
            {
                var binary = new BinaryFile(new byte[0x200]);
                binary.Write(0, 0x100, Signature);
                binary.WriteString(0x100, Encoding.ASCII, Magic);
                binary.WriteInt32(0x104, ContentSize);
                binary.WriteInt64(0x108, PartitionId);
                binary.WriteInt16(0x110, MakerCode);
                binary.WriteInt16(0x112, Version);
                binary.WriteInt32(0x114, ContentLockSeedHash);
                binary.WriteInt64(0x118, ProgramId);
                binary.Write(0x120, 0x10, Reserved1);
                binary.Write(0x130, 0x20, LogoRegionHash);
                binary.WriteString(0x150, Encoding.ASCII, ProductCode);
                binary.Write(0x160, 0x20, ExHeaderHash);
                binary.WriteInt32(0x180, ExHeaderSize);
                binary.WriteInt32(0x184, Reserved2);
                binary.Write(0x188, 0x8, Flags);
                binary.WriteInt32(0x190, PlainRegionOffset);
                binary.WriteInt32(0x194, PlainRegionSize);
                binary.WriteInt32(0x198, LogoRegionOffset);
                binary.WriteInt32(0x19C, LogoRegionSize);
                binary.WriteInt32(0x1A0, ExeFsOffset);
                binary.WriteInt32(0x1A4, ExeFsSize);
                binary.WriteInt32(0x1A8, ExeFsHashRegionSize);
                binary.WriteInt32(0x1AC, Reserved3);
                binary.WriteInt32(0x1B0, RomFsOffset);
                binary.WriteInt32(0x1B4, RomFsSize);
                binary.WriteInt32(0x1B8, RomFsHashRegionSize);
                binary.WriteInt32(0x1BC, Reserved4);
                binary.Write(0x1C0, 0x20, ExeFsSuperblockHash);
                binary.Write(0x1E0, 0x20, RomFsSuperblockHash);
                return binary;
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
