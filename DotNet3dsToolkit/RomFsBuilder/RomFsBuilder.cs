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
    internal static class RomFsBuilder
    {
        private const int PADDING_ALIGN = 16;

        public static void BuildRomFS(string rootDirectory, IFileSystem fileSystem, Stream outputStream, ExtractionProgressedToken progressToken = null)
        {
            var RomFiles = RomfsFile.LoadFromFileSystem(rootDirectory, fileSystem);
            var metadata = RomFsMetadataBuilder.BuildRomFSHeader(RomFiles, rootDirectory, fileSystem);
            MakeRomFSData(outputStream, fileSystem, RomFiles, metadata, progressToken);
        }

        private static void MakeRomFSData(Stream outputStream, IFileSystem fileSystem, List<RomfsFile> RomFiles, byte[] metadata, ExtractionProgressedToken progressToken = null)
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
            ivfcLevels[1].DataSize = (BitMath.Align(ivfcLevels[2].DataSize, ivfcLevels[2].HashBlockSize) / ivfcLevels[2].HashBlockSize) * 0x20; //0x20 per SHA256 hash
            ivfcLevels[0].DataSize = (BitMath.Align(ivfcLevels[1].DataSize, ivfcLevels[1].HashBlockSize) / ivfcLevels[1].HashBlockSize) * 0x20; //0x20 per SHA256 hash
            long MasterHashLen = (BitMath.Align(ivfcLevels[0].DataSize, ivfcLevels[0].HashBlockSize) / ivfcLevels[0].HashBlockSize) * 0x20;
            long lofs = 0;
            for (int i = 0; i < ivfcLevels.Length; i++)
            {
                ivfcLevels[i].HashOffset = lofs;
                lofs += BitMath.Align(ivfcLevels[i].DataSize, ivfcLevels[i].HashBlockSize);
            }
            int IVFC_MAGIC = 0x43465649; //IVFC
            int RESERVED = 0x0;
            int HeaderLen = 0x5C;
            int MEDIA_UNIT_SIZE = 0x200;
            byte[] SuperBlockHash = new byte[0x20];
            outputStream.Seek(0, SeekOrigin.Begin);
            outputStream.Write(BitConverter.GetBytes(IVFC_MAGIC), 0, 0x4);
            outputStream.Write(BitConverter.GetBytes(0x10000), 0, 0x4);
            outputStream.Write(BitConverter.GetBytes(MasterHashLen), 0, 0x4);
            for (int i = 0; i < ivfcLevels.Length; i++)
            {
                outputStream.Write(BitConverter.GetBytes(ivfcLevels[i].HashOffset), 0, 0x8);
                outputStream.Write(BitConverter.GetBytes(ivfcLevels[i].DataSize), 0, 0x8);
                outputStream.Write(BitConverter.GetBytes((int)(Math.Log(ivfcLevels[i].HashBlockSize, 2))), 0, 0x4);
                outputStream.Write(BitConverter.GetBytes(RESERVED), 0, 0x4);
            }
            outputStream.Write(BitConverter.GetBytes(HeaderLen), 0, 0x4);
            //IVFC Header is Written.
            outputStream.Seek(BitMath.Align(MasterHashLen + 0x60, ivfcLevels[0].HashBlockSize), SeekOrigin.Begin);
            outputStream.Write(metadata, 0, metadata.Length);
            long baseOfs = outputStream.Position;

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
                outputStream.Seek((long)(baseOfs + (long)RomFiles[i].Offset), SeekOrigin.Begin);
                using (var inStream = fileSystem.OpenFileReadOnly(RomFiles[i].FullName))
                {
                    while (inStream.Position < inStream.Length)
                    {
                        byte[] buffer = new byte[inStream.Length - inStream.Position > 0x100000 ? 0x100000 : inStream.Length - inStream.Position];
                        inStream.Read(buffer, 0, buffer.Length);
                        outputStream.Write(buffer, 0, buffer.Length);
                    }
                }

                if (progressToken != null)
                {
                    progressToken.ExtractedFileCount += 1;
                }
            }

            long hashBaseOfs = BitMath.Align(outputStream.Position, ivfcLevels[2].HashBlockSize);
            long hOfs = BitMath.Align(MasterHashLen, ivfcLevels[0].HashBlockSize);
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
                    outputStream.Seek(hOfs, SeekOrigin.Begin);
                    outputStream.Read(buffer, 0, (int)ivfcLevels[i].HashBlockSize);
                    hOfs = outputStream.Position;
                    byte[] hash = sha.ComputeHash(buffer);
                    outputStream.Seek(cOfs, SeekOrigin.Begin);
                    outputStream.Write(hash, 0, hash.Length);
                    cOfs = outputStream.Position;

                    if (progressToken != null)
                    {
                        progressToken.ExtractedFileCount += 1;
                    }
                }
                if (i == 2)
                {
                    long len = outputStream.Position;
                    if (len % 0x1000 != 0)
                    {
                        len = BitMath.Align(len, 0x1000);
                        byte[] buf = new byte[len - outputStream.Position];
                        outputStream.Write(buf, 0, buf.Length);
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
                        cOfs = BitMath.Align(HeaderLen, PADDING_ALIGN);
                    }
                }
            }
            outputStream.Seek(0, SeekOrigin.Begin);
            var SuperBlockLen = BitMath.Align(MasterHashLen + 0x60, MEDIA_UNIT_SIZE);
            byte[] MasterHashes = new byte[SuperBlockLen];
            outputStream.Read(MasterHashes, 0, (int)SuperBlockLen);
            SuperBlockHash = sha.ComputeHash(MasterHashes);

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
                Len = BitMath.Align(Len, 0x10);

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
