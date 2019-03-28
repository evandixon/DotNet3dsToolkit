using DotNet3dsToolkit.Extensions;
using DotNet3dsToolkit.Infrastructure;
using SkyEditor.IO;
using SkyEditor.IO.Binary;
using SkyEditor.IO.FileSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNet3dsToolkit.Ctr
{
    public class RomFs : IDisposable
    {
        /// <summary>
        /// Arbitrary upper bound of a filename that DotNet3dsToolkit will attempt to read, to prevent hogging all memory if there's a problem
        /// </summary>
        const int MaxFilenameLength = 1000;

        public static async Task<bool> IsRomFs(IReadOnlyBinaryDataAccessor file)
        {
            try
            {
                if (file.Length < 4)
                {
                    return false;
                }

                return await file.ReadStringAsync(0, 4, Encoding.ASCII) == "IVFC";
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Loads an existing ROM file system from the given data.
        /// </summary>
        /// <param name="data">Accessor to the raw data to load as a ROM file system</param>
        /// <returns>The ROM file system the given data represents</returns>
        public static async Task<RomFs> Load(IReadOnlyBinaryDataAccessor data)
        {
            var header = new RomFsHeader(await data.ReadArrayAsync(0, 0x6B));
            var romfs = new RomFs(data, header);
            await romfs.Initialize();
            return romfs;
        }

        /// <summary>
        /// Builds a new ROM file system from the given directory
        /// </summary>
        /// <param name="directory">Directory from which to load the files</param>
        /// <param name="fileSystem">File system from which to load the files</param>
        /// <returns>A newly built ROM file system</returns>
        public static async Task<RomFs> Build(string directory, IFileSystem fileSystem, ExtractionProgressedToken progressToken = null)
        {
            Stream stream = null;
            string tempFilename = null;
            try
            {
                // A memory stream is faster, but an internal limitation means it's unsuitable for files larger than 2GB
                // We'll fall back to a file stream if our raw data won't fit within 2GB minus 400 MB (for safety, since there's still metadata, hashes, and other partitions)
                if (fileSystem.GetDirectoryLength(directory) < (int.MaxValue - (400 * Math.Pow(1024, 2))))
                {
                    stream = new MemoryStream();
                }
                else
                {
                    // Do not use IFileSystem; this is for temporary storage since RAM isn't an option
                    tempFilename = Path.GetTempFileName();
                    stream = File.Open(tempFilename, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                }

                RomFsBuilder.RomFsBuilder.BuildRomFS(directory, fileSystem, stream, progressToken);

                BinaryFile data;
                if (stream is FileStream)
                {
                    // We want the BinaryFile class to own the file so it will dispose of it properly
                    // So let's dispose our copy and let it re-open it however it sees fit
                    // And instead of a BinaryFile, use a FileDeletingBinaryFile to delete the file on dispose

                    stream.Dispose();
                    stream = null;

                    if (string.IsNullOrEmpty(tempFilename))
                    {
                        // The developer (probably me) made a mistake
                        throw new Exception("Temporary file not found");
                    }

                    data = new FileDeletingBinaryFile(tempFilename);
                }
                else if (stream is MemoryStream memoryStream)
                {
                    try
                    {
                        // Faster but maybe more memory-intensive
                        var memoryStreamArray = memoryStream.ToArray();
                        data = new BinaryFile(memoryStreamArray);
                    }
                    catch (OutOfMemoryException)
                    {
                        // Slower but more reliable
                        data = new BinaryFile(memoryStream);
                    }                    
                }
                else
                {
                    // The developer (probably me) made a mistake
                    throw new Exception("Unexpected type of stream in RomFs.Build");
                }                
                
                var header = new RomFsHeader(await data.ReadArrayAsync(0, 0x6B));
                var romFs = new RomFs(data, header);
                await romFs.Initialize();
                return romFs;
            }
            finally
            {
                stream?.Dispose();
            }
        }

        /// <param name="data">The raw data. Note: This will be disposed when RomFs is disposed</param>
        public RomFs(IReadOnlyBinaryDataAccessor data, RomFsHeader header)
        {
            Data = data ?? throw new ArgumentNullException(nameof(data));
            Header = header ?? throw new ArgumentNullException(nameof(header));

            LevelLocations = new IvfcLevelLocation[]
            {
                new IvfcLevelLocation
                {
                    HashBlockSize = 1 << header.Level1BlockSize,
                    HashOffset = 0x60
                },
                new IvfcLevelLocation
                {
                    HashBlockSize = 1 << header.Level2BlockSize
                },
                new IvfcLevelLocation
                {
                    HashBlockSize = 1 << header.Level3BlockSize
                }
            };

            BodyOffset = BitMath.Align(LevelLocations[0].HashOffset + header.MasterHashSize, LevelLocations[2].HashBlockSize);
            BodySize = header.Level3HashDataSize;

            LevelLocations[2].DataOffset = BodyOffset;
            LevelLocations[2].DataSize = BitMath.Align(BodySize, LevelLocations[2].HashBlockSize);

            LevelLocations[1].HashOffset = BitMath.Align(BodyOffset + BodySize, LevelLocations[2].HashBlockSize);
            LevelLocations[2].HashOffset = LevelLocations[1].HashOffset + header.Level2LogicalOffset - header.Level1LogicalOffset;

            LevelLocations[1].DataOffset = LevelLocations[2].HashOffset;
            LevelLocations[1].DataSize = BitMath.Align(header.Level2HashDataSize, LevelLocations[1].HashBlockSize);

            LevelLocations[0].DataOffset = LevelLocations[2].HashOffset;
            LevelLocations[0].DataSize = BitMath.Align(header.Level1HashDataSize, LevelLocations[0].HashBlockSize);

            // To-do: verify hashes
        }

        public async Task Initialize()
        {
            Level3 = await IvfcLevel.Load(Data, LevelLocations[2]);
        }

        public IReadOnlyBinaryDataAccessor Data { get; }

        public RomFsHeader Header { get; }

        private IvfcLevelLocation[] LevelLocations { get; }

        public IvfcLevel Level3 { get; private set; }

        private long BodyOffset { get; }

        private long BodySize { get; }

        public async Task ExtractFiles(string directoryName, IFileSystem fileSystem, ExtractionProgressedToken progressReportToken = null)
        {
            if (progressReportToken != null)
            {
                progressReportToken.TotalFileCount = Level3.RootFiles.Length + Level3.RootDirectoryMetadataTable.CountChildFiles();
            }

            if (!fileSystem.DirectoryExists(directoryName))
            {
                fileSystem.CreateDirectory(directoryName);
            }

            async Task extractDirectory(DirectoryMetadata dir, string subDirectory)
            {
                var destDirectory = Path.Combine(subDirectory, dir.Name);
                if (!fileSystem.DirectoryExists(destDirectory))
                {
                    fileSystem.CreateDirectory(destDirectory);
                }
                
                await Task.WhenAll(
                    Task.WhenAll(dir.ChildFiles.Select(async f =>
                    {
                        fileSystem.WriteAllBytes(Path.Combine(destDirectory, f.Name), await f.GetDataReference().ReadArrayAsync());
                        if (progressReportToken != null)
                        {
                            progressReportToken.IncrementExtractedFileCount();
                        }
                    })),
                    Task.WhenAll(dir.ChildDirectories.Select(async d => {
                        await extractDirectory(d, destDirectory);
                    }))
                );
            }

            var directoryExtractTasks = Level3
                .RootDirectoryMetadataTable
                .ChildDirectories
                .Select(d => extractDirectory(d, directoryName))
                .ToList();

            var fileExtractTasks = Level3
                .RootFiles
                .Select(async f => fileSystem.WriteAllBytes(
                                        Path.Combine(directoryName, f.Name),
                                        await f.GetDataReference().ReadArrayAsync()))
                .ToList();

            await Task.WhenAll(directoryExtractTasks);
            await Task.WhenAll(fileExtractTasks);
        }

        public void Dispose()
        {
            if (Data is IDisposable disposableData)
            {
                disposableData.Dispose();
                disposableData = null;
            }   
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

        public class DirectoryMetadata
        {
            public static async Task<DirectoryMetadata> Load(IReadOnlyBinaryDataAccessor data, IvfcLevelHeader header, int offsetOffDirTable)
            {
                var offset = header.DirectoryMetadataTableOffset + offsetOffDirTable;
                var metadata = new DirectoryMetadata(data, header);
                metadata.ParentDirectoryOffset = await data.ReadInt32Async(offset + 0);
                metadata.SiblingDirectoryOffset = await data.ReadInt32Async(offset + 4);
                metadata.FirstChildDirectoryOffset = await data.ReadInt32Async(offset + 8);
                metadata.FirstFileOffset = await data.ReadInt32Async(offset + 0xC);
                metadata.NextDirectoryOffset = await data.ReadInt32Async(offset + 0x10);
                metadata.NameLength = await data.ReadInt32Async(offset + 0x14);
                if (metadata.NameLength > 0)
                {
                    metadata.Name = Encoding.Unicode.GetString(await data.ReadArrayAsync(offset + 0x18, Math.Min(metadata.NameLength, MaxFilenameLength)));
                }
                
                await Task.WhenAll(
                    metadata.LoadChildDirectories(),
                    metadata.LoadChildFiles()
                );

                return metadata;
            }

            public DirectoryMetadata(IReadOnlyBinaryDataAccessor data, IvfcLevelHeader header)
            {
                LevelData = data ?? throw new ArgumentNullException(nameof(data));
                IvfcLevelHeader = header ?? throw new ArgumentNullException(nameof(data));
            }

            private IReadOnlyBinaryDataAccessor LevelData { get; }
            private IvfcLevelHeader IvfcLevelHeader { get; }

            /// <summary>
            /// Offset of Parent Directory (self if Root)
            /// </summary>
            public int ParentDirectoryOffset { get; set; } // Offset: 0x0

            /// <summary>
            /// Offset of next Sibling Directory
            /// </summary>
            public int SiblingDirectoryOffset { get; set; } // Offset: 0x4

            /// <summary>
            /// Offset of first Child Directory (Subdirectory)
            /// </summary>
            public int FirstChildDirectoryOffset { get; set; } // Offset: 0x8

            /// <summary>
            /// Offset of first File (in File Metadata Table)
            /// </summary>
            public int FirstFileOffset { get; set; } // Offset: 0xC

            /// <summary>
            /// Offset of next Directory in the same Hash Table bucket
            /// </summary>
            public int NextDirectoryOffset { get; set; } // Offset: 0x10

            /// <summary>
            /// Name Length
            /// </summary>
            public int NameLength { get; set; } // Offset: 0x14

            /// <summary>
            /// Directory Name (Unicode)
            /// </summary>
            public string Name { get; set; }

            public List<DirectoryMetadata> ChildDirectories { get; set; }

            public List<FileMetadata> ChildFiles { get; set; }

            public async Task LoadChildDirectories()
            {
                ChildDirectories = new List<DirectoryMetadata>();

                if (FirstChildDirectoryOffset > 0)
                {
                    var currentChild = await DirectoryMetadata.Load(LevelData, IvfcLevelHeader, FirstChildDirectoryOffset);
                    ChildDirectories.Add(currentChild);
                    while (currentChild.SiblingDirectoryOffset > 0)
                    {
                        currentChild = await DirectoryMetadata.Load(LevelData, IvfcLevelHeader, currentChild.SiblingDirectoryOffset);
                        ChildDirectories.Add(currentChild);
                    }
                }                
            }

            public async Task LoadChildFiles()
            {
                ChildFiles = new List<FileMetadata>();
                if (FirstFileOffset > 0)
                {
                    var currentChild = await FileMetadata.Load(LevelData, IvfcLevelHeader, FirstFileOffset);
                    ChildFiles.Add(currentChild);
                    while (currentChild.NextSiblingFileOffset > 0)
                    {
                        currentChild = await FileMetadata.Load(LevelData, IvfcLevelHeader, currentChild.NextSiblingFileOffset);
                        ChildFiles.Add(currentChild);
                    }
                }
            }

            public int CountChildFiles()
            {
                return ChildFiles.Count + ChildDirectories.Select(d => d.CountChildFiles()).Sum();
            }

            public override string ToString()
            {
                return !string.IsNullOrEmpty(Name) ? $"RomFs Directory Metadata: {Name}" : "RomFs Directory Metadata (No Name)";
            }
        }

        public class FileMetadata
        {
            public static async Task<FileMetadata> Load(IReadOnlyBinaryDataAccessor data, IvfcLevelHeader header, long offsetFromMetadataTable)
            {
                var offset = header.FileMetadataTableOffset + offsetFromMetadataTable;
                var metadata = new FileMetadata(data, header);
                metadata.ContainingDirectoryOffset = await data.ReadInt32Async(offset + 0);
                metadata.NextSiblingFileOffset = await data.ReadInt32Async(offset + 4);
                metadata.FileDataOffset = await data.ReadInt64Async(offset + 8);
                metadata.FileDataLength = await data.ReadInt64Async(offset + 0x10);
                metadata.NextFileOffset = await data.ReadInt32Async(offset + 0x18);
                metadata.NameLength = await data.ReadInt32Async(offset + 0x1C);
                if (metadata.NameLength > 0)
                {
                    metadata.Name = Encoding.Unicode.GetString(await data.ReadArrayAsync(offset + 0x20, Math.Min(metadata.NameLength, MaxFilenameLength)));
                }
                return metadata;
            }

            public FileMetadata(IReadOnlyBinaryDataAccessor data, IvfcLevelHeader header)
            {
                LevelData = data ?? throw new ArgumentNullException(nameof(data));
                Header = header ?? throw new ArgumentNullException(nameof(header));
            }

            private IReadOnlyBinaryDataAccessor LevelData { get; }

            public IvfcLevelHeader Header { get; }

            /// <summary>
            /// Offset of Containing Directory (within Directory Metadata Table)
            /// </summary>
            public int ContainingDirectoryOffset { get; set; } // Offset: 0x0

            /// <summary>
            /// Offset of next Sibling File
            /// </summary>
            public int NextSiblingFileOffset { get; set; } // Offset: 0x4

            /// <summary>
            /// Offset of File's Data
            /// </summary>
            public long FileDataOffset { get; set; } // Offset: 0x8

            /// <summary>
            /// Length of File's Data
            /// </summary>
            public long FileDataLength { get; set; } // Offset: 0x10

            /// <summary>
            /// Offset of next File in the same Hash Table bucket
            /// </summary>
            public int NextFileOffset { get; set; } // Offset: 0x18

            /// <summary>
            /// Name Length
            /// </summary>
            public int NameLength { get; set; } // Offset: 0x1C

            /// <summary>
            /// File Name (Unicode)
            /// </summary>
            public string Name { get; set; } // Offset: 0x20

            public IReadOnlyBinaryDataAccessor GetDataReference()
            {
                return LevelData.GetReadOnlyDataReference(Header.FileDataOffset + FileDataOffset, FileDataLength);
            }

            public override string ToString()
            {
                return !string.IsNullOrEmpty(Name) ? $"RomFs File Metadata: {Name}" : "RomFs File Metadata (No Name)";
            }
        }

        public class IvfcLevelHeader
        {
            public IvfcLevelHeader(byte[] header)
            {
                if (header == null)
                {
                    throw new ArgumentNullException(nameof(header));
                }

                if (header.Length < 0x28)
                {
                    throw new ArgumentException(string.Format(Properties.Resources.BufferUnderflow, 0x28.ToString()), nameof(header));
                }

                Length = BitConverter.ToInt32(header, 0);
                DirectoryHashTableOffset = BitConverter.ToInt32(header, 4);
                DirectoryHashTableLength = BitConverter.ToInt32(header, 8);
                DirectoryMetadataTableOffset = BitConverter.ToInt32(header, 0xC);
                DirectoryMetadataTableLength = BitConverter.ToInt32(header, 0x10);
                FileHashTableOffset = BitConverter.ToInt32(header, 0x14);
                FileHashTableLength = BitConverter.ToInt32(header, 0x18);
                FileMetadataTableOffset = BitConverter.ToInt32(header, 0x1C);
                FileMetadataTableLength = BitConverter.ToInt32(header, 0x20);
                FileDataOffset = BitConverter.ToInt32(header, 0x24);
            }

            public int Length { get; set; } // Offset: 0x0
            public int DirectoryHashTableOffset { get; set; } // Offset: 0x4
            public int DirectoryHashTableLength { get; set; } // Offset: 0x8
            public int DirectoryMetadataTableOffset { get; set; } // Offset: 0xC
            public int DirectoryMetadataTableLength { get; set; } // Offset: 0x10
            public int FileHashTableOffset { get; set; } // Offset: 0x14
            public int FileHashTableLength { get; set; } // Offset: 0x18
            public int FileMetadataTableOffset { get; set; } // Offset: 0x1C
            public int FileMetadataTableLength { get; set; } // Offset: 0x20
            public int FileDataOffset { get; set; } // Offset: 0x24
        }

        /// <summary>
        /// Calculated properties used to find the location of a <see cref="IvfcLevel"/>.
        /// </summary>
        /// <remarks>
        /// Unlike most other child classes here, this does not represent a physical data structure.
        /// </remarks>
        public class IvfcLevelLocation
        {
            public long DataOffset { get; set; }
            public long DataSize { get; set; }
            public long HashOffset { get; set; }
            public int HashBlockSize { get; set; }

            /// <summary>
            /// A boolean indicating whether the hashes are good, or null of they have not been checked
            /// </summary>
            public bool? HashCheck { get; set; }
        }

        public class IvfcLevel
        {
            public static async Task<IvfcLevel> Load(IReadOnlyBinaryDataAccessor romfsData, IvfcLevelLocation location)
            {
                var header = new IvfcLevelHeader(await romfsData.ReadArrayAsync(location.DataOffset, 0x28));
                var level = new IvfcLevel(romfsData.GetReadOnlyDataReference(location.DataOffset, location.DataSize), header);
                await level.Initialize();
                return level;
            }

            public IvfcLevel(IReadOnlyBinaryDataAccessor data, IvfcLevelHeader header)
            {
                LevelData = data ?? throw new ArgumentNullException(nameof(data));
                Header = header ?? throw new ArgumentNullException(nameof(header));
            }

            public async Task Initialize()
            {
                DirectoryHashKeyTable = await LevelData.ReadArrayAsync(Header.DirectoryHashTableOffset, Header.DirectoryHashTableLength);
                RootDirectoryMetadataTable = await DirectoryMetadata.Load(LevelData, Header, 0);
                FileHashKeyTable = await LevelData.ReadArrayAsync(Header.FileHashTableOffset, Header.FileHashTableLength);

                var rootFiles = new List<FileMetadata>();
                var currentRootFile = await FileMetadata.Load(LevelData, Header, 0);
                if (currentRootFile.Name.Length > 0)
                {
                    rootFiles.Add(currentRootFile);
                    while (currentRootFile.NextSiblingFileOffset > 0)
                    {
                        currentRootFile = await FileMetadata.Load(LevelData, Header, currentRootFile.NextSiblingFileOffset);
                        rootFiles.Add(currentRootFile);
                    }
                }
                RootFiles = rootFiles.ToArray();
            }

            private IReadOnlyBinaryDataAccessor LevelData { get; }

            public IvfcLevelHeader Header { get; } // Offset: 0, size: 0x28

            public byte[] DirectoryHashKeyTable { get; private set; }
            public DirectoryMetadata RootDirectoryMetadataTable { get; private set; }
            public byte[] FileHashKeyTable { get; private set; }
            public FileMetadata[] RootFiles { get; private set; }

            /// <remarks>
            /// Source code: https://www.3dbrew.org/wiki/RomFS
            /// </remarks>
            private static uint GetHashTableLength(uint numEntries)
            {
                uint count = numEntries;
                if (numEntries < 3)
                    count = 3;
                else if (numEntries < 19)
                    count |= 1;
                else
                {
                    while (count % 2 == 0
                        || count % 3 == 0
                        || count % 5 == 0
                        || count % 7 == 0
                        || count % 11 == 0
                        || count % 13 == 0
                        || count % 17 == 0)
                    {
                        count++;
                    }
                }
                return count;
            }

            /// <remarks>
            /// Source code: https://www.3dbrew.org/wiki/RomFS
            /// </remarks>
            private static uint CalcPathHash(byte[] name, uint parentOffset)
            {
                uint hash = parentOffset ^ 123456789;
                for (int i = 0; i < name.Length; i += 2)
                {
                    hash = (hash >> 5) | (hash << 27);
                    hash ^= (ushort)((name[i]) | (name[i + 1] << 8));
                }
                return hash;
            }
        }

        #endregion
    }
}
