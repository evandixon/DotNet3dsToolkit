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
using System.Collections.Concurrent;

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

        /// <summary>
        /// A single entry in an overlay table
        /// </summary>
        private struct OverlayTableEntry
        {
            public OverlayTableEntry(byte[] rawData, int offset = 0)
            {
                OverlayID = BitConverter.ToInt32(rawData, offset + 0);
                RamAddress = BitConverter.ToInt32(rawData, offset + 4);
                RamSize = BitConverter.ToInt32(rawData, offset + 8);
                BssSize = BitConverter.ToInt32(rawData, offset + 0xC);
                StaticInitStart = BitConverter.ToInt32(rawData, offset + 0x10);
                StaticInitEnd = BitConverter.ToInt32(rawData, offset + 0x14);
                FileID = BitConverter.ToInt32(rawData, offset + 0x18);
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

        /// <summary>
        /// A single entry in the FAT
        /// </summary>
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
            public override string ToString()
            {
                return $"Length: {Length}, Sub-Directory ID: {SubDirectoryID}, Parent File ID: {ParentFileID}, Name: {Name}";
            }
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

        /// <summary>
        /// Represents an entry in the overlay table, in addition to the overlay itself
        /// </summary>
        private class Overlay
        {
            public OverlayTableEntry TableEntry { get; set; }
            public byte[] Data { get; set; }
        }

        /// <summary>
        /// The NDS header
        /// </summary>
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

        public struct Range
        {
            public int Start { get; set; }

            public int Length { get; set; }

            public int End
            {
                get
                {
                    return Start + Length - 1;
                }
                set
                {
                    Length = (value - Start) + 1;
                }
            }
        }

        public class LayoutAnalysisReport
        {
            public LayoutAnalysisReport()
            {
                Ranges = new Dictionary<Range, string>();
            }

            /// <summary>
            /// The address ranges of the ROM. Key: range; Value: name
            /// </summary>
            public Dictionary<Range, string> Ranges { get; set; }

            /// <summary>
            /// Consolidates consecutive ranges of the same category
            /// </summary>
            public void CollapseRanges()
            {
                var newRanges = new Dictionary<Range, string>();
                foreach (var item in Ranges.Where(x => x.Key.Length > 0).OrderBy(x => x.Key.Start).GroupBy(x => x.Value, x => x.Key))
                {
                    // Get a set of all Ranges in the same category, ordered by the start address
                    var ranges = item.ToList();

                    // Collapse consecutive ranges
                    var currentRange = ranges.First();
                    if (ranges.Count > 1)
                    {
                        for (int i = 1; i < ranges.Count; i += 1)
                        {
                            if (currentRange.End + 1 == ranges[i].Start)
                            {
                                // This range is consecutive
                                currentRange.Length += currentRange.Length;
                            }
                            else
                            {
                                // This range is separate
                                newRanges.Add(currentRange, item.Key);
                                currentRange = ranges[i];
                            }
                        }
                    }
                    else
                    {
                        newRanges.Add(currentRange, item.Key);
                    }
                }
                Ranges = newRanges;
            }

            public string GenerateCSV()
            {
                CollapseRanges();

                var report = new StringBuilder();
                report.AppendLine("Section,Start Address (decimal),End Address (decimal),Length (decimal),Start Address (hex),End Address (hex), Length (hex)");

                var ranges = Ranges.OrderBy(x => x.Key.Start).ToList();
                var currentRange = ranges.First();
                report.AppendLine($"{currentRange.Value},{currentRange.Key.Start},{currentRange.Key.End},{currentRange.Key.Length},{currentRange.Key.Start.ToString("X")},{currentRange.Key.End.ToString("X")},{currentRange.Key.Length.ToString("X")}");
                for (int i = 1; i < ranges.Count; i += 1)
                {
                    if (currentRange.Key.End + 1 < ranges[i].Key.Start)
                    {
                        // There's some unknown parts between the previous one and this one
                        var section = Properties.Resources.NdsRom_Analysis_UnknownSection;
                        var start = currentRange.Key.End + 1;
                        var length = ranges[i].Key.Start - start;
                        var end = start + length - 1;
                        report.AppendLine($"{section},{start},{end},{length},{start.ToString("X")},{end.ToString("X")},{length.ToString("X")}");
                    }
                    currentRange = ranges[i];
                    report.AppendLine($"{currentRange.Value},{currentRange.Key.Start},{currentRange.Key.End},{currentRange.Key.Length},{currentRange.Key.Start.ToString("X")},{currentRange.Key.End.ToString("X")},{currentRange.Key.Length.ToString("X")}");
                }

                return report.ToString();
            }
        }
        #endregion

        public NdsRom()
        {
            EnableInMemoryLoad = true;
            (this as IIOProvider).ResetWorkingDirectory();
            DataPath = "data";
        }

        public event EventHandler<ProgressReportedEventArgs> ProgressChanged;
        public event EventHandler Completed;

        /// <summary>
        /// The path of the nitrofs file system is stored. Defaults to "data".
        /// </summary>
        public string DataPath { get; set; }

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

        public override async Task Save(string filename, IIOProvider provider)
        {
            var filesAlloc = new ConcurrentDictionary<int, byte[]>(); // Allocated files (including overlays)
            var fileNames = new ConcurrentDictionary<string, int>(); // File names of nitrofs (excluding overlays, so not all entries in filesAlloc have a name)
            var overlay9 = new ConcurrentDictionary<int, OverlayTableEntry>(); // Key = index in table, value = the entry
            var overlay7 = new ConcurrentDictionary<int, OverlayTableEntry>(); // Key = index in table, value = the entry

            // Read header-related files
            var header = new NdsHeader();
            await header.OpenFile("/header.bin", this);

            var banner = (this as IIOProvider).ReadAllBytes("/banner.bin");
            var arm9Bin = (this as IIOProvider).ReadAllBytes("/arm9.bin");
            var arm7Bin = (this as IIOProvider).ReadAllBytes("/arm7.bin");

            // Identify ARM9 overlays
            var overlay9Raw = (this as IIOProvider).ReadAllBytes("/y9.bin");
            var arm9For = new AsyncFor();
            arm9For.RunSynchronously = !IsThreadSafe;
            await arm9For.RunFor(i =>
            {
                var entry = new OverlayTableEntry(overlay9Raw, i);
                var overlayPath = $"/overlay/overlay_{entry.FileID.ToString().PadLeft(4, '0')}.bin";
                if ((this as IIOProvider).FileExists(overlayPath))
                {
                    filesAlloc[entry.FileID] = (this as IIOProvider).ReadAllBytes(overlayPath);
                }
                overlay9[i / 32] = entry;
            }, 0, overlay9Raw.Length - 1, 32);

            // Identify ARM7 overlays
            var overlay7Raw = (this as IIOProvider).ReadAllBytes("/y7.bin");
            var arm7For = new AsyncFor();
            arm9For.RunSynchronously = !this.IsThreadSafe;
            await arm7For.RunFor(i =>
            {
                var entry = new OverlayTableEntry(overlay7Raw, i);
                var overlayPath = $"/overlay7/overlay_{entry.FileID.ToString().PadLeft(4, '0')}.bin";
                if ((this as IIOProvider).FileExists(overlayPath))
                {
                    var data = (this as IIOProvider).ReadAllBytes(overlayPath);
                    filesAlloc[entry.FileID] = data;
                }
                overlay7[i / 32] = entry;
            }, 0, overlay7Raw.Length - 1, 32);

            // Identify files to add to new archive
            var files = (this as IIOProvider).GetFiles("/data", "*", false);
            var filesFor = new AsyncFor();
            filesFor.RunSynchronously = !IsThreadSafe;
            await filesFor.RunFor(i =>
            {
                var data = (this as IIOProvider).ReadAllBytes(files[i]);
                filesAlloc[i] = data;
                fileNames[files[i]] = i;

            }, 0, files.Length - 1);

            // Build FNT
            var fntSection = EncodeFNT("/data", fileNames);

            // Set file size
            var filesAllocSize = filesAlloc.Values.Select(x => x.Length).Sum();
            var fatSize = filesAlloc.Keys.Max() * 8;
            int fileSize = (int)(Math.Pow(2, header.DeviceCapacity) * 128 * 1024);

            // To-do: analyze files and adjust placement and size of arm9/arm7 binaries/overlays
            // To-do: analyze files and increase capacity if needed

            this.Length = fileSize;

            // Write header sections
            var headerData = header.Read();
            await WriteAsync(0, headerData);
            await WriteAsync(header.Arm9RomOffset, arm9Bin);
            await WriteAsync(header.Arm7RomOffset, arm7Bin);
            await WriteAsync(header.IconOffset, banner);

            // Write overlay tables
            // - ARM9
            for (int i = 0; i < overlay9.Count; i += 1)
            {
                await WriteAsync(header.FileArm9OverlayOffset + 32 * i, overlay9[i].GetBytes());
            }

            // - ARM7
            for (int i = 0; i < overlay7.Count; i += 1)
            {
                await WriteAsync(header.FileArm7OverlayOffset + 32 * i, overlay7[i].GetBytes());
            }

            // Write nitrofs files
            int nextFileOffset = Math.Max(header.FileArm9OverlayOffset + header.FileArm9OverlaySize + 0xA0, header.FileArm7OverlayOffset + header.FileArm7OverlaySize + 0xA0);
            if (nextFileOffset == 0)
            {
                // No overlays to guide to
                throw new NotImplementedException();
            }

            // - Generate FAT
            var fat = new List<byte>(fatSize);

            // - Write Files
            for (int i = 0; i < fatSize / 8; i += 1)
            {
                if (filesAlloc.ContainsKey(i))
                {
                    var data = filesAlloc[i];
                    await WriteAsync(nextFileOffset, data);

                    fat.AddRange(BitConverter.GetBytes(nextFileOffset));
                    fat.AddRange(BitConverter.GetBytes(nextFileOffset + data.Length - 1));
                    nextFileOffset += data.Length;
                }
                else
                {
                    fat.AddRange(Enumerable.Repeat<byte>(0, 8));
                }
            }

            await base.Save(filename, provider);
        }

        /// <summary>
        /// Analyzes the layout of the sections of the ROM
        /// </summary>
        public LayoutAnalysisReport AnalyzeLayout(bool showPadding = false)
        {
            var report = new LayoutAnalysisReport();

            // Header
            report.Ranges.Add(new Range { Start = 0, Length = (int)Header.Length }, Properties.Resources.NdsRom_Analysis_HeaderSection);

            // Icon
            report.Ranges.Add(new Range { Start = Header.IconOffset, Length = Header.IconLength }, Properties.Resources.NdsRom_Analysis_IconSection);

            // ARM9 binary
            var arm9Length = Header.Arm9Size;
            if (CheckNeedsArm9Footer())
            {
                arm9Length += 0xC;
            }
            report.Ranges.Add(new Range { Start = Header.Arm9RomOffset, Length = arm9Length }, Properties.Resources.NdsRom_Analysis_ARM9Section);

            // ARM7 binary
            report.Ranges.Add(new Range { Start = Header.Arm7RomOffset, Length = Header.Arm7Size }, Properties.Resources.NdsRom_Analysis_ARM7Section);

            // ARM9 overlay table
            report.Ranges.Add(new Range { Start = Header.FileArm9OverlayOffset, Length = Header.FileArm9OverlaySize }, Properties.Resources.NdsRom_Analysis_ARM9OverlaySection);

            // ARM7 overlay table
            report.Ranges.Add(new Range { Start = Header.FileArm7OverlayOffset, Length = Header.FileArm7OverlaySize }, Properties.Resources.NdsRom_Analysis_ARM7OverlaySection);

            // FNT
            report.Ranges.Add(new Range { Start = Header.FilenameTableOffset, Length = Header.FilenameTableSize }, Properties.Resources.NdsRom_Analysis_FNTSection);

            // FAT
            report.Ranges.Add(new Range { Start = Header.FileAllocationTableOffset, Length = Header.FileAllocationTableSize }, Properties.Resources.NdsRom_Analysis_FATSection);

            // Files (includes overlay files)
            if (showPadding)
            {
                foreach (var item in FAT)
                {
                    report.Ranges.Add(new Range { Start = item.Offset, Length = item.Length }, Properties.Resources.NdsRom_Analysis_FileSection);
                }
            }
            else
            {
                report.Ranges.Add(new Range { Start = FAT.Min(x => x.Offset), Length = FAT.Max(x => x.EndAddress) }, Properties.Resources.NdsRom_Analysis_FileSection);
            }

            return report;
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
                output.Add(new FileAllocationEntry(await ReadInt32Async(i), await ReadInt32Async(i + 4)));
            }
            return output;
        }

        private async Task<FilenameTable> ParseFNT()
        {
            // Read the raw structures
            var root = new DirectoryMainTable(await ReadAsync(Header.FilenameTableOffset, 8));
            var rootDirectories = new List<DirectoryMainTable>();

            // - In the root directory only, ParentDir means the number of directories
            for (int i = 1; i < root.ParentDir; i += 1)
            {
                var offset = Header.FilenameTableOffset + i * 8;
                rootDirectories.Add(new DirectoryMainTable(await ReadAsync(offset, 8)));
            }

            // Build the filename table
            var output = new FilenameTable();
            output.Name = DataPath;
            await BuildFNTFromROM(output, root, rootDirectories);
            return output;
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
                    var name = await ReadStringAsync(offset + 1, length & 0x7F, Encoding.ASCII);
                    var subDirID = await ReadUInt16Async(offset + 1 + (length & 0x7F));
                    subTables.Add(new FNTSubTable { Length = length, Name = name, SubDirectoryID = subDirID });
                    offset += (length & 0x7F) + 1 + 2;
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

        private async Task BuildFNTFromROM(FilenameTable parentFNT, DirectoryMainTable root, List<DirectoryMainTable> directories)
        {
            foreach (var item in await ReadFNTSubTable(root.SubTableOffset, root.FirstSubTableFileID))
            {
                var child = new FilenameTable { Name = item.Name };
                parentFNT.Children.Add(child);
                if (item.Length > 128)
                {
                    // Directory
                    await BuildFNTFromROM(child, directories[(item.SubDirectoryID & 0x0FFF) - 1], directories);
                }
                else
                {
                    // File
                    child.FileIndex = item.ParentFileID;
                }
            }
        }

        /// <summary>
        /// Builds a filename table from current files, including the shadow directory
        /// </summary>
        /// <param name="path">Path of the node from which to build the FNT</param>
        /// <param name="filenames">Dicitonary matching paths to file indexes. </param>
        private FilenameTable BuildCurrentFNTChild(string path, IDictionary<string, int> filenames, ref int directoryCount, ref int fileCount)
        {
            var provider = this as IIOProvider;

            var table = new FilenameTable();
            table.Name = Path.GetFileName(path);

            if (provider.FileExists(path))
            {
                table.FileIndex = filenames[path];
                fileCount += 1;
            }
            else // Assume directory exists
            {
                var children = provider.GetDirectories(path, true);
                foreach (var item in children)
                {
                    table.Children.Add(BuildCurrentFNTChild(item, filenames, ref directoryCount, ref fileCount));
                    directoryCount += 1;
                }
            }
            return table;
        }

        private int? GetFirstFNTFileID(FilenameTable table)
        {
            var firstFileID = table.Children.FirstOrDefault(x => !x.IsDirectory)?.FileIndex;
            if (firstFileID.HasValue)
            {
                return firstFileID;
            }
            else
            {
                foreach (var item in table.Children)
                {
                    firstFileID = GetFirstFNTFileID(item);
                    if (firstFileID.HasValue)
                    {
                        return firstFileID;
                    }
                    // Otherwise, keep looking
                }

                // Couldn't find a file
                return null;
            }
        }

        /// <summary>
        /// Gets the binary representation of the given filename table
        /// </summary>
        protected List<byte> EncodeFNT(string path, IDictionary<string, int> filenames)
        {
            // Generate the FNT
            int directoryCount = 0;
            int fileCount = 0;
            var table = BuildCurrentFNTChild(path, filenames, ref directoryCount, ref fileCount);

            // Encode the FNT
            var numberTablesWritten = 0;
            var nextSubDirOffset = (fileCount + directoryCount) * 8;
            var tables = new List<byte>(nextSubDirOffset);
            var subTables = new List<byte>();

            // Write root table
            tables.AddRange(BitConverter.GetBytes(nextSubDirOffset)); // Offset to Sub-table
            tables.AddRange(BitConverter.GetBytes(GetFirstFNTFileID(table) ?? -1)); // ID of first file in sub-table
            tables.AddRange(BitConverter.GetBytes(directoryCount)); // Special root definition
            numberTablesWritten += 1;

            // Write children
            WriteFNTChildren(tables, subTables, table, ref numberTablesWritten, ref nextSubDirOffset);

            // Concat tables
            tables.AddRange(subTables);

            return tables;
        }

        private void WriteFNTChildren(List<byte> tables, List<byte> subTables, FilenameTable table, ref int numberTablesWritten, ref int nextSubDirOffset)
        {
            int parentID = numberTablesWritten;

            // Write children
            foreach (var item in table.Children)
            {
                // Write subtable
                byte filenameLength = (byte)Math.Max(item.Name.Length, 127);

                if (item.IsDirectory)
                {
                    filenameLength &= 0x80; // Set directory flag
                }

                subTables.Add(filenameLength);
                subTables.AddRange(Encoding.ASCII.GetBytes(item.Name).Take(127));

                if (item.IsDirectory)
                {
                    subTables.AddRange(BitConverter.GetBytes((UInt16)(numberTablesWritten & 0xF000))); // Sub-directory ID
                }
                nextSubDirOffset += 1 + (filenameLength & 0x7F) + (item.IsDirectory ? 2 : 0);

                // Write table
                tables.AddRange(BitConverter.GetBytes(nextSubDirOffset)); // Offset to Sub-table
                tables.AddRange(BitConverter.GetBytes(GetFirstFNTFileID(table) ?? -1)); // ID of first file in sub-table
                tables.AddRange(BitConverter.GetBytes(parentID)); // Parent directory ID
                numberTablesWritten += 1;
            }

            // End sub table
            subTables.Add(0);
            nextSubDirOffset += 1;

            // Write childrens' children
            foreach (var item in table.Children)
            {
                WriteFNTChildren(tables, subTables, item, ref numberTablesWritten, ref nextSubDirOffset);
            }
        }

        /// <summary>
        /// Determines whether or not an additional 0xC of the ARM9 binary is needed
        /// </summary>
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
            var files = (this as IIOProvider).GetFiles("/", "*", false);

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
                    var dest = Path.Combine(targetDir, currentItem.TrimStart('/'));
                    if (!Directory.Exists(Path.GetDirectoryName(dest)))
                    {
                        lock (_unpackDirectoryCreateLock)
                        {
                            if (!Directory.Exists(Path.GetDirectoryName(dest)))
                            {
                                Directory.CreateDirectory(Path.GetDirectoryName(dest));
                            }
                        }
                    }
                    provider.WriteAllBytes(dest, (this as IIOProvider).ReadAllBytes(currentItem));
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
        private object _unpackDirectoryCreateLock = new object();
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

        private FileAllocationEntry? GetFATEntry(string path, bool throwIfNotFound = true)
        {
            var parts = GetPathParts(path);
            var partLower = parts[0].ToLower();
            switch (partLower)
            {
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
                default:
                    if (partLower == DataPath)
                    {
                        var currentEntry = FNT;
                        for (int i = 1; i < parts.Length; i += 1)
                        {
                            currentEntry = currentEntry?.Children.Where(x => x.Name.ToLower() == parts[i].ToLower()).FirstOrDefault();
                        }
                        if (currentEntry != null && !currentEntry.IsDirectory)
                        {
                            return FAT[currentEntry.FileIndex];
                        }
                    }
                    break;
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

        long IIOProvider.GetFileLength(string filename)
        {
            return GetFATEntry(filename).Value.Length;
        }

        bool IIOProvider.FileExists(string filename)
        {
            return CurrentIOProvider.FileExists(GetVirtualPath(filename)) || GetFATEntry(filename, false).HasValue;
        }

        private bool DirectoryExists(string[] parts)
        {
            if (parts.Length == 1)
            {
                switch (parts[0].ToLower())
                {
                    case "overlay":
                        return true;
                    case "overlay7":
                        return true;
                    default:
                        return parts[0].ToLower() == DataPath;
                }
            }
            else if (parts.Length == 0)
            {
                throw new ArgumentException("Argument cannot be empty", nameof(parts));
            }
            else
            {
                if (parts[0].ToLower() == DataPath)
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

        bool IIOProvider.DirectoryExists(string path)
        {
            return !BlacklistedPaths.Contains(FixPath(path)) && (CurrentIOProvider.DirectoryExists(GetVirtualPath(path)) || DirectoryExists(GetPathParts(path)));
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

        string[] IIOProvider.GetFiles(string path, string searchPattern, bool topDirectoryOnly)
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
                    if (!topDirectoryOnly)
                    {
                        output.AddRange((this as IIOProvider).GetFiles("/overlay", searchPattern, topDirectoryOnly));
                        output.AddRange((this as IIOProvider).GetFiles("/overlay7", searchPattern, topDirectoryOnly));
                        output.AddRange((this as IIOProvider).GetFiles("/" + DataPath, searchPattern, topDirectoryOnly));
                    }
                    return output.ToArray();
                case "overlay":
                    // Original files
                    for (int i = 0; i < Arm9OverlayTable.Count; i += 1)
                    {
                        var overlayPath = $"/overlay/overlay_{Arm9OverlayTable[i].FileID.ToString().PadLeft(4, '0')}.bin";
                        if (searchPatternRegex.IsMatch(Path.GetFileName(overlayPath)))
                        {
                            if (!BlacklistedPaths.Contains(overlayPath))
                            {
                                output.Add(overlayPath);
                            }
                        }
                    }

                    // Apply shadowed files
                    var virtualPath = GetVirtualPath(parts[0].ToLower());
                    if (CurrentIOProvider.DirectoryExists(virtualPath))
                    {
                        foreach (var item in CurrentIOProvider.GetFiles(virtualPath, "overlay_*.bin", true))
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
                    }
                    return output.ToArray();
                case "overlay7":
                    // Original files
                    for (int i = 0; i < Arm7OverlayTable.Count; i += 1)
                    {
                        var overlayPath = $"/overlay7/overlay_{Arm7OverlayTable[i].FileID.ToString().PadLeft(4, '0')}.bin";
                        if (searchPatternRegex.IsMatch(Path.GetFileName(overlayPath)))
                        {
                            if (!BlacklistedPaths.Contains(overlayPath))
                            {
                                output.Add(overlayPath);
                            }
                        }
                    }

                    // Apply shadowed files
                    var virtualPath7 = GetVirtualPath(parts[0].ToLower());
                    if (CurrentIOProvider.DirectoryExists(virtualPath7))
                    {
                        foreach (var item in CurrentIOProvider.GetFiles(virtualPath7, "overlay_*.bin", true))
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
                    }
                    return output.ToArray();
                default:
                    if (parts[0].ToLower() == DataPath)
                    {
                        // Get the desired directory
                        var currentEntry = FNT;
                        var pathBase = new StringBuilder();
                        pathBase.Append("/" + DataPath);
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
                        var virtualPathData = GetVirtualPath(path);
                        if (CurrentIOProvider.DirectoryExists(virtualPathData))
                        {
                            foreach (var item in CurrentIOProvider.GetFiles(virtualPathData, searchPattern, topDirectoryOnly))
                            {
                                var filePath = "/" + FileSystem.MakeRelativePath(item, VirtualPath);
                                if (!output.Contains(filePath))
                                {
                                    output.Add(filePath);
                                }
                            }
                        }
                    }
                    else
                    {
                        throw new FileNotFoundException(string.Format(Properties.Resources.ErrorRomFileNotFound, path), path);
                    }
                    break;
            }
            return output.ToArray();
        }

        string[] IIOProvider.GetDirectories(string path, bool topDirectoryOnly)
        {
            var output = new List<string>();
            var parts = GetPathParts(path);
            switch (parts[0].ToLower())
            {
                case "":
                    output.Add("/" + DataPath);
                    output.Add("/overlay");
                    output.Add("/overlay7");
                    break;
                case "overlay":
                case "overlay7":
                    // Overlays have no child directories
                    break;
                default:
                    if (parts[0].ToLower() == DataPath)
                    {
                        var currentEntry = FNT;
                        for (int i = 1; i < parts.Length; i += 1)
                        {
                            var partLower = parts[i].ToLower();
                            currentEntry = currentEntry?.Children.Where(x => x.Name.ToLower() == partLower && x.IsDirectory).FirstOrDefault();
                        }

                        if (currentEntry != null && currentEntry.IsDirectory)
                        {
                            output.AddRange(currentEntry.Children.Where(x => x.IsDirectory).Select(x => path + "/" + x.Name));
                        }
                        else
                        {
                            throw new FileNotFoundException(string.Format(Properties.Resources.ErrorRomFileNotFound, path), path);
                        }
                    }
                    else
                    {
                        throw new FileNotFoundException(string.Format(Properties.Resources.ErrorRomFileNotFound, path), path);
                    }
                    break;
            }
            if (!topDirectoryOnly)
            {
                foreach (var item in output)
                {
                    output.AddRange((this as IIOProvider).GetDirectories(item, topDirectoryOnly));
                }
            }
            return output.ToArray();
        }

        byte[] IIOProvider.ReadAllBytes(string filename)
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

            CurrentIOProvider.WriteAllBytes(GetVirtualPath(filename), data);
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
            if (CurrentIOProvider.FileExists(virtualPath))
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
            if (CurrentIOProvider.FileExists(virtualPath))
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

        private Stream OpenFile(string filename, FileAccess access)
        {
            var virtualPath = GetVirtualPath(filename);
            if (!CurrentIOProvider.DirectoryExists(virtualPath))
            {
                CurrentIOProvider.CreateDirectory(virtualPath);
            }
            CurrentIOProvider.WriteAllBytes(virtualPath, (this as IIOProvider).ReadAllBytes(filename));

            return File.Open(virtualPath, FileMode.OpenOrCreate, access);
        }

        Stream IIOProvider.OpenFile(string filename)
        {
            return OpenFile(filename, FileAccess.ReadWrite);
        }

        Stream IIOProvider.OpenFileReadOnly(string filename)
        {
            return OpenFile(filename, FileAccess.Read);
        }

        Stream IIOProvider.OpenFileWriteOnly(string filename)
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
