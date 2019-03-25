using SkyEditor.IO.FileSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static DotNet3dsToolkit.Ctr.RomFs;

namespace DotNet3dsToolkit.RomFsBuilder
{
    public static class Helpers
    {
        private const int PADDING_ALIGN = 16;

        private static void BuildRomFS(string rootDirectory, string outputFile, IFileSystem fileSystem)
        {
            var RomFiles = RomfsFile.LoadFromFileSystem(rootDirectory, fileSystem);

            using (MemoryStream memoryStream = new MemoryStream())
            {
                MetadataBuilder mdb = new MetadataBuilder();
                mdb.BuildRomFSHeader(memoryStream, RomFiles, rootDirectory);
                MakeRomFSData(outputFile, RomFiles, memoryStream);
            }
        }

        public static int Align(int input, int alignsize)
        {
            int output = input;
            if (output % alignsize != 0)
            {
                output += (alignsize - (output % alignsize));
            }
            return output;
        }

        public static long Align(long input, long alignsize)
        {
            long output = input;
            if (output % alignsize != 0)
            {
                output += (alignsize - (output % alignsize));
            }
            return output;
        }

        private static void MakeRomFSData(string outputFile, List<RomfsFile> RomFiles, MemoryStream metadata, ExtractionProgressedToken progressToken = null)
        {
            // Computing IVFC Header Data...
            var ivfcLevels = new IvfcLevelLocation[3];
            for (int i = 0; i < ivfcLevels.Length; i++)
            {
                ivfcLevels[i] = new IvfcLevelLocation
                {
                    HashBlockSize = 0x1000
                };
            }
            ivfcLevels[2].DataSize = RomfsFile.GetDataBlockLength(RomFiles, metadata.Length);
            ivfcLevels[1].DataSize = (Align(ivfcLevels[2].DataSize, ivfcLevels[2].HashBlockSize) / ivfcLevels[2].HashBlockSize) * 0x20; //0x20 per SHA256 hash
            ivfcLevels[0].DataSize = (Align(ivfcLevels[1].DataSize, ivfcLevels[1].HashBlockSize) / ivfcLevels[1].HashBlockSize) * 0x20; //0x20 per SHA256 hash
            long MasterHashLen = (Align(ivfcLevels[0].DataSize, ivfcLevels[0].HashBlockSize) / ivfcLevels[0].HashBlockSize) * 0x20;
            long lofs = 0;
            for (int i = 0; i < ivfcLevels.Length; i++)
            {
                ivfcLevels[i].HashOffset = lofs;
                lofs += Align(ivfcLevels[i].DataSize, ivfcLevels[i].HashBlockSize);
            }
            int IVFC_MAGIC = 0x43465649; //IVFC
            int RESERVED = 0x0;
            int HeaderLen = 0x5C;
            int MEDIA_UNIT_SIZE = 0x200;
            byte[] SuperBlockHash = new byte[0x20];
            using (var OutFileStream = new FileStream(outputFile, FileMode.Create, FileAccess.ReadWrite))
            {
                OutFileStream.Seek(0, SeekOrigin.Begin);
                OutFileStream.Write(BitConverter.GetBytes(IVFC_MAGIC), 0, 0x4);
                OutFileStream.Write(BitConverter.GetBytes(0x10000), 0, 0x4);
                OutFileStream.Write(BitConverter.GetBytes(MasterHashLen), 0, 0x4);
                for (int i = 0; i < ivfcLevels.Length; i++)
                {
                    OutFileStream.Write(BitConverter.GetBytes(ivfcLevels[i].HashOffset), 0, 0x8);
                    OutFileStream.Write(BitConverter.GetBytes(ivfcLevels[i].DataSize), 0, 0x8);
                    OutFileStream.Write(BitConverter.GetBytes((int)(Math.Log(ivfcLevels[i].HashBlockSize, 2))), 0, 0x4);
                    OutFileStream.Write(BitConverter.GetBytes(RESERVED), 0, 0x4);
                }
                OutFileStream.Write(BitConverter.GetBytes(HeaderLen), 0, 0x4);
                //IVFC Header is Written.
                OutFileStream.Seek((long)Align(MasterHashLen + 0x60, ivfcLevels[0].HashBlockSize), SeekOrigin.Begin);
                byte[] metadataArray = metadata.ToArray();
                OutFileStream.Write(metadataArray, 0, metadataArray.Length);
                long baseOfs = OutFileStream.Position;

                // Initialize progress token
                // The maximum will be the total file count and each ivfc block
                if (progressToken != null)
                {
                    progressToken.ExtractedFileCount = 0;
                    progressToken.TotalFileCount = RomFiles.Count + ivfcLevels.Select(l => (int)(l.DataSize / l.HashBlockSize)).Sum();
                }

                // Writing Level 2 Data...
                for (int i = 0; i < RomFiles.Count; i++)
                {
                    OutFileStream.Seek((long)(baseOfs + (long)RomFiles[i].Offset), SeekOrigin.Begin);
                    using (FileStream inStream = new FileStream(RomFiles[i].FullName, FileMode.Open, FileAccess.Read))
                    {
                        while (inStream.Position < inStream.Length)
                        {
                            byte[] buffer = new byte[inStream.Length - inStream.Position > 0x100000 ? 0x100000 : inStream.Length - inStream.Position];
                            inStream.Read(buffer, 0, buffer.Length);
                            OutFileStream.Write(buffer, 0, buffer.Length);
                        }
                    }

                    if (progressToken != null)
                    {
                        progressToken.ExtractedFileCount += 1;
                    }
                }

                long hashBaseOfs = Align(OutFileStream.Position, ivfcLevels[2].HashBlockSize);
                long hOfs = Align(MasterHashLen, ivfcLevels[0].HashBlockSize);
                long cOfs = hashBaseOfs + ivfcLevels[1].HashOffset;
                SHA256Managed sha = new SHA256Managed();
                for (int i = ivfcLevels.Length - 1; i >= 0; i--)
                {
                    // Computing Level {i} Hashes...
                    byte[] buffer = new byte[(int)ivfcLevels[i].HashBlockSize];
                    if (progressToken != null)
                    {
                        progressToken.ExtractedFileCount = 0;
                        progressToken.TotalFileCount = (int)(ivfcLevels[i].DataSize / ivfcLevels[i].HashBlockSize);
                    }
                    for (long ofs = 0; ofs < (long)ivfcLevels[i].DataSize; ofs += ivfcLevels[i].HashBlockSize)
                    {
                        OutFileStream.Seek(hOfs, SeekOrigin.Begin);
                        OutFileStream.Read(buffer, 0, (int)ivfcLevels[i].HashBlockSize);
                        hOfs = OutFileStream.Position;
                        byte[] hash = sha.ComputeHash(buffer);
                        OutFileStream.Seek(cOfs, SeekOrigin.Begin);
                        OutFileStream.Write(hash, 0, hash.Length);
                        cOfs = OutFileStream.Position;

                        if (progressToken != null)
                        {
                            progressToken.ExtractedFileCount += 1;
                        }
                    }
                    if (i == 2)
                    {
                        long len = OutFileStream.Position;
                        if (len % 0x1000 != 0)
                        {
                            len = Align(len, 0x1000);
                            byte[] buf = new byte[len - OutFileStream.Position];
                            OutFileStream.Write(buf, 0, buf.Length);
                        }
                    }
                    if (i > 0)
                    {
                        hOfs = hashBaseOfs + (long)ivfcLevels[i - 1].HashOffset;
                        if (i > 1)
                        {
                            cOfs = hashBaseOfs + (long)ivfcLevels[i - 2].HashOffset;
                        }
                        else
                        {
                            cOfs = (long)Align(HeaderLen, PADDING_ALIGN);
                        }
                    }
                }
                OutFileStream.Seek(0, SeekOrigin.Begin);
                uint SuperBlockLen = (uint)Align(MasterHashLen + 0x60, MEDIA_UNIT_SIZE);
                byte[] MasterHashes = new byte[SuperBlockLen];
                OutFileStream.Read(MasterHashes, 0, (int)SuperBlockLen);
                SuperBlockHash = sha.ComputeHash(MasterHashes);
            }
        }
    }

    public class RomfsFile
    {
        public static List<RomfsFile> LoadFromFileSystem(string rootDirectory, IFileSystem fileSystem)
        {
            var list = new List<RomfsFile>();
            long Len = 0;
            foreach (var filePath in fileSystem.GetFiles(rootDirectory, "*", false))
            {
                Len = Helpers.Align(Len, 0x10);

                var output = new RomfsFile
                {
                    FullName = filePath,
                    PathName = filePath.Replace(Path.GetFullPath(rootDirectory), "").Replace("\\", "/"),
                    Offset = Len,
                    Size = fileSystem.GetFileLength(filePath)
                };
                list.Add(output);

                Len += output.Size;
            }
            return list;
        }

        public string PathName { get; set; }
        public long Offset { get; set; }
        public long Size { get; set; }
        public string FullName { get; set; }

        public static long GetDataBlockLength(List<RomfsFile> files, long PreData)
        {
            return (files.Count == 0) ? PreData : PreData + files[files.Count - 1].Offset + files[files.Count - 1].Size;
        }
    }

    public class FileToAdd
    {              
        public string FilePath { get; set; }
        public long Offset { get; set; }
        public long Size { get; set; }
    }
}
