using SkyEditor.Core.IO;
using SkyEditor.Core.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DotNet3dsToolkit
{
    public class ThreeDsRom : IOpenableFile, IIOProvider,  IDisposable
    {
        private const int MediaUnitSize = 0x200;

        public ThreeDsRom()
        {
            (this as IIOProvider).ResetWorkingDirectory();
        }

        public NcsdHeader Header { get; set; }

        public NcchPartition[] Partitions { get; set; }

        private GenericFile RawData { get; set; }

        private IIOProvider CurrentIOProvider { get; set; }

        public async Task OpenFile(string filename, IIOProvider provider)
        {
            CurrentIOProvider = provider;

            if (provider.FileExists(filename))
            {
                // Clear virtual path if it exists
                if (!string.IsNullOrEmpty(VirtualPath) && provider.DirectoryExists(VirtualPath))
                {
                    provider.DeleteDirectory(VirtualPath);
                }

                VirtualPath = provider.GetTempDirectory();

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
                    var partitionStart = (long)Header.Partitions[i].Offset * MediaUnitSize;
                    var partitionLength = (long)Header.Partitions[i].Length * MediaUnitSize;
                    Partitions[i] = await NcchPartition.Load(new GenericFileReference(RawData, partitionStart, (int)partitionLength), i);
                }, 0, Header.Partitions.Length - 1);
            }
            else if (provider.DirectoryExists(filename))
            {
                VirtualPath = filename;
                DisposeVirtualPath = false;
            }
            else
            {
                throw new FileNotFoundException("Could not find file or directory at the given path", filename);
            }            
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

                if (partition?.ExHeader != null)
                {
                    ExtractionProgressedToken exefsExtractionProgressedToken = null;
                    if (exefsExtractionProgressedToken != null)
                    {
                        exefsExtractionProgressedToken = new ExtractionProgressedToken();
                        exefsExtractionProgressedToken.TotalFileCount = 1;
                        exefsExtractionProgressedToken.FileCountChanged += onExtractionTokenProgressed;
                        extractionProgressedTokens.Add(exefsExtractionProgressedToken);
                    }
                    tasks.Add(Task.Run(async () => {
                        File.WriteAllBytes(Path.Combine(directoryName, GetExHeaderFileName(i)), await partition.ExHeader.ReadAsync());
                        exefsExtractionProgressedToken.IncrementExtractedFileCount();
                    }));
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
            switch (partitionId)
            {
                case 0:
                    return "ExeFS";
                default:
                    return "ExeFS-Partition-" + partitionId.ToString();
            }
        }

        private string GetExHeaderFileName(int partitionId)
        {
            switch (partitionId)
            {
                case 0:
                    return "ExHeader.bin";
                default:
                    return "ExHeader" + partitionId.ToString() + ".bin";
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

        #region IIOProvider Implementation
        /// <summary>
        /// Gets a regular expression for the given search pattern for use with <see cref="GetFiles(string, string, bool)"/>.  Do not provide asterisks.
        /// </summary>
        private static StringBuilder GetFileSearchRegexQuestionMarkOnly(string searchPattern)
        {
            var parts = searchPattern.Split('?');
            var regexString = new StringBuilder();
            foreach (var item in parts)
            {
                regexString.Append(Regex.Escape(item));
                if (item != parts[parts.Length - 1])
                {
                    regexString.Append(".?");
                }
            }
            return regexString;
        }

        /// <summary>
        /// Gets a regular expression for the given search pattern for use with <see cref="GetFiles(string, string, bool)"/>.
        /// </summary>
        /// <param name="searchPattern"></param>
        /// <returns></returns>
        private static string GetFileSearchRegex(string searchPattern)
        {
            var asteriskParts = searchPattern.Split('*');
            var regexString = new StringBuilder();

            foreach (var part in asteriskParts)
            {
                if (string.IsNullOrEmpty(part))
                {
                    // Asterisk
                    regexString.Append(".*");
                }
                else
                {
                    regexString.Append(GetFileSearchRegexQuestionMarkOnly(part));
                }
            }

            return regexString.ToString();
        }

        /// <summary>
        /// Keeps track of files that have been logically deleted
        /// </summary>
        private List<string> BlacklistedPaths => new List<string>();

        /// <summary>
        /// Path in the current I/O provider where temporary files are stored
        /// </summary>
        private string VirtualPath { get; set; }

        /// <summary>
        /// Whether or not to delete <see cref="VirtualPath"/> on delete
        /// </summary>
        private bool DisposeVirtualPath { get; set; }

        string IIOProvider.WorkingDirectory
        {
            get
            {
                var path = new StringBuilder();
                foreach (var item in _workingDirectoryParts)
                {
                    if (!string.IsNullOrEmpty(item))
                    {
                        path.Append("/");
                        path.Append(item);
                    }
                }
                path.Append("/");
                return path.ToString();
            }
            set
            {
                _workingDirectoryParts = GetPathParts(value);
            }
        }
        private string[] _workingDirectoryParts;

        protected string[] GetPathParts(string path)
        {
            var parts = new List<string>();

            path = path.Replace('\\', '/');
            if (!path.StartsWith("/") && !(_workingDirectoryParts.Length == 1 && _workingDirectoryParts[0] == string.Empty))
            {
                parts.AddRange(_workingDirectoryParts);
            }

            foreach (var item in path.TrimStart('/').Split('/'))
            {
                switch (item)
                {
                    case "":
                    case ".":
                        break;
                    case "..":
                        parts.RemoveAt(parts.Count - 1);
                        break;
                    default:
                        parts.Add(item);
                        break;
                }
            }
            if (parts.Count == 0)
            {
                parts.Add(string.Empty);
            }
            return parts.ToArray();
        }

        void IIOProvider.ResetWorkingDirectory()
        {
            (this as IIOProvider).WorkingDirectory = "/";
        }

        private string FixPath(string path)
        {
            var fixedPath = path.Replace('\\', '/');

            // Apply working directory
            if (fixedPath.StartsWith("/"))
            {
                return fixedPath;
            }
            else
            {
                return Path.Combine((this as IIOProvider).WorkingDirectory, path);
            }
        }

        private string GetVirtualPath(string path)
        {
            return Path.Combine(VirtualPath, path.TrimStart('/'));
        }
        
        private GenericFileReference GetDataReference(string[] parts, bool throwIfNotFound = true)
        {
            GenericFileReference getExeFsDataReference(string[] pathParts, int partitionId)
            {
                if (pathParts.Length == 2)
                {
                    return Partitions[partitionId]?.ExeFs?.GetDataReference(pathParts.Last());
                }

                return null;
            }

            GenericFileReference getRomFsDataReference(string[] pathParts, int partitionId)
            {
                var currentDirectory = Partitions[partitionId]?.RomFs?.Level3.RootDirectoryMetadataTable;
                for (int i = 1; i < pathParts.Length - 1; i += 1)
                {
                    currentDirectory = currentDirectory?.ChildDirectories.Where(x => string.Compare(x.Name, pathParts[i], true) == 0).FirstOrDefault();
                }
                if (currentDirectory != null)
                {
                    if (ReferenceEquals(currentDirectory, Partitions[partitionId].RomFs.Level3.RootDirectoryMetadataTable))
                    {
                        // The root RomFS directory doesn't contain files; those are located in the level 3
                        return Partitions[partitionId].RomFs.Level3.RootFiles.FirstOrDefault(f => string.Compare(f.Name, pathParts.Last(), true) == 0)?.GetDataReference();
                    }
                    else
                    {
                        return currentDirectory.ChildFiles.FirstOrDefault(f => string.Compare(f.Name, pathParts.Last(), true) == 0)?.GetDataReference();
                    }
                    
                }

                return null;
            }

            GenericFileReference dataReference = null;

            var firstDirectory = parts[0].ToLower();
            switch (firstDirectory)
            {
                case "exefs":
                    dataReference = getExeFsDataReference(parts, 0);
                    break;
                case "romfs":
                    dataReference = getRomFsDataReference(parts, 0);
                    break;
                case "manual":
                    dataReference = getRomFsDataReference(parts, 1);
                    break;
                case "downloadplay":
                    dataReference = getRomFsDataReference(parts, 2);
                    break;
                case "n3dsupdate":
                    dataReference = getRomFsDataReference(parts, 6);
                    break;
                case "o3dsupdate":
                    dataReference = getRomFsDataReference(parts, 7);
                    break;
                case "exheader.bin":
                    dataReference = Partitions[0]?.ExHeader;
                    break;
            }

            if (dataReference != null)
            {
                return dataReference;
            }

            if (throwIfNotFound)
            {
                var path = "/" + string.Join("/", parts);
                throw new FileNotFoundException(string.Format(Properties.Resources.ThreeDsRom_ErrorRomFileNotFound, path), path);
            }
            else
            {
                return null;
            }
        }

        long IIOProvider.GetFileLength(string filename)
        {
            return GetDataReference(GetPathParts(filename)).Length;
        }

        bool IIOProvider.FileExists(string filename)
        {
            return (CurrentIOProvider != null && CurrentIOProvider.FileExists(GetVirtualPath(filename)))
                || GetDataReference(GetPathParts(filename), false) != null;
        }

        private bool DirectoryExists(string[] parts)
        {
            bool romfsDirectoryExists(string[] pathParts, int partitionId)
            {
                var currentDirectory = Partitions[partitionId]?.RomFs?.Level3.RootDirectoryMetadataTable;
                for (int i = 1; i < pathParts.Length - 1; i += 1)
                {
                    currentDirectory = currentDirectory?.ChildDirectories.Where(x => string.Compare(x.Name, pathParts[i], true) == 0).FirstOrDefault();
                }
                return currentDirectory != null;
            }

            if (parts.Length == 1)
            {
                switch (parts[0].ToLower())
                {
                    case "exefs":
                        return Partitions[0]?.ExeFs != null;
                    case "romfs":
                        return Partitions[0]?.RomFs != null;
                    case "manual":
                        return Partitions[1]?.RomFs != null;
                    case "downloadplay":
                        return Partitions[2]?.RomFs != null;
                    case "n3dsupdate":
                        return Partitions[6]?.RomFs != null;
                    case "o3dsupdate":
                        return Partitions[7]?.RomFs != null;
                    default:
                        return false;
                }
            }
            else if (parts.Length == 0)
            {
                throw new ArgumentException("Argument cannot be empty", nameof(parts));
            }
            else
            {
                switch (parts[0].ToLower())
                {
                    case "exefs":
                        // Directories inside exefs are not supported
                        return false;
                    case "romfs":
                        return romfsDirectoryExists(parts, 0);
                    case "manual":
                        return romfsDirectoryExists(parts, 1);
                    case "downloadplay":
                        return romfsDirectoryExists(parts, 2);
                    case "n3dsupdate":
                        return romfsDirectoryExists(parts, 6);
                    case "o3dsupdate":
                        return romfsDirectoryExists(parts, 7);
                    default:
                        return false;
                }
            }
        }

        bool IIOProvider.DirectoryExists(string path)
        {
            return !BlacklistedPaths.Contains(FixPath(path))
                    &&
                    ((CurrentIOProvider != null && CurrentIOProvider.DirectoryExists(GetVirtualPath(path)))
                        || DirectoryExists(GetPathParts(path))
                    );
        }

        void IIOProvider.CreateDirectory(string path)
        {
            var fixedPath = FixPath(path);
            if (BlacklistedPaths.Contains(fixedPath))
            {
                BlacklistedPaths.Remove(fixedPath);
            }

            if (!(this as IIOProvider).DirectoryExists(fixedPath))
            {
                CurrentIOProvider?.CreateDirectory(GetVirtualPath(fixedPath));
            }
        }

        string[] IIOProvider.GetFiles(string path, string searchPattern, bool topDirectoryOnly)
        {
            var searchPatternRegex = new Regex(GetFileSearchRegex(searchPattern), RegexOptions.Compiled | RegexOptions.IgnoreCase);
            var parts = GetPathParts(path);
            var output = new List<string>();

            void addRomFsFiles(int partitionId) 
            {
                var directory = "/" + GetRomFsDirectoryName(partitionId) + "/";

                var currentDirectory = Partitions[partitionId]?.RomFs?.Level3.RootDirectoryMetadataTable;
                for (int i = 1; i < parts.Length; i += 1)
                {
                    currentDirectory = currentDirectory?.ChildDirectories.Where(x => string.Compare(x.Name, parts[i], true) == 0).FirstOrDefault();
                    directory += currentDirectory.Name + "/";
                }

                if (currentDirectory != null)
                {
                    IEnumerable<string> files;
                    if (ReferenceEquals(currentDirectory, Partitions[partitionId].RomFs.Level3.RootDirectoryMetadataTable)) {
                        // The root RomFS directory doesn't contain files; those are located in the level 3
                        files = Partitions[partitionId].RomFs.Level3.RootFiles
                        .Where(f => searchPatternRegex.IsMatch(f.Name))
                        .Select(f => directory + f.Name);
                    }
                    else
                    {
                        files = currentDirectory.ChildFiles
                        .Where(f => searchPatternRegex.IsMatch(f.Name))
                        .Select(f => directory + f.Name);
                    }
                    
                    output.AddRange(files);

                    if (!topDirectoryOnly)
                    {
                        foreach (var d in currentDirectory.ChildDirectories)
                        {
                            output.AddRange((this as IIOProvider).GetFiles(directory + d.Name + "/", searchPattern, topDirectoryOnly));
                        }
                    }
                }
            }

            switch (parts[0].ToLower())
            {
                case "" when parts.Length == 1:
                    if (Partitions[0]?.ExHeader != null && searchPatternRegex.IsMatch("ExHeader.bin"))
                    {
                        output.Add("ExHeader.bin");
                    }
                    if (!topDirectoryOnly)
                    {
                        output.AddRange((this as IIOProvider).GetFiles("/ExeFS", searchPattern, topDirectoryOnly));
                        output.AddRange((this as IIOProvider).GetFiles("/RomFS", searchPattern, topDirectoryOnly));
                        output.AddRange((this as IIOProvider).GetFiles("/Manual", searchPattern, topDirectoryOnly));
                        output.AddRange((this as IIOProvider).GetFiles("/N3DSUpdate", searchPattern, topDirectoryOnly));
                        output.AddRange((this as IIOProvider).GetFiles("/O3DSUpdate", searchPattern, topDirectoryOnly));
                    }
                    break;
                case "exefs" when parts.Length == 1:
                    foreach (var file in Partitions[0]?.ExeFs?.Headers
                        ?.Where(h => searchPatternRegex.IsMatch(h.Filename) && !string.IsNullOrWhiteSpace(h.Filename))
                        ?.Select(h => h.Filename))
                    {
                        output.Add("/ExeFS/" + file);
                    }
                    break;
                case "romfs":
                    addRomFsFiles(0);
                    break;
                case "manual":
                    addRomFsFiles(1);
                    break;
                case "downloadplay":
                    addRomFsFiles(2);
                    break;
                case "n3dsupdate":
                    addRomFsFiles(6);
                    break;
                case "o3dsupdate":
                    addRomFsFiles(7);
                    break;
            }

            // Apply shadowed files
            var virtualPath = GetVirtualPath(path);
            if (CurrentIOProvider != null && CurrentIOProvider.DirectoryExists(virtualPath))
            {
                foreach (var item in CurrentIOProvider.GetFiles(virtualPath, searchPattern, topDirectoryOnly))
                {
                    var overlayPath = "/" + FileSystem.MakeRelativePath(item, VirtualPath);
                    if (!BlacklistedPaths.Contains(overlayPath) && !output.Contains(overlayPath, StringComparer.OrdinalIgnoreCase))
                    {
                        output.Add(overlayPath);
                    }
                }
            }

            return output.ToArray();
        }

        string[] IIOProvider.GetDirectories(string path, bool topDirectoryOnly)
        {
            var parts = GetPathParts(path);
            var output = new List<string>();

            void addRomFsDirectories(int partitionId)
            {
                var directory = "/" + GetRomFsDirectoryName(partitionId) + "/";

                var currentDirectory = Partitions[partitionId]?.RomFs?.Level3.RootDirectoryMetadataTable;
                for (int i = 1; i < parts.Length; i += 1)
                {
                    currentDirectory = currentDirectory?.ChildDirectories.Where(x => string.Compare(x.Name, parts[i], true) == 0).FirstOrDefault();
                    directory += currentDirectory.Name + "/";
                }
                if (currentDirectory != null)
                {
                    var dirs = currentDirectory.ChildDirectories
                        .Select(f => directory + f.Name + "/");
                    output.AddRange(dirs);

                    if (!topDirectoryOnly)
                    {
                        foreach (var d in currentDirectory.ChildDirectories)
                        {
                            output.AddRange((this as IIOProvider).GetDirectories(directory + d.Name + "/", topDirectoryOnly));
                        }
                    }
                }
            }

            switch (parts[0].ToLower())
            {
                case "" when parts.Length == 1:
                    if (Partitions[0]?.ExeFs != null)
                    {
                        output.Add("/ExeFS/");
                    }
                    if (Partitions[0]?.RomFs != null)
                    {
                        output.Add("/RomFS/");
                    }
                    if (Partitions[1]?.RomFs != null)
                    {
                        output.Add("/Manual/");
                    }
                    if (Partitions[2]?.RomFs != null)
                    {
                        output.Add("/DownloadPlay/");
                    }
                    if (Partitions[6]?.RomFs != null)
                    {
                        output.Add("/N3DSUpdate/");
                    }
                    if (Partitions[7]?.RomFs != null)
                    {
                        output.Add("/O3DSUpdate/");
                    }

                    if (!topDirectoryOnly)
                    {
                        output.AddRange((this as IIOProvider).GetDirectories("/ExeFS", topDirectoryOnly));
                        output.AddRange((this as IIOProvider).GetDirectories("/RomFS", topDirectoryOnly));
                        output.AddRange((this as IIOProvider).GetDirectories("/Manual", topDirectoryOnly));
                        output.AddRange((this as IIOProvider).GetDirectories("/N3DSUpdate", topDirectoryOnly));
                        output.AddRange((this as IIOProvider).GetDirectories("/O3DSUpdate", topDirectoryOnly));
                    }
                    break;
                case "exefs" when parts.Length == 1:
                    // ExeFs doesn't support directories
                    break;
                case "romfs":
                    addRomFsDirectories(0);
                    break;
                case "manual":
                    addRomFsDirectories(1);
                    break;
                case "downloadplay":
                    addRomFsDirectories(2);
                    break;
                case "n3dsupdate":
                    addRomFsDirectories(6);
                    break;
                case "o3dsupdate":
                    addRomFsDirectories(7);
                    break;
            }

            // Apply shadowed files
            var virtualPath = GetVirtualPath(path);
            if (CurrentIOProvider != null && CurrentIOProvider.DirectoryExists(virtualPath))
            {
                foreach (var item in CurrentIOProvider.GetDirectories(virtualPath, topDirectoryOnly))
                {
                    var overlayPath = "/" + FileSystem.MakeRelativePath(item, VirtualPath);
                    if (!BlacklistedPaths.Contains(overlayPath) && !output.Contains(overlayPath, StringComparer.OrdinalIgnoreCase))
                    {
                        output.Add(overlayPath);
                    }
                }
            }

            return output.ToArray();
        }

        byte[] IIOProvider.ReadAllBytes(string filename)
        {
            var fixedPath = FixPath(filename);
            if (BlacklistedPaths.Contains(fixedPath))
            {
                throw new FileNotFoundException(string.Format(Properties.Resources.ThreeDsRom_ErrorRomFileNotFound, filename), filename);
            }
            else
            {
                var virtualPath = GetVirtualPath(fixedPath);
                if (CurrentIOProvider != null && CurrentIOProvider.FileExists(virtualPath))
                {
                    return CurrentIOProvider.ReadAllBytes(virtualPath);
                }
                else
                {
                    var data = GetDataReference(GetPathParts(filename));
                    return data.Read();
                }
            }
        }

        string IIOProvider.ReadAllText(string filename)
        {
            return Encoding.UTF8.GetString((this as IIOProvider).ReadAllBytes(filename));
        }

        void IIOProvider.WriteAllBytes(string filename, byte[] data)
        {
            var fixedPath = FixPath(filename);
            if (BlacklistedPaths.Contains(fixedPath))
            {
                BlacklistedPaths.Remove(fixedPath);
            }

            CurrentIOProvider?.WriteAllBytes(GetVirtualPath(filename), data);
        }

        void IIOProvider.WriteAllText(string filename, string data)
        {
            (this as IIOProvider).WriteAllBytes(filename, Encoding.UTF8.GetBytes(data));
        }

        void IIOProvider.CopyFile(string sourceFilename, string destinationFilename)
        {
            (this as IIOProvider).WriteAllBytes(destinationFilename, (this as IIOProvider).ReadAllBytes(sourceFilename));
        }

        void IIOProvider.DeleteFile(string filename)
        {
            var fixedPath = FixPath(filename);
            if (!BlacklistedPaths.Contains(fixedPath))
            {
                BlacklistedPaths.Add(fixedPath);
            }

            var virtualPath = GetVirtualPath(filename);
            if (CurrentIOProvider != null && CurrentIOProvider.FileExists(virtualPath))
            {
                CurrentIOProvider.DeleteFile(virtualPath);
            }
        }

        void IIOProvider.DeleteDirectory(string path)
        {
            var fixedPath = FixPath(path);
            if (!BlacklistedPaths.Contains(fixedPath))
            {
                BlacklistedPaths.Add(fixedPath);
            }

            var virtualPath = GetVirtualPath(path);
            if (CurrentIOProvider != null && CurrentIOProvider.FileExists(virtualPath))
            {
                CurrentIOProvider.DeleteFile(virtualPath);
            }
        }

        string IIOProvider.GetTempFilename()
        {
            var path = "/temp/files/" + Guid.NewGuid().ToString();
            (this as IIOProvider).WriteAllBytes(path, new byte[] { });
            return path;
        }

        string IIOProvider.GetTempDirectory()
        {
            var path = "/temp/dirs/" + Guid.NewGuid().ToString();
            (this as IIOProvider).CreateDirectory(path);
            return path;
        }

        Stream IIOProvider.OpenFile(string filename)
        {
            if (CurrentIOProvider != null)
            {
                throw new NotSupportedException("Cannot open a file as a stream without an IO provider.");
            }

            var virtualPath = GetVirtualPath(filename);
            if (!CurrentIOProvider.DirectoryExists(virtualPath))
            {
                CurrentIOProvider.CreateDirectory(virtualPath);
            }
            CurrentIOProvider.WriteAllBytes(virtualPath, (this as IIOProvider).ReadAllBytes(filename));

            return CurrentIOProvider.OpenFile(filename);
        }

        Stream IIOProvider.OpenFileReadOnly(string filename)
        {
            if (CurrentIOProvider != null)
            {
                throw new NotSupportedException("Cannot open a file as a stream without an IO provider.");
            }

            var virtualPath = GetVirtualPath(filename);
            if (!CurrentIOProvider.DirectoryExists(virtualPath))
            {
                CurrentIOProvider.CreateDirectory(virtualPath);
            }
            CurrentIOProvider.WriteAllBytes(virtualPath, (this as IIOProvider).ReadAllBytes(filename));

            return CurrentIOProvider.OpenFileReadOnly(filename);
        }

        Stream IIOProvider.OpenFileWriteOnly(string filename)
        {
            if (CurrentIOProvider != null)
            {
                throw new NotSupportedException("Cannot open a file as a stream without an IO provider.");
            }

            var virtualPath = GetVirtualPath(filename);
            if (!CurrentIOProvider.DirectoryExists(virtualPath))
            {
                CurrentIOProvider.CreateDirectory(virtualPath);
            }
            CurrentIOProvider.WriteAllBytes(virtualPath, (this as IIOProvider).ReadAllBytes(filename));

            return CurrentIOProvider.OpenFileWriteOnly(filename);
        }

        #endregion
    }
}
