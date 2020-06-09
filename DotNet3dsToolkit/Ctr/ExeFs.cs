using SkyEditor.IO;
using SkyEditor.IO.Binary;
using SkyEditor.IO.FileSystem;
using SkyEditor.Utilities.AsyncFor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DotNet3dsToolkit.Ctr
{
    public class ExeFs
    {
        public static async Task<bool> IsExeFs(IReadOnlyBinaryDataAccessor file)
        {
            try
            {
                if (file.Length < 0x200)
                {
                    return false;
                }

                var exefs = await ExeFs.Load(file).ConfigureAwait(false);
                return exefs.Files.Any() && exefs.AreAllHashesValid();
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Loads an existing executable file system from the given data.
        /// </summary>
        /// <param name="data">Accessor to the raw data to load as a executable file system</param>
        /// <returns>The executable file system the given data represents</returns>
        public static async Task<ExeFs> Load(IReadOnlyBinaryDataAccessor exeFsData)
        {
            var exefs = new ExeFs(exeFsData);
            await exefs.Initialize().ConfigureAwait(false);
            return exefs;
        }

        /// <summary>
        /// Builds a new executable file system from the given directory
        /// </summary>
        /// <param name="directory">Directory from which to load the files</param>
        /// <param name="fileSystem">File system from which to load the files</param>
        /// <returns>A newly built executable file system</returns>
        public static async Task<ExeFs> Build(string directory, IFileSystem fileSystem, ExtractionProgressedToken? progressReportToken = null)
        {
            var files = fileSystem.GetFiles(directory, "*", true).ToList();
            if (files.Count > 10)
            {
                throw new ArgumentException(Properties.Resources.ExeFs_ExceededMaximumFileCount, nameof(directory));
            }

            if (progressReportToken != null)
            {
                progressReportToken.TotalFileCount = files.Count;
            }

            var exefs = new ExeFs();
            foreach (var file in files)
            {
                exefs.Files.Add(Path.GetFileName(file), null);
            }
            await files.RunAsyncForEach(file =>
            {
                exefs.Files[Path.GetFileName(file)] = new ExeFsEntry(fileSystem.ReadAllBytes(file));
                if (progressReportToken != null)
                {
                    progressReportToken.IncrementExtractedFileCount();
                }
            }).ConfigureAwait(false);

            return exefs;
        }

        public ExeFs()
        {
            Files = new Dictionary<string, ExeFsEntry?>(StringComparer.OrdinalIgnoreCase);
        }

        public ExeFs(IReadOnlyBinaryDataAccessor exeFsData) : this()
        {
            ExeFsData = exeFsData ?? throw new ArgumentNullException(nameof(exeFsData));
        }

        protected async Task Initialize()
        {
            var headers = new ExeFsHeader[10];
            var fileData = new byte[10][];
            var hashes = new byte[10][];
            await Task.WhenAll(Enumerable.Range(0, 10).Select(async i =>
            {
                headers[i] = new ExeFsHeader(await ExeFsData.ReadArrayAsync(i * 16, 16).ConfigureAwait(false));
                hashes[i] = await ExeFsData.ReadArrayAsync(0xC0 + ((9 - i) * 32), 32).ConfigureAwait(false); // Hashes are stored in reverse order from headers
                if (headers[i].FileSize > 0)
                {
                    fileData[i] = await ExeFsData.Slice(headers[i].Offset + 0x200, headers[i].FileSize).ReadArrayAsync().ConfigureAwait(false);
                }
            })).ConfigureAwait(false);

            for (int i = 0; i < headers.Length; i++)
            {
                var header = headers[i];
                var data = fileData[i];
                var hash = hashes[i];
                if (header.FileSize > 0)
                {
                    Files.Add(header.Filename, new ExeFsEntry(data, hash));
                }
            }
        }

        protected IReadOnlyBinaryDataAccessor ExeFsData { get; set; }

        public Dictionary<string, ExeFsEntry?> Files { get; set; }

        /// <summary>
        /// Saves all files in the executable file system to the specified file system
        /// </summary>
        /// <param name="directoryName">Directory on the specified file system to which the files should be saved.</param>
        /// <param name="fileSystem">File system to which the files should be saved.</param>
        /// <param name="progressReportToken">Optional token to be used to track the progress of the extraction.</param>
        public async Task ExtractFiles(string directoryName, IFileSystem fileSystem, ExtractionProgressedToken? progressReportToken = null)
        {
            if (progressReportToken != null)
            {
                progressReportToken.TotalFileCount = Files.Count;
            }

            if (!fileSystem.DirectoryExists(directoryName))
            {
                fileSystem.CreateDirectory(directoryName);
            }

            await Files.RunAsyncForEach((KeyValuePair<string, ExeFsEntry> file) =>
            {
                fileSystem.WriteAllBytes(Path.Combine(directoryName, file.Key), file.Value.RawData);

                if (progressReportToken != null)
                {
                    progressReportToken.IncrementExtractedFileCount();
                }
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Turns the executable file system into its binary representation
        /// </summary>
        public byte[] ToByteArray()
        {
            var header = new byte[0x200];
            var data = new List<byte>();
            var fileIndex = 0;
            foreach (var file in Files.OrderBy(kv => kv.Key, StringComparer.Ordinal))
            {
                // Header
                var nameBytes = Encoding.ASCII.GetBytes(file.Key);
                Array.Copy(nameBytes, 0, header, 0x10 * fileIndex, Math.Min(nameBytes.Length, 8));
                Array.Copy(BitConverter.GetBytes(data.Count), 0, header, 0x10 * fileIndex + 8, 4);
                Array.Copy(BitConverter.GetBytes(file.Value.RawData.Length), 0, header, 0x10 * fileIndex + 0xC, 4);

                // Hash
                // Note: Hashes are stored in reverse order from headers
                Array.Copy(file.Value.Hash, 0, header, 0xC0 + ((9 - fileIndex) * 32), Math.Min(file.Value.Hash.Length, 32));

                // Data
                data.AddRange(file.Value.RawData);
                while (data.Count % 0x200 != 0)
                {
                    data.Add(0);
                }

                fileIndex += 1;
            }

            data.InsertRange(0, header);
            return data.ToArray();
        }

        public long GetBinaryLength()
        {
            return 0x200 // Header
                + Files.Values
                .Where(f => f != null)
                .Select(f => f.RawData.Length + (0x200 - f.RawData.Length % 0x200))
                .Sum();
        }

        public bool AreAllHashesValid()
        {
            return Files.Values
                .Select(f => f.IsFileHashValid())
                .All(valid => valid);
        }

        public static byte[] GetSuperblockHash(SHA256 sha, byte[] data)
        {
            return sha.ComputeHash(data, 0, 0x200);
        }

        public byte[] GetSuperblockHash(SHA256 sha)
        {
            var data = ToByteArray();
            return GetSuperblockHash(sha, data);
        }

        public byte[] GetSuperblockHash()
        {
            using var sha = SHA256.Create();
            return GetSuperblockHash(sha);
        }

        protected class ExeFsHeader
        {
            public ExeFsHeader(byte[] data)
            {
                if (data == null)
                {
                    throw new ArgumentNullException(nameof(data));
                }

                Filename = Encoding.ASCII.GetString(data, 0, 8).TrimEnd('\0');
                Offset = BitConverter.ToInt32(data, 8);
                FileSize = BitConverter.ToInt32(data, 0xC);
            }

            public string Filename { get; private set; }
            public int Offset { get; private set; }
            public int FileSize { get; private set; }
        }

        public class ExeFsEntry
        {
            public ExeFsEntry(byte[] rawData)
            {
                RawData = rawData ?? throw new ArgumentNullException(nameof(rawData));
                Hash = ComputeHash();
            }

            public ExeFsEntry(byte[] rawData, byte[] hash)
            {
                RawData = rawData ?? throw new ArgumentNullException(nameof(rawData));
                Hash = hash ?? throw new ArgumentNullException(nameof(hash));
            }

            public byte[] RawData { get; set; }
            public byte[] Hash { get; set; }

            private byte[] ComputeHash()
            {
                using var sha = SHA256.Create();
                return sha.ComputeHash(RawData);
            }

            public bool IsFileHashValid()
            {
                return ComputeHash().SequenceEqual(Hash);
            }
        }
    }
}
