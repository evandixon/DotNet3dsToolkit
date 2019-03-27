using SkyEditor.IO.FileSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNet3dsToolkit.RomFsBuilder
{
    public class RomFsMetadataBuilder
    {
        private const int PADDING_ALIGN = 16;
        private const int ROMFS_UNUSED_ENTRY = -1;

        public static byte[] BuildRomFSHeader(List<RomfsFile> Entries, string directory, IFileSystem fileSystem)
        {
            var metadata = new RomFsMetadataBuilder();
            metadata.CalcRomfsSize(directory, fileSystem);
            metadata.PopulateRomfs(Entries, directory, fileSystem);
            return metadata.WriteMetaDataToStream();
        }

        public RomFsMetadataBuilder()
        {
            this.InfoHeader = new Romfs_InfoHeader();
            this.DirTableLen = 0;
            this.M_DirTableLen = 0;
            this.FileTableLen = 0;
            this.DirTable = new List<Romfs_DirEntry>();
            this.FileTable = new List<Romfs_FileEntry>();
            this.InfoHeader.HeaderLength = 0x28;
            this.InfoHeader.Sections = new Romfs_SectionHeader[]
            {
                new Romfs_SectionHeader(),
                new Romfs_SectionHeader(),
                new Romfs_SectionHeader(),
                new Romfs_SectionHeader()
            };

            this.DirHashTable = new List<int>();
            this.FileHashTable = new List<int>();
        }

        public Romfs_InfoHeader InfoHeader { get; set; }
        public int DirNum { get; set; }
        public int FileNum { get; set; }
        public List<int> DirHashTable { get; set; }
        public int M_DirHashTableEntry { get; set; }
        public List<Romfs_DirEntry> DirTable { get; set; }
        public int DirTableLen { get; set; }
        public int M_DirTableLen { get; set; }
        public List<int> FileHashTable { get; set; }
        public int M_FileHashTableEntry { get; set; }
        public List<Romfs_FileEntry> FileTable { get; set; }
        public int FileTableLen { get; set; }
        public int M_FileTableLen { get; set; }        

        private void CalcRomfsSize(string rootDirectory, IFileSystem fileSystem)
        {
            this.DirNum = 1;
            CalcDirSize(rootDirectory, fileSystem);

            this.M_DirHashTableEntry = GetHashTableEntryCount(this.DirNum);

            this.M_FileHashTableEntry = GetHashTableEntryCount(this.FileNum);

            int MetaDataSize = BitMath.Align(0x28 + this.M_DirHashTableEntry * 4 + this.M_DirTableLen + this.M_FileHashTableEntry * 4 + this.M_FileTableLen, PADDING_ALIGN);
            for (int i = 0; i < this.M_DirHashTableEntry; i++)
            {
                this.DirHashTable.Add(ROMFS_UNUSED_ENTRY);
            }
            for (int i = 0; i < this.M_FileHashTableEntry; i++)
            {
                this.FileHashTable.Add(ROMFS_UNUSED_ENTRY);
            }
            int Pos = this.InfoHeader.HeaderLength;
            for (int i = 0; i < 4; i++)
            {
                this.InfoHeader.Sections[i].Offset = Pos;
                int size = 0;
                switch (i)
                {
                    case 0:
                        size = this.M_DirHashTableEntry * 4;
                        break;
                    case 1:
                        size = this.M_DirTableLen;
                        break;
                    case 2:
                        size = this.M_FileHashTableEntry * 4;
                        break;
                    case 3:
                        size = this.M_FileTableLen;
                        break;
                }
                this.InfoHeader.Sections[i].Size = size;
                Pos += size;
            }
            this.InfoHeader.DataOffset = MetaDataSize;
        }

        private int GetHashTableEntryCount(int Entries)
        {
            int count = Entries;
            if (Entries < 3)
                count = 3;
            else if (count < 19)
                count |= 1;
            else
            {
                while (count % 2 == 0 || count % 3 == 0 || count % 5 == 0 || count % 7 == 0 || count % 11 == 0 || count % 13 == 0 || count % 17 == 0)
                {
                    count++;
                }
            }
            return count;
        }

        private void CalcDirSize(string rootDirectory, IFileSystem fileSystem)
        {
            if (this.M_DirTableLen == 0)
            {
                this.M_DirTableLen = 0x18;
            }
            else
            {
                this.M_DirTableLen += 0x18 + BitMath.Align(Path.GetFileName(rootDirectory.TrimEnd('/')).Length * 2, 4);
            }

            var filePaths = fileSystem.GetFiles(rootDirectory, "*", true);
            foreach (var filePath in filePaths)
            {
                var filename = Path.GetFileName(filePath);
                this.M_FileTableLen += 0x20 + BitMath.Align(filename.Length * 2, 4);
            }

            var dirPaths = fileSystem.GetDirectories(rootDirectory, true);
            foreach (var dirPath in dirPaths)
            {
                CalcDirSize(dirPath, fileSystem);
            }

            this.FileNum += filePaths.Length;
            this.DirNum += dirPaths.Length;
        }

        private void PopulateRomfs(List<RomfsFile> Entries, string root, IFileSystem fileSystem)
        {
            PopulateRomfs(Entries, root, root, fileSystem);
        }

        private void PopulateRomfs(List<RomfsFile> Entries, string root, string directory, IFileSystem fileSystem)
        {
            //Recursively Add All Directories to DirectoryTable
            AddDir(root, directory, fileSystem, 0, ROMFS_UNUSED_ENTRY);

            //Iteratively Add All Files to FileTable
            AddFiles(Entries);

            //Set HashKeyPointers, Build HashTables
            for (int i = 0; i < this.DirTable.Count; i++)
            {
                AddDirHashKey(i);
            }
            for (int i = 0; i < this.FileTable.Count; i++)
            {
                AddFileHashKey(i);
            }

            //Thats it.
        }

        private void AddDirHashKey(int index)
        {
            int parent = this.DirTable[index].ParentOffset;
            string Name = this.DirTable[index].Name;
            byte[] NArr = (index == 0) ? Encoding.Unicode.GetBytes("") : Encoding.Unicode.GetBytes(Name);
            uint hash = CalcPathHash(parent, NArr, 0, NArr.Length);
            int ind2 = (int)(hash % this.M_DirHashTableEntry);
            if (this.DirHashTable[ind2] == ROMFS_UNUSED_ENTRY)
            {
                this.DirHashTable[ind2] = this.DirTable[index].Offset;
            }
            else
            {
                int i = GetRomfsDirEntry(this.DirHashTable[ind2]);
                int tempindex = index;
                this.DirHashTable[ind2] = this.DirTable[index].Offset;
                while (true)
                {
                    if (this.DirTable[tempindex].HashKeyPointer == ROMFS_UNUSED_ENTRY)
                    {
                        this.DirTable[tempindex].HashKeyPointer = this.DirTable[i].Offset;
                        break;
                    }
                    else
                    {
                        i = tempindex;
                        tempindex = GetRomfsDirEntry(this.DirTable[i].HashKeyPointer);
                    }
                }
            }
        }

        private void AddFileHashKey(int index)
        {
            int parent = this.FileTable[index].ParentDirOffset;
            string Name = this.FileTable[index].Name;
            byte[] NArr = Encoding.Unicode.GetBytes(Name);
            uint hash = CalcPathHash(parent, NArr, 0, NArr.Length);
            int ind2 = (int)(hash % this.M_FileHashTableEntry);
            if (this.FileHashTable[ind2] == ROMFS_UNUSED_ENTRY)
            {
                this.FileHashTable[ind2] = this.FileTable[index].Offset;
            }
            else
            {
                int i = GetRomfsFileEntry(this.FileHashTable[ind2]);
                int tempindex = index;
                this.FileHashTable[ind2] = this.FileTable[index].Offset;
                while (true)
                {
                    if (this.FileTable[tempindex].HashKeyPointer == ROMFS_UNUSED_ENTRY)
                    {
                        this.FileTable[tempindex].HashKeyPointer = this.FileTable[i].Offset;
                        break;
                    }
                    else
                    {
                        i = tempindex;
                        tempindex = GetRomfsFileEntry(this.FileTable[i].HashKeyPointer);
                    }
                }
            }
        }

        private uint CalcPathHash(int ParentOffset, byte[] NameArray, int start, int len)
        {
            uint hash = (uint)ParentOffset ^ 123456789;
            for (int i = 0; i < NameArray.Length; i += 2)
            {
                hash = (uint)((hash >> 5) | (hash << 27));
                hash ^= (ushort)((NameArray[start + i]) | (NameArray[start + i + 1] << 8));
            }
            return hash;
        }

        private void AddDir(string root, string directory, IFileSystem fileSystem, int parent, int sibling)
        {
            AddDir(root, directory, fileSystem, parent, sibling, false);
            AddDir(root, directory, fileSystem, parent, sibling, true);
        }

        private void AddDir(string root, string directory, IFileSystem fileSystem, int parent, int sibling, bool DoSubs)
        {
            var dirName = Path.GetFileName(directory.TrimEnd('/'));
            var SubDirectories = fileSystem.GetDirectories(directory, true);
            if (!DoSubs)
            {
                int CurrentDir = this.DirTableLen;
                Romfs_DirEntry Entry = new Romfs_DirEntry
                {
                    ParentOffset = parent,
                    ChildOffset = ROMFS_UNUSED_ENTRY,
                    HashKeyPointer = ROMFS_UNUSED_ENTRY,
                    FileOffset = ROMFS_UNUSED_ENTRY,
                    SiblingOffset = sibling,
                    FullName = directory,
                    Name = (directory == root) ? "" : dirName,
                    Offset = CurrentDir
                };
                this.DirTable.Add(Entry);
                this.DirTableLen += (CurrentDir == 0) ? 0x18 : 0x18 + BitMath.Align(dirName.Length * 2, 4);
                int ParentIndex = GetRomfsDirEntry(directory);
                int poff = this.DirTable[ParentIndex].Offset;
            }
            else
            {
                int CurIndex = GetRomfsDirEntry(directory);
                int CurrentDir = this.DirTable[CurIndex].Offset;
                for (int i = 0; i < SubDirectories.Length; i++)
                {
                    AddDir(root, SubDirectories[i], fileSystem, CurrentDir, sibling, false);
                    if (i > 0)
                    {
                        string PrevFullName = SubDirectories[i - 1];
                        string ThisName = SubDirectories[i];
                        int PrevIndex = GetRomfsDirEntry(PrevFullName);
                        int ThisIndex = GetRomfsDirEntry(ThisName);
                        this.DirTable[PrevIndex].SiblingOffset = this.DirTable[ThisIndex].Offset;
                    }
                }
                for (int i = 0; i < SubDirectories.Length; i++)
                {
                    AddDir(root, SubDirectories[i], fileSystem, CurrentDir, sibling, true);
                }
            }
            if (SubDirectories.Length > 0)
            {
                int curindex = GetRomfsDirEntry(directory);
                int childindex = GetRomfsDirEntry(SubDirectories[0]);
                if (curindex > -1 && childindex > -1)
                {
                    this.DirTable[curindex].ChildOffset = this.DirTable[childindex].Offset;
                }
            }
        }

        private void AddFiles(List<RomfsFile> Entries)
        {
            string PrevDirPath = "";
            for (int i = 0; i < Entries.Count; i++)
            {
                var fileName = Path.GetFileName(Entries[i].FullName);
                Romfs_FileEntry Entry = new Romfs_FileEntry();
                string DirPath = Path.GetDirectoryName(Entries[i].FullName);
                int ParentIndex = GetRomfsDirEntry(DirPath);
                Entry.FullName = Entries[i].FullName;
                Entry.Offset = this.FileTableLen;
                Entry.ParentDirOffset = this.DirTable[ParentIndex].Offset;
                Entry.SiblingOffset = ROMFS_UNUSED_ENTRY;
                if (DirPath == PrevDirPath)
                {
                    this.FileTable[i - 1].SiblingOffset = Entry.Offset;
                }
                if (this.DirTable[ParentIndex].FileOffset == ROMFS_UNUSED_ENTRY)
                {
                    this.DirTable[ParentIndex].FileOffset = Entry.Offset;
                }
                Entry.HashKeyPointer = ROMFS_UNUSED_ENTRY;
                Entry.NameSize = fileName.Length * 2;
                Entry.Name = fileName;
                Entry.DataOffset = Entries[i].Offset;
                Entry.DataSize = Entries[i].Size;
                this.FileTable.Add(Entry);
                this.FileTableLen += 0x20 + BitMath.Align(fileName.Length * 2, 4);
                PrevDirPath = DirPath;
            }
        }

        private byte[] WriteMetaDataToStream()
        {
            using (var stream = new MemoryStream())
            {
                //First, InfoHeader.
                stream.Write(BitConverter.GetBytes(this.InfoHeader.HeaderLength), 0, 4);
                foreach (Romfs_SectionHeader SH in this.InfoHeader.Sections)
                {
                    stream.Write(BitConverter.GetBytes(SH.Offset), 0, 4);
                    stream.Write(BitConverter.GetBytes(SH.Size), 0, 4);
                }
                stream.Write(BitConverter.GetBytes(this.InfoHeader.DataOffset), 0, 4);

                //DirHashTable
                foreach (uint u in this.DirHashTable)
                {
                    stream.Write(BitConverter.GetBytes(u), 0, 4);
                }

                //DirTable
                foreach (Romfs_DirEntry dir in this.DirTable)
                {
                    stream.Write(BitConverter.GetBytes(dir.ParentOffset), 0, 4);
                    stream.Write(BitConverter.GetBytes(dir.SiblingOffset), 0, 4);
                    stream.Write(BitConverter.GetBytes(dir.ChildOffset), 0, 4);
                    stream.Write(BitConverter.GetBytes(dir.FileOffset), 0, 4);
                    stream.Write(BitConverter.GetBytes(dir.HashKeyPointer), 0, 4);
                    uint nlen = (uint)dir.Name.Length * 2;
                    stream.Write(BitConverter.GetBytes(nlen), 0, 4);
                    byte[] NameArray = new byte[BitMath.Align(nlen, 4)];
                    Array.Copy(Encoding.Unicode.GetBytes(dir.Name), 0, NameArray, 0, nlen);
                    stream.Write(NameArray, 0, NameArray.Length);
                }

                //FileHashTable
                foreach (uint u in this.FileHashTable)
                {
                    stream.Write(BitConverter.GetBytes(u), 0, 4);
                }

                //FileTable
                foreach (Romfs_FileEntry file in this.FileTable)
                {
                    stream.Write(BitConverter.GetBytes(file.ParentDirOffset), 0, 4);
                    stream.Write(BitConverter.GetBytes(file.SiblingOffset), 0, 4);
                    stream.Write(BitConverter.GetBytes(file.DataOffset), 0, 8);
                    stream.Write(BitConverter.GetBytes(file.DataSize), 0, 8);
                    stream.Write(BitConverter.GetBytes(file.HashKeyPointer), 0, 4);
                    uint nlen = (uint)file.Name.Length * 2;
                    stream.Write(BitConverter.GetBytes(nlen), 0, 4);
                    byte[] NameArray = new byte[BitMath.Align(nlen, 4)];
                    Array.Copy(Encoding.Unicode.GetBytes(file.Name), 0, NameArray, 0, nlen);
                    stream.Write(NameArray, 0, NameArray.Length);
                }

                //Padding
                while (stream.Position % PADDING_ALIGN != 0)
                    stream.Write(new byte[PADDING_ALIGN - (stream.Position % 0x10)], 0, (int)(PADDING_ALIGN - (stream.Position % 0x10)));
                //All Done.

                return stream.ToArray();
            }
        }

        //GetRomfs[...]Entry Functions are all O(n)

        private int GetRomfsDirEntry(string FullName)
        {
            for (int i = 0; i < this.DirTable.Count; i++)
            {
                if (this.DirTable[i].FullName.TrimEnd('/') == FullName.Replace("\\", "/").TrimEnd('/'))
                {
                    return i;
                }
            }
            return -1;
        }

        private int GetRomfsDirEntry(int Offset)
        {
            for (int i = 0; i < this.DirTable.Count; i++)
            {
                if (this.DirTable[i].Offset == Offset)
                {
                    return i;
                }
            }
            return -1;
        }

        private int GetRomfsFileEntry(int Offset)
        {
            for (int i = 0; i < this.FileTable.Count; i++)
            {
                if (this.FileTable[i].Offset == Offset)
                {
                    return i;
                }
            }
            return -1;
        }

        public class Romfs_SectionHeader
        {
            public int Offset { get; set; }
            public int Size { get; set; }
        }

        public class Romfs_InfoHeader
        {
            public int HeaderLength { get; set; }
            public Romfs_SectionHeader[] Sections { get; set; }
            public int DataOffset { get; set; }
        }

        public class Romfs_DirEntry
        {
            public int ParentOffset { get; set; }
            public int SiblingOffset { get; set; }
            public int ChildOffset { get; set; }
            public int FileOffset { get; set; }
            public int HashKeyPointer { get; set; }
            public string Name { get; set; }
            public string FullName { get; set; }
            public int Offset { get; set; }
        }

        public class Romfs_FileEntry
        {
            public int ParentDirOffset { get; set; }
            public int SiblingOffset { get; set; }
            public long DataOffset { get; set; }
            public long DataSize { get; set; }
            public int HashKeyPointer { get; set; }
            public int NameSize { get; set; }
            public string Name { get; set; }
            public string FullName { get; set; }
            public int Offset { get; set; }
        }
    }
}
