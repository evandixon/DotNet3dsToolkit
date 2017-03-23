using SkyEditor.Core.IO;
using SkyEditor.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace DotNet3dsToolkit.Core
{
    /// <summary>
    /// A ROM for the Nintendo DS
    /// </summary>
    public class NdsRom : GenericFile, IReportProgress, IIOProvider, IDisposable
    {

        #region Static Methods
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
        #endregion

        #region Child Classes
        private struct OverlayTableEntry
        {
            public OverlayTableEntry(byte[] rawData)
            {
                OverlayID = BitConverter.ToInt32(rawData, 0);
                RamAddress = BitConverter.ToInt32(rawData, 4);
                RamSize = BitConverter.ToInt32(rawData, 8);
                BssSize = BitConverter.ToInt32(rawData, 0xC);
                StaticInitStart = BitConverter.ToInt32(rawData, 0x10);
                StaticInitEnd = BitConverter.ToInt32(rawData, 0x14);
                FileID = BitConverter.ToInt32(rawData, 0x18);
            }

            public int OverlayID { get; set; }
            public int RamAddress { get; set; }
            public int RamSize { get; set; }
            public int BssSize { get; set; }
            public int StaticInitStart { get; set; }
            public int StaticInitEnd { get; set; }
            public int FileID { get; set; }

            public byte[] GetBytes()
            {
                var output = new List<byte>();

                output.AddRange(BitConverter.GetBytes(OverlayID));
                output.AddRange(BitConverter.GetBytes(RamAddress));
                output.AddRange(BitConverter.GetBytes(RamSize));
                output.AddRange(BitConverter.GetBytes(BssSize));
                output.AddRange(BitConverter.GetBytes(StaticInitStart));
                output.AddRange(BitConverter.GetBytes(StaticInitEnd));
                output.AddRange(BitConverter.GetBytes(FileID));

                return output.ToArray();
            }

        }

        private struct FileAllocationEntry
        {
            public FileAllocationEntry(int offset, int endAddress)
            {
                Offset = offset;
                EndAddress = endAddress;
            }

            public int Offset { get; set; }
            public int EndAddress { get; set; }
            public int Length => EndAddress - Offset;
        }

        private struct DirectoryMainTable
        {
            public DirectoryMainTable(byte[] rawData)
            {
                SubTableOffset = BitConverter.ToUInt32(rawData, 0);
                FirstSubTableFileID = BitConverter.ToUInt16(rawData, 4);
                ParentDir = BitConverter.ToUInt16(rawData, 6);
            }

            public UInt32 SubTableOffset { get; set; }
            public UInt16 FirstSubTableFileID { get; set; }
            public UInt16 ParentDir { get; set; }
        }

        private struct FNTSubTable
        {
            public byte Length { get; set; }
            public string Name { get; set; }
            public UInt16 SubDirectoryID { get; set; } // Only used for directories
            public UInt16 ParentFileID { get; set; }
        }

        private class FilenameTable
        {
            public FilenameTable()
            {
                FileIndex = -1;
                Children = new List<FilenameTable>();
            }

            public string Name { get; set; }

            public int FileIndex { get; set; }

            public bool IsDirectory => FileIndex < 0;

            public List<FilenameTable> Children { get; set; }

            public override string ToString()
            {
                return Name;
            }
        }

        public class NdsHeader : GenericFile
        {

            public string GameTitle
            {
                get
                {
                    return ReadString(0, 12, Encoding.ASCII);
                }
                set
                {
                    WriteString(0, Encoding.ASCII, value.PadRight(12, '\0').Substring(0, 12));
                }
            }

            public string GameCode
            {
                get
                {
                    return ReadString(12, 4, Encoding.ASCII);
                }
                set
                {
                    WriteString(12, Encoding.ASCII, value.PadRight(4, '\0').Substring(0, 4));
                }
            }

            public string MakerCode
            {
                get
                {
                    return ReadString(16, 2, Encoding.ASCII);
                }
                set
                {
                    WriteString(16, Encoding.ASCII, value.PadRight(2, '\0').Substring(0, 2));
                }
            }

            public byte UnitCode
            {
                get
                {
                    return Read(0x12);
                }
                set
                {
                    Write(0x12, value);
                }
            }

            public byte EncryptionSeedSelect
            {
                get
                {
                    return Read(0x13);
                }
                set
                {
                    Write(0x13, value);
                }
            }

            /// <summary>
            /// The capacity of the cartridge.  Cartridge size = 128KB * (2 ^ DeviceCapacity)
            /// </summary>
            public byte DeviceCapacity
            {
                get
                {
                    return Read(0x14);
                }
                set
                {
                    Write(0x14, value);
                }
            }

            /// <summary>
            /// Region of the ROM.
            /// (00h=Normal, 80h=China, 40h=Korea)
            /// </summary>
            public byte NdsRegion
            {
                get
                {
                    return Read(0x1D);
                }
                set
                {
                    Write(0x1D, value);
                }
            }

            public byte RomVersion
            {
                get
                {
                    return Read(0x1E);
                }
                set
                {
                    Write(0x1E, value);
                }
            }

            //01Fh    1     Autostart (Bit2: Skip "Press Button" after Health and Safety)
            //(Also skips bootmenu, even in Manual mode & even Start pressed)

            public int Arm9RomOffset
            {
                get
                {
                    return ReadInt32(0x20);
                }
                set
                {
                    WriteInt32(0x20, value);
                }
            }

            public int Arm9EntryAddress
            {
                get
                {
                    return ReadInt32(0x24);
                }
                set
                {
                    WriteInt32(0x24, value);
                }
            }
            public int Arm9RamAddress
            {
                get
                {
                    return ReadInt32(0x28);
                }
                set
                {
                    WriteInt32(0x28, value);
                }
            }

            public int Arm9Size
            {
                get
                {
                    return ReadInt32(0x2C);
                }
                set
                {
                    WriteInt32(0x2C, value);
                }
            }

            public int Arm7RomOffset
            {
                get
                {
                    return ReadInt32(0x30);
                }
                set
                {
                    WriteInt32(0x30, value);
                }
            }

            public int Arm7EntryAddress
            {
                get
                {
                    return ReadInt32(0x34);
                }
                set
                {
                    WriteInt32(0x34, value);
                }
            }
            public int Arm7RamAddress
            {
                get
                {
                    return ReadInt32(0x38);
                }
                set
                {
                    WriteInt32(0x38, value);
                }
            }

            public int Arm7Size
            {
                get
                {
                    return ReadInt32(0x3C);
                }
                set
                {
                    WriteInt32(0x3C, value);
                }
            }

            public int FilenameTableOffset
            {
                get
                {
                    return ReadInt32(0x40);
                }
                set
                {
                    WriteInt32(0x40, value);
                }
            }

            public int FilenameTableSize
            {
                get
                {
                    return ReadInt32(0x44);
                }
                set
                {
                    WriteInt32(0x44, value);
                }
            }

            public int FileAllocationTableOffset
            {
                get
                {
                    return ReadInt32(0x48);
                }
                set
                {
                    WriteInt32(0x48, value);
                }
            }

            public int FileAllocationTableSize
            {
                get
                {
                    return ReadInt32(0x4C);
                }
                set
                {
                    WriteInt32(0x4C, value);
                }
            }

            public int FileArm9OverlayOffset
            {
                get
                {
                    return ReadInt32(0x50);
                }
                set
                {
                    WriteInt32(0x50, value);
                }
            }

            public int FileArm9OverlaySize
            {
                get
                {
                    return ReadInt32(0x54);
                }
                set
                {
                    WriteInt32(0x54, value);
                }
            }

            public int FileArm7OverlayOffset
            {
                get
                {
                    return ReadInt32(0x58);
                }
                set
                {
                    WriteInt32(0x58, value);
                }
            }

            public int FileArm7OverlaySize
            {
                get
                {
                    return ReadInt32(0x5C);
                }
                set
                {
                    WriteInt32(0x5C, value);
                }
            }

            // 060h    4     Port 40001A4h setting for normal commands (usually 00586000h)
            // 064h    4     Port 40001A4h setting for KEY1 commands   (usually 001808F8h)

            public int IconOffset
            {
                get
                {
                    return ReadInt32(0x68);
                }
                set
                {
                    WriteInt32(0x68, value);
                }
            }

            public int IconLength
            {
                get
                {
                    return 0x840;
                }
            }

            // 06Ch    2     Secure Area Checksum, CRC-16 of [ [20h]..7FFFh]
            // 06Eh    2     Secure Area Loading Timeout (usually 051Eh)
            // 070h    4     ARM9 Auto Load List RAM Address (?)
            // 074h    4     ARM7 Auto Load List RAM Address (?)
            // 078h    8     Secure Area Disable (by encrypted "NmMdOnly") (usually zero)
            // 080h    4     Total Used ROM size (remaining/unused bytes usually FFh-padded)
            // 084h    4     ROM Header Size (4000h)
            // 088h    38h   Reserved (zero filled)
            // 0C0h    9Ch   Nintendo Logo (compressed bitmap, same as in GBA Headers)
            // 15Ch    2     Nintendo Logo Checksum, CRC-16 of [0C0h-15Bh], fixed CF56h
            // 15Eh    2     Header Checksum, CRC-16 of [000h-15Dh]
            // 160h    4     Debug rom_offset   (0=none) (8000h and up)       ;only if debug
            // 164h    4     Debug size         (0=none) (max 3BFE00h)        ;version with
            // 168h    4     Debug ram_address  (0=none) (2400000h..27BFE00h) ;SIO and 8MB
            // 16Ch    4     Reserved (zero filled) (transferred, and stored, but not used)
            // 170h    90h   Reserved (zero filled) (transferred, but not stored in RAM)

        }
        #endregion

        public NdsRom()
        {
            EnableInMemoryLoad = true;
        }

        public event EventHandler<ProgressReportedEventArgs> ProgressChanged;
        public event EventHandler Completed;

        public override async Task OpenFile(string filename, IIOProvider provider)
        {
            await base.OpenFile(filename, provider);

            // Clear virtual path if it exists
            if (!string.IsNullOrEmpty(VirtualPath) && provider.DirectoryExists(VirtualPath))
            {
                provider.DeleteDirectory(VirtualPath);
            }

            VirtualPath = provider.GetTempDirectory();


            // Load data
            Header = new NdsHeader();
            Header.CreateFile(await ReadAsync(0, 512));

            var loadingTasks = new List<Task>();

            // Load Arm9 Overlays
            var arm9overlayTask = Task.Run(async () => Arm9OverlayTable = await ParseArm9OverlayTable());

            if (IsThreadSafe)
            {
                loadingTasks.Add(arm9overlayTask);
            }
            else
            {
                await arm9overlayTask;
            }

            // Load Arm7 Overlays
            var arm7overlayTask = Task.Run(async () => Arm7OverlayTable = await ParseArm7OverlayTable());

            if (IsThreadSafe)
            {
                loadingTasks.Add(arm7overlayTask);
            }
            else
            {
                await arm7overlayTask;
            }

            // Load FAT
            var fatTask = Task.Run(async () => FAT = await ParseFAT());

            if (IsThreadSafe)
            {
                loadingTasks.Add(fatTask);
            }
            else
            {
                await fatTask;
            }

            // Load FNT
            var fntTask = Task.Run(async () => FNT = await ParseFNT());

            if (IsThreadSafe)
            {
                loadingTasks.Add(fntTask);
            }
            else
            {
                await fntTask;
            }

            // Wait for all loading

            await Task.WhenAll(loadingTasks);
        }

        #region Properties
        private NdsHeader Header { get; set; }

        private List<OverlayTableEntry> Arm9OverlayTable { get; set; }

        private List<OverlayTableEntry> Arm7OverlayTable { get; set; }

        private List<FileAllocationEntry> FAT { get; set; }

        private FilenameTable FNT { get; set; }

        /// <summary>
        /// Path in the current I/O provider where temporary files are stored
        /// </summary>
        private string VirtualPath { get; set; }

        #endregion

        #region Functions
        private async Task<List<OverlayTableEntry>> ParseArm9OverlayTable()
        {
            var output = new List<OverlayTableEntry>();
            for (int i = Header.FileArm9OverlayOffset; i < Header.FileArm9OverlayOffset + Header.FileArm9OverlaySize; i += 32)
            {
                output.Add(new OverlayTableEntry(await ReadAsync(i, 32)));
            }
            return output;
        }

        private async Task<List<OverlayTableEntry>> ParseArm7OverlayTable()
        {
            var output = new List<OverlayTableEntry>();
            for (int i = Header.FileArm7OverlayOffset; i < Header.FileArm7OverlayOffset + Header.FileArm7OverlaySize; i += 32)
            {
                output.Add(new OverlayTableEntry(await ReadAsync(i, 32)));
            }
            return output;
        }

        private async Task<List<FileAllocationEntry>> ParseFAT()
        {
            var output = new List<FileAllocationEntry>();
            for (int i = Header.FileAllocationTableOffset; i < Header.FileAllocationTableOffset + Header.FileAllocationTableSize; i += 8)
            {
                output.Add(new FileAllocationEntry(await ReadInt32Async(i), await ReadInt32Async(i)));
            }
            return output;
        }

        private async Task<FilenameTable> ParseFNT()
        {
            // Read the raw structures
            var root = new DirectoryMainTable(await ReadAsync(Header.FilenameTableOffset, 8));
            var rootDirectories = new List<DirectoryMainTable>();

            // - In the root directory only, ParentDir means the number of directories
            for (int i = 1; i <= root.ParentDir; i += 1)
            {
                var offset = Header.FilenameTableOffset + i * 8;
                rootDirectories.Add(new DirectoryMainTable(await ReadAsync(offset, 8)));
            }

            // Build the filename table
            var output = new FilenameTable();
            output.Name = "data";
            await BuildFNT(output, root, rootDirectories);
            return output;
        }

        private async Task BuildFNT(FilenameTable parentFNT, DirectoryMainTable root, List<DirectoryMainTable> directories)
        {
            foreach (var item in await ReadFNTSubTable(root.SubTableOffset, root.FirstSubTableFileID))
            {
                var child = new FilenameTable { Name = item.Name };
                parentFNT.Children.Add(child);
                if (item.Length > 128)
                {
                    // Directory
                    await BuildFNT(child, directories[item.SubDirectoryID & 0x0FFF - 1], directories);
                }
                else
                {
                    // File
                    child.FileIndex = item.ParentFileID;
                }
            }
        }

        private async Task<List<FNTSubTable>> ReadFNTSubTable(uint rootSubTableOffset, ushort parentFileID)
        {
            var subTables = new List<FNTSubTable>();
            var offset = rootSubTableOffset + Header.FilenameTableOffset;
            var length = await ReadAsync(offset);
            while (length > 0)
            {
                if (length > 128)
                {
                    // Directory
                    var name = await ReadStringAsync(offset + 1, length - 128, Encoding.ASCII);
                    var subDirID = await ReadUInt16Async(offset + 1 + length - 128);
                    subTables.Add(new FNTSubTable { Length = length, Name = name, SubDirectoryID = subDirID });
                    offset += length - 128 + 1 + 2;
                }
                else if (length < 128)
                {
                    // File
                    var name = await ReadStringAsync(offset + 1, length, Encoding.ASCII);
                    subTables.Add(new FNTSubTable { Length = length, Name = name, ParentFileID = parentFileID });
                    parentFileID += 1;
                    offset += length + 1;
                }
                else
                {
                    throw new FormatException($"Subtable length of 0x80 is not supported and likely invalid.  Root subtable offset: {rootSubTableOffset}");
                }

                length = await ReadAsync(offset);
            }
            return subTables;
        }

        private bool CheckNeedsArm9Footer()
        {
            return ReadUInt32(Header.Arm9RomOffset + Header.Arm9Size) == 0xDEC00621;
        }

        /// <summary>
        /// Extracts the files contained within the ROM.
        /// </summary>
        /// <param name="targetDir">Directory in the given I/O provider (<paramref name="provider"/>) to store the extracted files</param>
        /// <param name="provider">The I/O provider to which the files should be written</param>
        public async Task Unpack(string targetDir, IIOProvider provider)
        {
            // Get the files
            var files = GetFiles("/", "*", false);

            // Set progress
            TotalFileCount = files.Length;
            ExtractedFileCount = 0;

            // Ensure directory exists
            if (!provider.DirectoryExists(targetDir))
            {
                provider.CreateDirectory(targetDir);
            }

            // Extract the files
            var extractionTasks = new List<Task>();
            foreach (var item in files)
            {
                var currentItem = item;
                var currentTask = Task.Run(() => 
                {
                    provider.WriteAllBytes(Path.Combine(targetDir, currentItem.TrimStart('/')), this.ReadAllBytes(currentItem));
                    Interlocked.Increment(ref _extractedFileCount);
                    ReportProgressChanged();
                });

                if (IsThreadSafe)
                {
                    extractionTasks.Add(currentTask);
                }
                else
                {
                    await currentTask;
                }
            }
            await Task.WhenAll(extractionTasks);
        }
        #endregion

        #region IReportProgress Implementation

        /// <summary>
        /// Raises <see cref="ProgressChanged"/> using the value of relevant properties
        /// </summary>
        private void ReportProgressChanged()
        {
            ProgressChanged?.Invoke(this, new ProgressReportedEventArgs { Message = Message, IsIndeterminate = false, Progress = Progress });
        }

        /// <summary>
        /// The number of files that have been extracted in the current extraction operation
        /// </summary>
        public int ExtractedFileCount
        {
            get
            {
                return _extractedFileCount;
            }
            set
            {
                if (_extractedFileCount != value)
                {
                    _extractedFileCount = value;
                    ReportProgressChanged();
                }
            }
        }
        private int _extractedFileCount;

        /// <summary>
        /// The total number of files in the ROM
        /// </summary>
        public int TotalFileCount
        {
            get
            {
                return _totalFileCount;
            }
            set
            {
                if (_totalFileCount != value)
                {
                    _totalFileCount = value;
                    ReportProgressChanged();
                }
            }
        }
        private int _totalFileCount;

        /// <summary>
        /// A percentage representing the progress of the current extraction operation
        /// </summary>
        public float Progress => ExtractedFileCount / TotalFileCount;

        /// <summary>
        /// A string representing what is being done in the current extraction operation
        /// </summary>
        public string Message
        {
            get
            {
                if (IsCompleted)
                {
                    return Properties.Resources.Complete;
                }
                else
                {
                    return Properties.Resources.LoadingUnpacking;
                }
            }
        }

        /// <summary>
        /// Whether or not the progress of the current extraction operation can be determined
        /// </summary>
        bool IReportProgress.IsIndeterminate => false;

        /// <summary>
        /// Whether or not the current extraction operation is complete
        /// </summary>
        public bool IsCompleted
        {
            get
            {
                return _isCompleted;
            }
            set
            {
                _isCompleted = value;
                if (_isCompleted)
                {
                    Completed?.Invoke(this, new EventArgs());
                }
            }
        }
        private bool _isCompleted;

        #endregion

        #region IIOProvider Implementation
        /// <summary>
        /// Keeps track of files that have been logically deleted
        /// </summary>
        private List<string> BlacklistedPaths => new List<string>();

        public string WorkingDirectory
        {
            get
            {
                var path = new StringBuilder();
                foreach (var item in _workingDirectoryParts)
                {
                    path.Append("/");
                    path.Append(item);
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
            if (!path.StartsWith("/"))
            {
                parts.AddRange(_workingDirectoryParts);
            }

            foreach (var item in path.Split('/'))
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

        public void ResetWorkingDirectory()
        {
            WorkingDirectory = "/";
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
                return Path.Combine(WorkingDirectory, path);
            }
        }

        private string GetVirtualPath(string path)
        {
            return Path.Combine(VirtualPath, path);
        }

        private FileAllocationEntry? GetFATEntry(string path, bool throwIfNotFound = true)
        {
            var parts = GetPathParts(path);
            switch (parts[0].ToLower())
            {
                case "data":
                    var currentEntry = FNT;
                    for (int i = 1; i < parts.Length; i += 1)
                    {
                        currentEntry = currentEntry?.Children.Where(x => x.Name.ToLower() == parts[i]).FirstOrDefault();
                    }
                    if (!currentEntry.IsDirectory)
                    {
                        return FAT[currentEntry.FileIndex];
                    }
                    break;
                case "overlay":
                    int index;
                    if (int.TryParse(parts[1].ToLower().Substring(8, 4), out index))
                    {
                        return FAT[Arm9OverlayTable[index].FileID];
                    }
                    break;
                case "overlay7":
                    int index7;
                    if (int.TryParse(parts[1].ToLower().Substring(8, 4), out index7))
                    {
                        return FAT[Arm7OverlayTable[index7].FileID];
                    }
                    break;
                case "arm7.bin":
                    return new FileAllocationEntry(Header.Arm7RomOffset, Header.Arm7RomOffset + Header.Arm7Size);
                case "arm9.bin":
                    if (CheckNeedsArm9Footer())
                    {
                        return new FileAllocationEntry(Header.Arm9RomOffset, Header.Arm9RomOffset + Header.Arm9Size + 0xC);
                    }
                    else
                    {
                        return new FileAllocationEntry(Header.Arm9RomOffset, Header.Arm9RomOffset + Header.Arm9Size + 0xC);
                    }
                case "header.bin":
                    return new FileAllocationEntry(0, 0x200);
                case "banner.bin":
                    return new FileAllocationEntry(Header.IconOffset, Header.IconOffset + Header.IconLength);
                case "y7.bin":
                    return new FileAllocationEntry(Header.FileArm7OverlayOffset, Header.FileArm7OverlayOffset + Header.FileArm7OverlaySize);
                case "y9.bin":
                    return new FileAllocationEntry(Header.FileArm9OverlayOffset, Header.FileArm9OverlayOffset + Header.FileArm9OverlaySize);
            }

            // Default
            if (throwIfNotFound)
            {
                throw new FileNotFoundException(string.Format(Properties.Resources.ErrorRomFileNotFound, path), path);
            }
            else
            {
                return null;
            }
        }

        public long GetFileLength(string filename)
        {
            return GetFATEntry(filename).Value.Length;
        }

        public bool FileExists(string filename)
        {
            return CurrentIOProvider.FileExists(GetVirtualPath(filename)) || GetFATEntry(filename, false).HasValue;
        }

        private bool DirectoryExists(string[] parts)
        {
            if (parts.Length == 1)
            {
                switch (parts[0].ToLower())
                {
                    case "data":
                        return true;
                    case "overlay":
                        return true;
                    case "overlay7":
                        return true;
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
                if (parts[0].ToLower() == "data")
                {
                    var currentEntry = FNT;
                    for (int i = 1; i < parts.Length; i += 1)
                    {
                        var currentPartLower = parts[i].ToLower();
                        currentEntry = currentEntry?.Children.Where(x => x.Name.ToLower() == currentPartLower).FirstOrDefault();
                    }
                    return (currentEntry?.IsDirectory).HasValue;
                }
                else
                {
                    return false;
                }
            }
        }

        public bool DirectoryExists(string path)
        {
            return !BlacklistedPaths.Contains(FixPath(path)) && (CurrentIOProvider.DirectoryExists(GetVirtualPath(path)) || DirectoryExists(GetPathParts(path)));
        }

        public void CreateDirectory(string path)
        {
            var fixedPath = FixPath(path);
            if (BlacklistedPaths.Contains(fixedPath))
            {
                BlacklistedPaths.Remove(fixedPath);
            }

            if (!DirectoryExists(fixedPath))
            {
                CurrentIOProvider.CreateDirectory(GetVirtualPath(fixedPath));
            }
        }

        private IEnumerable<string> GetFilesFromNode(string pathBase, FilenameTable currentTable, Regex searchPatternRegex, bool topDirectoryOnly)
        {
            var output = new List<string>();
            foreach (var item in currentTable.Children.Where(x => !x.IsDirectory))
            {
                if (searchPatternRegex.IsMatch(item.Name))
                {
                    output.Add(pathBase + "/" + item.Name);
                }
            }
            if (!topDirectoryOnly)
            {
                foreach (var item in currentTable.Children.Where(x => x.IsDirectory))
                {
                    output.AddRange(GetFilesFromNode(pathBase + "/" + item.Name, item, searchPatternRegex, topDirectoryOnly));
                }
            }
            return output;
        }

        public string[] GetFiles(string path, string searchPattern, bool topDirectoryOnly)
        {
            var output = new List<string>();
            var parts = GetPathParts(path);
            var searchPatternRegex = new Regex(GetFileSearchRegex(searchPattern), RegexOptions.Compiled | RegexOptions.IgnoreCase);
            switch (parts[0].ToLower())
            {
                case "":
                    output.Add("/arm7.bin");
                    output.Add("/arm9.bin");
                    output.Add("/header.bin");
                    output.Add("/banner.bin");
                    output.Add("/y7.bin");
                    output.Add("/y9.bin");
                    return output.ToArray();
                case "overlay":
                case "overlay7":
                    // Original files
                    for (int i = 0; i < Arm9OverlayTable.Count; i += 1)
                    {
                        var overlayPath = $"{parts[0].ToLower()}/overlay_{i.ToString().PadLeft(4, '0')}.bin";
                        if (searchPatternRegex.IsMatch(Path.GetFileName(overlayPath)))
                        {
                            if (!BlacklistedPaths.Contains(overlayPath))
                            {
                                output.Add(overlayPath);
                            }
                        }
                    }

                    // Apply shadowed files
                    foreach (var item in CurrentIOProvider.GetFiles(GetVirtualPath(parts[0].ToLower()), "overlay_*.bin", true))
                    {
                        if (searchPatternRegex.IsMatch(Path.GetFileName(item)))
                        {
                            var overlayPath = "/" + FileSystem.MakeRelativePath(item, VirtualPath);
                            if (!BlacklistedPaths.Contains(overlayPath) && !output.Contains(overlayPath))
                            {
                                output.Add(overlayPath);
                            }
                        }
                    }
                    return output.ToArray();
                case "data":
                    // Get the desired directory
                    var currentEntry = FNT;
                    var pathBase = new StringBuilder();
                    pathBase.Append("/data");
                    for (int i = 1; i < parts.Length; i += 1)
                    {
                        var partLower = parts[i].ToLower();
                        currentEntry = currentEntry?.Children.Where(x => x.Name.ToLower() == partLower && x.IsDirectory).FirstOrDefault();
                        if (currentEntry == null)
                        {
                            break;
                        }
                        else
                        {
                            pathBase.Append($"/{currentEntry.Name}");
                        }
                    }

                    // Get the files
                    if (currentEntry != null && currentEntry.IsDirectory)
                    {
                        output.AddRange(GetFilesFromNode(pathBase.ToString(), currentEntry, searchPatternRegex, topDirectoryOnly));
                    }
                    else
                    {
                        throw new FileNotFoundException(string.Format(Properties.Resources.ErrorRomFileNotFound, path), path);
                    }

                    // Apply shadowed files
                    var virtualPath = GetVirtualPath(path);
                    if (CurrentIOProvider.DirectoryExists(virtualPath))
                    {
                        foreach (var item in CurrentIOProvider.GetFiles(virtualPath, searchPattern, topDirectoryOnly))
                        {
                            var filePath = "/" + FileSystem.MakeRelativePath(item, VirtualPath);
                            if (!output.Contains(filePath))
                            {
                                output.Add(filePath);
                            }
                        }
                    }
                    break;
                default:
                    throw new FileNotFoundException(string.Format(Properties.Resources.ErrorRomFileNotFound, path), path);
            }
            return output.ToArray();
        }

        public string[] GetDirectories(string path, bool topDirectoryOnly)
        {
            var output = new List<string>();
            var parts = GetPathParts(path);
            switch (parts[0].ToLower())
            {
                case "":
                    output.Add("/data");
                    output.Add("/overlay");
                    output.Add("/overlay7");
                    break;
                case "overlay":
                case "overlay7":
                    // Overlays have no child directories
                    break;
                case "data":
                    var currentEntry = FNT;
                    for (int i = 1; i < parts.Length; i += 1)
                    {
                        var partLower = parts[i].ToLower();
                        currentEntry = currentEntry?.Children.Where(x => x.Name.ToLower() == partLower && x.IsDirectory).FirstOrDefault();
                    }

                    if (currentEntry != null && currentEntry.IsDirectory)
                    {
                        output.AddRange(currentEntry.Children.Where(x => x.IsDirectory).Select(x => x.Name));
                    }
                    else
                    {
                        throw new FileNotFoundException(string.Format(Properties.Resources.ErrorRomFileNotFound, path), path);
                    }
                    break;
                default:
                    throw new FileNotFoundException(string.Format(Properties.Resources.ErrorRomFileNotFound, path), path);
            }
            if (!topDirectoryOnly)
            {
                foreach (var item in output)
                {
                    output.AddRange(GetDirectories(item, topDirectoryOnly));
                }
            }
            return output.ToArray();
        }

        public byte[] ReadAllBytes(string filename)
        {
            var fixedPath = FixPath(filename);
            if (BlacklistedPaths.Contains(fixedPath))
            {
                throw new FileNotFoundException(string.Format(Properties.Resources.ErrorRomFileNotFound, filename), filename);
            }
            else
            {
                var virtualPath = GetVirtualPath(fixedPath);
                if (CurrentIOProvider.FileExists(virtualPath))
                {
                    return CurrentIOProvider.ReadAllBytes(virtualPath);
                }
                else
                {
                    var entry = GetFATEntry(filename);
                    return Read(entry.Value.Offset, entry.Value.Length);
                }
            }
        }

        public string ReadAllText(string filename)
        {
            return Encoding.UTF8.GetString(ReadAllBytes(filename));
        }

        public void WriteAllBytes(string filename, byte[] data)
        {
            var fixedPath = FixPath(filename);
            if (BlacklistedPaths.Contains(fixedPath))
            {
                BlacklistedPaths.Remove(fixedPath);
            }

            CurrentIOProvider.WriteAllBytes(GetVirtualPath(filename), data);
        }

        public void WriteAllText(string filename, string data)
        {
            WriteAllBytes(filename, Encoding.UTF8.GetBytes(data));
        }

        public void CopyFile(string sourceFilename, string destinationFilename)
        {
            WriteAllBytes(destinationFilename, ReadAllBytes(sourceFilename));
        }

        public void DeleteFile(string filename)
        {
            var fixedPath = FixPath(filename);
            if (!BlacklistedPaths.Contains(fixedPath))
            {
                BlacklistedPaths.Add(fixedPath);
            }

            var virtualPath = GetVirtualPath(filename);
            if (CurrentIOProvider.FileExists(virtualPath))
            {
                CurrentIOProvider.DeleteFile(virtualPath);
            }
        }

        public void DeleteDirectory(string path)
        {
            var fixedPath = FixPath(path);
            if (!BlacklistedPaths.Contains(fixedPath))
            {
                BlacklistedPaths.Add(fixedPath);
            }

            var virtualPath = GetVirtualPath(path);
            if (CurrentIOProvider.FileExists(virtualPath))
            {
                CurrentIOProvider.DeleteFile(virtualPath);
            }
        }

        public string GetTempFilename()
        {
            var path = "/temp/files/" + Guid.NewGuid().ToString();
            WriteAllBytes(path, new byte[] { });
            return path;
        }

        public string GetTempDirectory()
        {
            var path = "/temp/dirs/" + Guid.NewGuid().ToString();
            CreateDirectory(path);
            return path;
        }

        private Stream OpenFile(string filename, FileAccess access)
        {
            var virtualPath = GetVirtualPath(filename);
            if (!CurrentIOProvider.DirectoryExists(virtualPath))
            {
                CurrentIOProvider.CreateDirectory(virtualPath);
            }
            CurrentIOProvider.WriteAllBytes(virtualPath, ReadAllBytes(filename));

            return File.Open(virtualPath, FileMode.OpenOrCreate, access);
        }

        public Stream OpenFile(string filename)
        {
            return OpenFile(filename, FileAccess.ReadWrite);
        }

        public Stream OpenFileReadOnly(string filename)
        {
            return OpenFile(filename, FileAccess.Read);
        }

        public Stream OpenFileWriteOnly(string filename)
        {
            return OpenFile(filename, FileAccess.Write);
        }
        #endregion

        #region IDisposable Implementation

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!string.IsNullOrEmpty(VirtualPath) && CurrentIOProvider.DirectoryExists(VirtualPath))
            {
                CurrentIOProvider.DeleteDirectory(VirtualPath);
                VirtualPath = null;
            }
        }
        #endregion
    }
}
