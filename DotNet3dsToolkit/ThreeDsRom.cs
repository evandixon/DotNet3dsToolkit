using SkyEditor.Core.IO;
using SkyEditor.Core.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNet3dsToolkit
{
    public class ThreeDsRom : IOpenableFile, IDisposable
    {

        public NcsdHeader Header { get; set; }

        public NcchPartition[] Partitions { get; set; }

        private GenericFile RawData { get; set; }

        public RomFs RomFs
        {
            get
            {
                return Partitions[0].RomFs;
            }
        }

        public async Task OpenFile(string filename, IIOProvider provider)
        {
            RawData = new GenericFile();
            RawData.EnableMemoryMappedFileLoading = true;
            RawData.EnableInMemoryLoad = true;
            await RawData.OpenFile(filename, provider);

            // To-do: determine which NCSD header to use
            Header = new CartridgeNcsdHeader(await RawData.ReadAsync(0, 0x1500));

            Partitions = new NcchPartition[Header.Partitions.Length];

            var a = new AsyncFor();
            a.RunSynchronously = !RawData.IsThreadSafe;
            await a.RunFor(async i =>
            {
                var partitionStart = (long)Header.Partitions[i].Offset * 0x200;
                var partitionLength = (long)Header.Partitions[i].Length * 0x200;
                Partitions[i] = await NcchPartition.Load(new GenericFileReference(RawData, partitionStart, (int)partitionLength), i);
            }, 0, Header.Partitions.Length - 1);
        }

        public async Task ExtractFiles(string directoryName, IIOProvider provider, ProgressReportToken progressReportToken = null)
        {

            List<ExtractionProgressedToken> extractionProgressedTokens = null;
            if (progressReportToken != null)
            {
                extractionProgressedTokens = new List<ExtractionProgressedToken>();
                progressReportToken.IsIndeterminate = false;
            }

            void onExtractionTokenProgressed(object sender, EventArgs e)
            {
                if (progressReportToken != null)
                {
                    progressReportToken.Progress = (float)extractionProgressedTokens.Select(t => t.ExtractedFileCount).Sum() / extractionProgressedTokens.Select(t => t.TotalFileCount).Sum();
                }
            }

            if (!provider.DirectoryExists(directoryName))
            {
                provider.CreateDirectory(directoryName);
            }
            
            var tasks = new List<Task>();
            for (int i = 0; i < Partitions.Length; i++)
            {
                var partition = Partitions[i];

                if (partition?.ExeFs != null)
                {
                    ExtractionProgressedToken exefsExtractionProgressedToken = null;
                    if (exefsExtractionProgressedToken != null)
                    {
                        exefsExtractionProgressedToken = new ExtractionProgressedToken();
                        exefsExtractionProgressedToken.FileCountChanged += onExtractionTokenProgressed;
                        extractionProgressedTokens.Add(exefsExtractionProgressedToken);
                    }
                    tasks.Add(partition.ExeFs.ExtractFiles(Path.Combine(directoryName, GetExeFsDirectoryName(i)), provider, exefsExtractionProgressedToken));
                }

                if (partition?.RomFs != null)
                {
                    ExtractionProgressedToken romfsExtractionProgressedToken = null;
                    if (romfsExtractionProgressedToken != null)
                    {
                        romfsExtractionProgressedToken = new ExtractionProgressedToken();
                        romfsExtractionProgressedToken.FileCountChanged += onExtractionTokenProgressed;
                        extractionProgressedTokens.Add(romfsExtractionProgressedToken);
                    }

                    tasks.Add(partition.RomFs.ExtractFiles(Path.Combine(directoryName, GetRomFsDirectoryName(i)), provider, romfsExtractionProgressedToken));
                }

            }

            await Task.WhenAll(tasks);

            if (progressReportToken != null)
            {
                progressReportToken.Progress = 1;
                progressReportToken.IsCompleted = true;
            }
        }

        private string GetRomFsDirectoryName(int partitionId)
        {
            // To-do: get this from a an options object
            switch (partitionId)
            {
                case 0:
                    return "RomFS";
                case 1:
                    return "Manual";
                case 2:
                    return "DownloadPlay";
                case 6:
                    return "N3DSUpdate";
                case 7:
                    return "O3DSUpdate";
                default:
                    return "RomFS-Partition-" + partitionId.ToString();
            }
        }

        private string GetExeFsDirectoryName(int partitionId)
        {
            // To-do: get this from a an options object
            switch (partitionId)
            {
                case 0:
                    return "ExeFS";
                default:
                    return "ExeFS-Partition-" + partitionId.ToString();
            }
        }

        #region Child Classes

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

        public class CartridgeNcsdHeader : NcsdHeader
        {
            public CartridgeNcsdHeader(byte[] header) : base(header)
            {
                ExheaderHash = new byte[0x20];
                Array.Copy(header, 0x160, ExheaderHash, 0, 0x20);

                AdditionalHeaderSize = BitConverter.ToInt32(header, 0x180);
                SectorZeroOffset = BitConverter.ToInt32(header, 0x184);
                PartitionFlags = BitConverter.ToInt64(header, 0x188);

                PartitionIdTable = new byte[64];
                Array.Copy(header, 0x190, PartitionIdTable, 0, 64);

                Reserved1 = new byte[64];
                Array.Copy(header, 0x1D0, Reserved1, 0, 0x20);

                Reserved2 = new byte[64];
                Array.Copy(header, 0x1F0, Reserved2, 0, 0xE);

                Unknown1 = header[0x1FE];
                Unknown2 = header[0x1FF];
                Card2SaveAddress = BitConverter.ToInt32(header, 0x200);
                CardInfoBitmask = BitConverter.ToInt32(header, 0x204);

                Reserved3 = new byte[0x108];
                Array.Copy(header, 0x208, Reserved3, 0, 0x108);

                TitleVersion = BitConverter.ToInt16(header, 0x310);
                CardRevision = BitConverter.ToInt16(header, 0x312);

                Reserved4 = new byte[0xCEE];
                Array.Copy(header, 0x314, Reserved4, 0, 0xCEE);

                CardSeedY = new byte[0x10];
                Array.Copy(header, 0x1000, CardSeedY, 0, 0x10);

                EncryptedCardSeed = new byte[0x10];
                Array.Copy(header, 0x1010, EncryptedCardSeed, 0, 0x10);

                CardSeedAESMAC = new byte[0x10];
                Array.Copy(header, 0x1010, CardSeedAESMAC, 0, 0x10);

                CardSeedNonce = new byte[0x10];
                Array.Copy(header, 0x1020, CardSeedNonce, 0, 0x10);

                Reserved5 = new byte[0xC4];
                Array.Copy(header, 0x103C, Reserved5, 0, 0xC4);

                FirstNcchHeader = new byte[0x100];
                Array.Copy(header, 0x1100, FirstNcchHeader, 0, 0x100);

                CardDeviceReserved1 = new byte[0x200];
                Array.Copy(header, 0x1200, CardDeviceReserved1, 0, 0x200);

                TitleKey = new byte[0x10];
                Array.Copy(header, 0x1400, TitleKey, 0, 0x10);

                CardDeviceReserved2 = new byte[0xF0];
                Array.Copy(header, 0x1410, CardDeviceReserved2, 0, 0xF0);
            }

            /// <summary>
            /// Exheader SHA-256 hash
            /// </summary>
            public byte[] ExheaderHash { get; private set; } // Offset: 0x160, Size: 0x20

            public int AdditionalHeaderSize { get; private set; } // Offset: 0x180, Size: 0x4

            public int SectorZeroOffset { get; private set; } // Offset: 0x184, Size: 0x4

            public long PartitionFlags { get; private set; } // Offset: 0x188, Size: 0x8

            public byte[] PartitionIdTable { get; private set; } // Offset: 0x190, Size: 8*8

            protected byte[] Reserved1 { get; set; } // Offset: 0x1D0, Size: 0x20

            // Documentation is unsure about the use of this
            protected byte[] Reserved2 { get; set; } // Offset: 0x1F0, Size: 0xE

            /// <summary>
            /// Support for this was implemented with 9.6.0-X FIRM.
            /// Bit0=1 enables using bits 1-2, it's unknown what these two bits are actually used for
            /// (the value of these two bits get compared with some other value during NCSD verification/loading).
            /// This appears to enable a new, likely hardware-based, antipiracy check on cartridges.
            /// </summary>
            protected byte Unknown1 { get; set; } // Offset: 0x1FE, Size: 1

            /// <summary>
            /// Support for this was implemented with 9.6.0-X FIRM, see docs regarding save crypto.
            /// </summary>
            protected byte Unknown2 { get; set; } // Offset: 0x1FF, Size: 1

            // To-Do: Move this to NcsdHeader maybe? Maybe not?

            /// <summary>
            /// Writable address of the CARD2 on-chip savedata, or -1 if the cartridge is CARD1
            /// </summary>
            public int Card2SaveAddress { get; private set; } // Offset: 0x200, Size: 4

            public int CardInfoBitmask { get; private set; } // Offset: 0x204, Size: 4

            // Called Reserved1 in 3dbrew
            protected byte[] Reserved3 { get; set; } // Offset: 0x208, size: 0x108

            public short TitleVersion { get; private set; } // Offset: 0x310, size: 2

            public short CardRevision { get; private set; } // Offset: 0x312, size: 2

            // Called Reserved2 in 3dbrew
            // 3dbrew lists same offset as Reserved1 (aka Reserved3). This offset is based on math and may be inaccurate.
            protected byte[] Reserved4 { get; set; } // Offset: 0x314, size: 0xCEE

            /// <summary>
            /// Card seed keyY (first u64 is Media ID (same as first NCCH partitionId))
            /// </summary>
            public byte[] CardSeedY { get; private set; } // Offset: 0x1000, size: 0x10

            /// <summary>
            /// Encrypted card seed (AES-CCM, keyslot 0x3B for retail cards, see CTRCARD_SECSEED)
            /// </summary>
            public byte[] EncryptedCardSeed { get; private set; } // Offset: 0x1010, size: 0x10

            /// <summary>
            /// Card seed AES-MAC
            /// </summary>
            public byte[] CardSeedAESMAC { get; private set; } // Offset: 0x1020, size: 0x10

            public byte[] CardSeedNonce { get; private set; } // Offset: 0x1030, size: 0xC

            // Called Reserved3 in 3dbrew
            protected byte[] Reserved5 { get; set; } // Offset: 0x103C, size: 0xC4

            /// <summary>
            /// Copy of first NCCH header (excluding RSA signature)
            /// </summary>
            protected byte[] FirstNcchHeader { get; set; } // Offset: 0x1100, Size: 0x100

            // "private headers" stored in this area

            public byte[] CardDeviceReserved1 { get; private set; } // Offset: 0x1200, Size: 0x200

            public byte[] TitleKey { get; private set; } // Offset: 0x1400, Size: 0x10

            public byte[] CardDeviceReserved2 { get; private set; } // Offset: 0x1410, Size: 0xF0
        }

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

        #endregion

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    RawData?.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~ThreeDsRom() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
