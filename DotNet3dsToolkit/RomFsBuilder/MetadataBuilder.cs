using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNet3dsToolkit.RomFsBuilder
{
    public class MetadataBuilder
    {
        public string ROOT_DIR;
        private const int PADDING_ALIGN = 16;
        private const int ROMFS_UNUSED_ENTRY = -1;

        public void BuildRomFSHeader(MemoryStream romfs_stream, List<RomfsFile> Entries, string DIR)
        {
            ROOT_DIR = DIR;

            Romfs_MetaData MetaData = new Romfs_MetaData();

            InitializeMetaData(MetaData);

            CalcRomfsSize(MetaData);

            PopulateRomfs(MetaData, Entries);

            WriteMetaDataToStream(MetaData, romfs_stream);
        }

        private void InitializeMetaData(Romfs_MetaData MetaData)
        {
            MetaData.InfoHeader = new Romfs_InfoHeader();
            MetaData.DirTable = new Romfs_DirTable();
            MetaData.DirTableLen = 0;
            MetaData.M_DirTableLen = 0;
            MetaData.FileTable = new Romfs_FileTable();
            MetaData.FileTableLen = 0;
            MetaData.DirTable.DirectoryTable = new List<Romfs_DirEntry>();
            MetaData.FileTable.FileTable = new List<Romfs_FileEntry>();
            MetaData.InfoHeader.HeaderLength = 0x28;
            MetaData.InfoHeader.Sections = new Romfs_SectionHeader[4];
            MetaData.DirHashTable = new List<int>();
            MetaData.FileHashTable = new List<int>();
        }

        private void CalcRomfsSize(Romfs_MetaData MetaData)
        {
            MetaData.DirNum = 1;
            DirectoryInfo Root_DI = new DirectoryInfo(ROOT_DIR);
            CalcDirSize(MetaData, Root_DI);


            MetaData.M_DirHashTableEntry = GetHashTableEntryCount(MetaData.DirNum);

            MetaData.M_FileHashTableEntry = GetHashTableEntryCount(MetaData.FileNum);


            int MetaDataSize = Helpers.Align(0x28 + MetaData.M_DirHashTableEntry * 4 + MetaData.M_DirTableLen + MetaData.M_FileHashTableEntry * 4 + MetaData.M_FileTableLen, PADDING_ALIGN);
            for (int i = 0; i < MetaData.M_DirHashTableEntry; i++)
            {
                MetaData.DirHashTable.Add(ROMFS_UNUSED_ENTRY);
            }
            for (int i = 0; i < MetaData.M_FileHashTableEntry; i++)
            {
                MetaData.FileHashTable.Add(ROMFS_UNUSED_ENTRY);
            }
            int Pos = MetaData.InfoHeader.HeaderLength;
            for (int i = 0; i < 4; i++)
            {
                MetaData.InfoHeader.Sections[i].Offset = Pos;
                int size = 0;
                switch (i)
                {
                    case 0:
                        size = MetaData.M_DirHashTableEntry * 4;
                        break;
                    case 1:
                        size = MetaData.M_DirTableLen;
                        break;
                    case 2:
                        size = MetaData.M_FileHashTableEntry * 4;
                        break;
                    case 3:
                        size = MetaData.M_FileTableLen;
                        break;
                }
                MetaData.InfoHeader.Sections[i].Size = size;
                Pos += size;
            }
            MetaData.InfoHeader.DataOffset = MetaDataSize;
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

        private void CalcDirSize(Romfs_MetaData MetaData, DirectoryInfo dir)
        {
            if (MetaData.M_DirTableLen == 0)
            {
                MetaData.M_DirTableLen = 0x18;
            }
            else
            {
                MetaData.M_DirTableLen += 0x18 + Helpers.Align(dir.Name.Length * 2, 4);
            }

            FileInfo[] files = dir.GetFiles();
            for (int i = 0; i < files.Length; i++)
            {
                MetaData.M_FileTableLen += 0x20 + Helpers.Align(files[i].Name.Length * 2, 4);
            }

            DirectoryInfo[] SubDirectories = dir.GetDirectories();
            for (int i = 0; i < SubDirectories.Length; i++)
            {
                CalcDirSize(MetaData, SubDirectories[i]);
            }

            MetaData.FileNum += files.Length;
            MetaData.DirNum += SubDirectories.Length;
        }

        private void PopulateRomfs(Romfs_MetaData MetaData, List<RomfsFile> Entries)
        {
            //Recursively Add All Directories to DirectoryTable
            AddDir(MetaData, new DirectoryInfo(ROOT_DIR), 0, ROMFS_UNUSED_ENTRY);

            //Iteratively Add All Files to FileTable
            AddFiles(MetaData, Entries);

            //Set HashKeyPointers, Build HashTables
            PopulateHashTables(MetaData);

            //Thats it.
        }

        private void PopulateHashTables(Romfs_MetaData MetaData)
        {
            for (int i = 0; i < MetaData.DirTable.DirectoryTable.Count; i++)
            {
                AddDirHashKey(MetaData, i);
            }
            for (int i = 0; i < MetaData.FileTable.FileTable.Count; i++)
            {
                AddFileHashKey(MetaData, i);
            }
        }

        private void AddDirHashKey(Romfs_MetaData MetaData, int index)
        {
            int parent = MetaData.DirTable.DirectoryTable[index].ParentOffset;
            string Name = MetaData.DirTable.DirectoryTable[index].Name;
            byte[] NArr = (index == 0) ? Encoding.Unicode.GetBytes("") : Encoding.Unicode.GetBytes(Name);
            int hash = CalcPathHash(parent, NArr, 0, NArr.Length);
            int ind2 = (int)(hash % MetaData.M_DirHashTableEntry);
            if (MetaData.DirHashTable[ind2] == ROMFS_UNUSED_ENTRY)
            {
                MetaData.DirHashTable[ind2] = MetaData.DirTable.DirectoryTable[index].Offset;
            }
            else
            {
                int i = GetRomfsDirEntry(MetaData, MetaData.DirHashTable[ind2]);
                int tempindex = index;
                MetaData.DirHashTable[ind2] = MetaData.DirTable.DirectoryTable[index].Offset;
                while (true)
                {
                    if (MetaData.DirTable.DirectoryTable[tempindex].HashKeyPointer == ROMFS_UNUSED_ENTRY)
                    {
                        MetaData.DirTable.DirectoryTable[tempindex].HashKeyPointer = MetaData.DirTable.DirectoryTable[i].Offset;
                        break;
                    }
                    else
                    {
                        i = tempindex;
                        tempindex = GetRomfsDirEntry(MetaData, MetaData.DirTable.DirectoryTable[i].HashKeyPointer);
                    }
                }
            }
        }

        private void AddFileHashKey(Romfs_MetaData MetaData, int index)
        {
            int parent = MetaData.FileTable.FileTable[index].ParentDirOffset;
            string Name = MetaData.FileTable.FileTable[index].Name;
            byte[] NArr = Encoding.Unicode.GetBytes(Name);
            int hash = CalcPathHash(parent, NArr, 0, NArr.Length);
            int ind2 = (int)(hash % MetaData.M_FileHashTableEntry);
            if (MetaData.FileHashTable[ind2] == ROMFS_UNUSED_ENTRY)
            {
                MetaData.FileHashTable[ind2] = MetaData.FileTable.FileTable[index].Offset;
            }
            else
            {
                int i = GetRomfsFileEntry(MetaData, MetaData.FileHashTable[ind2]);
                int tempindex = index;
                MetaData.FileHashTable[ind2] = MetaData.FileTable.FileTable[index].Offset;
                while (true)
                {
                    if (MetaData.FileTable.FileTable[tempindex].HashKeyPointer == ROMFS_UNUSED_ENTRY)
                    {
                        MetaData.FileTable.FileTable[tempindex].HashKeyPointer = MetaData.FileTable.FileTable[i].Offset;
                        break;
                    }
                    else
                    {
                        i = tempindex;
                        tempindex = GetRomfsFileEntry(MetaData, MetaData.FileTable.FileTable[i].HashKeyPointer);
                    }
                }
            }
        }

        private int CalcPathHash(int ParentOffset, byte[] NameArray, int start, int len)
        {
            int hash = ParentOffset ^ 123456789;
            for (int i = 0; i < NameArray.Length; i += 2)
            {
                hash = (int)((hash >> 5) | (hash << 27));
                hash ^= (ushort)((NameArray[start + i]) | (NameArray[start + i + 1] << 8));
            }
            return hash;
        }

        private void AddDir(Romfs_MetaData MetaData, DirectoryInfo Dir, int parent, int sibling)
        {
            AddDir(MetaData, Dir, parent, sibling, false);
            AddDir(MetaData, Dir, parent, sibling, true);
        }

        private void AddDir(Romfs_MetaData MetaData, DirectoryInfo Dir, int parent, int sibling, bool DoSubs)
        {
            DirectoryInfo[] SubDirectories = Dir.GetDirectories();
            if (!DoSubs)
            {
                int CurrentDir = MetaData.DirTableLen;
                Romfs_DirEntry Entry = new Romfs_DirEntry();
                Entry.ParentOffset = parent;
                Entry.ChildOffset = Entry.HashKeyPointer = Entry.FileOffset = ROMFS_UNUSED_ENTRY;
                Entry.SiblingOffset = sibling;
                Entry.FullName = Dir.FullName;
                Entry.Name = (Entry.FullName == ROOT_DIR) ? "" : Dir.Name;
                Entry.Offset = CurrentDir;
                MetaData.DirTable.DirectoryTable.Add(Entry);
                MetaData.DirTableLen += (CurrentDir == 0) ? 0x18 : 0x18 + Helpers.Align(Dir.Name.Length * 2, 4);
                int ParentIndex = GetRomfsDirEntry(MetaData, Dir.FullName);
                int poff = MetaData.DirTable.DirectoryTable[ParentIndex].Offset;
            }
            else
            {
                int CurIndex = GetRomfsDirEntry(MetaData, Dir.FullName);
                int CurrentDir = MetaData.DirTable.DirectoryTable[CurIndex].Offset;
                for (int i = 0; i < SubDirectories.Length; i++)
                {
                    AddDir(MetaData, SubDirectories[i], CurrentDir, sibling, false);
                    if (i > 0)
                    {
                        string PrevFullName = SubDirectories[i - 1].FullName;
                        string ThisName = SubDirectories[i].FullName;
                        int PrevIndex = GetRomfsDirEntry(MetaData, PrevFullName);
                        int ThisIndex = GetRomfsDirEntry(MetaData, ThisName);
                        MetaData.DirTable.DirectoryTable[PrevIndex].SiblingOffset = MetaData.DirTable.DirectoryTable[ThisIndex].Offset;
                    }
                }
                for (int i = 0; i < SubDirectories.Length; i++)
                {
                    AddDir(MetaData, SubDirectories[i], CurrentDir, sibling, true);
                }
            }
            if (SubDirectories.Length > 0)
            {
                int curindex = GetRomfsDirEntry(MetaData, Dir.FullName);
                int childindex = GetRomfsDirEntry(MetaData, SubDirectories[0].FullName);
                if (curindex > -1 && childindex > -1)
                    MetaData.DirTable.DirectoryTable[curindex].ChildOffset = MetaData.DirTable.DirectoryTable[childindex].Offset;
            }
        }

        private void AddFiles(Romfs_MetaData MetaData, List<RomfsFile> Entries)
        {
            string PrevDirPath = "";
            for (int i = 0; i < Entries.Count; i++)
            {
                FileInfo file = new FileInfo(Entries[i].FullName);
                Romfs_FileEntry Entry = new Romfs_FileEntry();
                string DirPath = Path.GetDirectoryName(Entries[i].FullName);
                int ParentIndex = GetRomfsDirEntry(MetaData, DirPath);
                Entry.FullName = Entries[i].FullName;
                Entry.Offset = MetaData.FileTableLen;
                Entry.ParentDirOffset = MetaData.DirTable.DirectoryTable[ParentIndex].Offset;
                Entry.SiblingOffset = ROMFS_UNUSED_ENTRY;
                if (DirPath == PrevDirPath)
                {
                    MetaData.FileTable.FileTable[i - 1].SiblingOffset = Entry.Offset;
                }
                if (MetaData.DirTable.DirectoryTable[ParentIndex].FileOffset == ROMFS_UNUSED_ENTRY)
                {
                    MetaData.DirTable.DirectoryTable[ParentIndex].FileOffset = Entry.Offset;
                }
                Entry.HashKeyPointer = ROMFS_UNUSED_ENTRY;
                Entry.NameSize = file.Name.Length * 2;
                Entry.Name = file.Name;
                Entry.DataOffset = Entries[i].Offset;
                Entry.DataSize = Entries[i].Size;
                MetaData.FileTable.FileTable.Add(Entry);
                MetaData.FileTableLen += 0x20 + Helpers.Align(file.Name.Length * 2, 4);
                PrevDirPath = DirPath;
            }
        }

        private void WriteMetaDataToStream(Romfs_MetaData MetaData, MemoryStream stream)
        {
            //First, InfoHeader.
            stream.Write(BitConverter.GetBytes(MetaData.InfoHeader.HeaderLength), 0, 4);
            foreach (Romfs_SectionHeader SH in MetaData.InfoHeader.Sections)
            {
                stream.Write(BitConverter.GetBytes(SH.Offset), 0, 4);
                stream.Write(BitConverter.GetBytes(SH.Size), 0, 4);
            }
            stream.Write(BitConverter.GetBytes(MetaData.InfoHeader.DataOffset), 0, 4);

            //DirHashTable
            foreach (uint u in MetaData.DirHashTable)
            {
                stream.Write(BitConverter.GetBytes(u), 0, 4);
            }

            //DirTable
            foreach (Romfs_DirEntry dir in MetaData.DirTable.DirectoryTable)
            {
                stream.Write(BitConverter.GetBytes(dir.ParentOffset), 0, 4);
                stream.Write(BitConverter.GetBytes(dir.SiblingOffset), 0, 4);
                stream.Write(BitConverter.GetBytes(dir.ChildOffset), 0, 4);
                stream.Write(BitConverter.GetBytes(dir.FileOffset), 0, 4);
                stream.Write(BitConverter.GetBytes(dir.HashKeyPointer), 0, 4);
                uint nlen = (uint)dir.Name.Length * 2;
                stream.Write(BitConverter.GetBytes(nlen), 0, 4);
                byte[] NameArray = new byte[Helpers.Align(nlen, 4)];
                Array.Copy(Encoding.Unicode.GetBytes(dir.Name), 0, NameArray, 0, nlen);
                stream.Write(NameArray, 0, NameArray.Length);
            }

            //FileHashTable
            foreach (uint u in MetaData.FileHashTable)
            {
                stream.Write(BitConverter.GetBytes(u), 0, 4);
            }

            //FileTable
            foreach (Romfs_FileEntry file in MetaData.FileTable.FileTable)
            {
                stream.Write(BitConverter.GetBytes(file.ParentDirOffset), 0, 4);
                stream.Write(BitConverter.GetBytes(file.SiblingOffset), 0, 4);
                stream.Write(BitConverter.GetBytes(file.DataOffset), 0, 8);
                stream.Write(BitConverter.GetBytes(file.DataSize), 0, 8);
                stream.Write(BitConverter.GetBytes(file.HashKeyPointer), 0, 4);
                uint nlen = (uint)file.Name.Length * 2;
                stream.Write(BitConverter.GetBytes(nlen), 0, 4);
                byte[] NameArray = new byte[Helpers.Align(nlen, 4)];
                Array.Copy(Encoding.Unicode.GetBytes(file.Name), 0, NameArray, 0, nlen);
                stream.Write(NameArray, 0, NameArray.Length);
            }

            //Padding
            while (stream.Position % PADDING_ALIGN != 0)
                stream.Write(new byte[PADDING_ALIGN - (stream.Position % 0x10)], 0, (int)(PADDING_ALIGN - (stream.Position % 0x10)));
            //All Done.
        }

        //GetRomfs[...]Entry Functions are all O(n)

        private int GetRomfsDirEntry(Romfs_MetaData MetaData, string FullName)
        {
            for (int i = 0; i < MetaData.DirTable.DirectoryTable.Count; i++)
            {
                if (MetaData.DirTable.DirectoryTable[i].FullName == FullName)
                {
                    return i;
                }
            }
            return -1;
        }

        private int GetRomfsDirEntry(Romfs_MetaData MetaData, int Offset)
        {
            for (int i = 0; i < MetaData.DirTable.DirectoryTable.Count; i++)
            {
                if (MetaData.DirTable.DirectoryTable[i].Offset == Offset)
                {
                    return i;
                }
            }
            return -1;
        }

        private int GetRomfsFileEntry(Romfs_MetaData MetaData, int Offset)
        {
            for (int i = 0; i < MetaData.FileTable.FileTable.Count; i++)
            {
                if (MetaData.FileTable.FileTable[i].Offset == Offset)
                {
                    return i;
                }
            }
            return -1;
        }

        public class Romfs_MetaData
        {
            public Romfs_InfoHeader InfoHeader;
            public int DirNum;
            public int FileNum;
            public List<int> DirHashTable;
            public int M_DirHashTableEntry;
            public Romfs_DirTable DirTable;
            public int DirTableLen;
            public int M_DirTableLen;
            public List<int> FileHashTable;
            public int M_FileHashTableEntry;
            public Romfs_FileTable FileTable;
            public int FileTableLen;
            public int M_FileTableLen;
        }

        public struct Romfs_SectionHeader
        {
            public int Offset;
            public int Size;
        }

        public struct Romfs_InfoHeader
        {
            public int HeaderLength;
            public Romfs_SectionHeader[] Sections;
            public int DataOffset;
        }

        public class Romfs_DirTable
        {
            public List<Romfs_DirEntry> DirectoryTable;
        }

        public class Romfs_FileTable
        {
            public List<Romfs_FileEntry> FileTable;
        }

        public class Romfs_DirEntry
        {
            public int ParentOffset;
            public int SiblingOffset;
            public int ChildOffset;
            public int FileOffset;
            public int HashKeyPointer;
            public string Name;
            public string FullName;
            public int Offset;
        }

        public class Romfs_FileEntry
        {
            public int ParentDirOffset;
            public int SiblingOffset;
            public long DataOffset;
            public long DataSize;
            public int HashKeyPointer;
            public int NameSize;
            public string Name;
            public string FullName;
            public int Offset;
        }
    }
}
