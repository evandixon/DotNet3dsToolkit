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

                var exefsHeaders = (await ExeFs.Load(file).ConfigureAwait(false)).Headers.Where(h => !string.IsNullOrEmpty(h.Filename));
                if (!exefsHeaders.Any())
                {
                    return false;
                }

                return exefsHeaders.All(h => h.Offset >= 0x200 && (h.Offset + h.FileSize) < file.Length);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static async Task<ExeFs> Load(IReadOnlyBinaryDataAccessor exeFsData)
        {
            var exefs = new ExeFs(exeFsData);
            await exefs.Initialize().ConfigureAwait(false);
            return exefs;
        }

        public ExeFs(IReadOnlyBinaryDataAccessor exeFsData)
        {
            ExeFsData = exeFsData ?? throw new ArgumentNullException(nameof(exeFsData));
        }

        public async Task Initialize()
        {
            var headers = new ExeFsHeader[10];
            var hashes = new byte[10][];
            await Task.WhenAll(Enumerable.Range(0, 10).Select(async i =>
            {
                headers[i] = new ExeFsHeader(await ExeFsData.ReadArrayAsync(i * 16, 16).ConfigureAwait(false));
                hashes[i] = await ExeFsData.ReadArrayAsync(0xC0 + ((9 - i) * 32), 32).ConfigureAwait(false); // Hashes are stored in reverse order from headers
            })).ConfigureAwait(false);

            this.Headers = new List<ExeFsHeader>(10);
            this.Hashes = new List<byte[]>(10);
            for (int i = 0; i < headers.Length; i++)
            {
                var header = headers[i];
                var hash = hashes[i];
                if (header.FileSize > 0)
                {
                    this.Headers.Add(header);
                    this.Hashes.Add(hash);
                }
            }
        }

        private IReadOnlyBinaryDataAccessor ExeFsData { get; set; }

        /// <summary>
        /// Headers of the file data
        /// </summary>
        public List<ExeFsHeader> Headers { get; private set; }

        /// <summary>
        /// SHA256 hashes of the file data
        /// </summary>
        public List<byte[]> Hashes { get; set; }

        public async Task ExtractFiles(string directoryName, IFileSystem fileSystem, ExtractionProgressedToken progressReportToken = null)
        {
            if (progressReportToken != null)
            {
                progressReportToken.TotalFileCount = Headers.Count(h => h != null && !string.IsNullOrEmpty(h.Filename));
            }

            if (!fileSystem.DirectoryExists(directoryName))
            {
                fileSystem.CreateDirectory(directoryName);
            }

            await Headers.RunAsyncForEach(async (header) =>
            {
                if (string.IsNullOrEmpty(header.Filename))
                {
                    return;
                }

                fileSystem.WriteAllBytes(Path.Combine(directoryName, header.Filename), await ExeFsData.ReadArrayAsync(0x200 + header.Offset, header.FileSize).ConfigureAwait(false));

                if (progressReportToken != null)
                {
                    progressReportToken.IncrementExtractedFileCount();
                }
            }).ConfigureAwait(false);
        }

        public IReadOnlyBinaryDataAccessor GetDataReference(ExeFsHeader header)
        {
            return ExeFsData.GetReadOnlyDataReference(header.Offset + 0x200, header.FileSize);
        }

        public IReadOnlyBinaryDataAccessor GetDataReference(string filename)
        {
            var file = Headers?.FirstOrDefault(h => string.Compare(h.Filename, filename, false) == 0);

            if (file == null)
            {
                return null;
            }

            return GetDataReference(file);
        }

        public async Task<byte[]> CalculateFileHash(ExeFsHeader header)
        {
            var data = await GetDataReference(header).ReadArrayAsync().ConfigureAwait(false);
            using (var sha = SHA256.Create())
            {
                return sha.ComputeHash(data);
            }
        }

        public async Task<byte[]> CalculateFileHash(int fileIndex)
        {
            return await CalculateFileHash(Headers[fileIndex]);
        }

        public async Task<byte[]> CalculateFileHash(string filename)
        {
            var file = Headers?.FirstOrDefault(h => string.Compare(h.Filename, filename, false) == 0);

            if (file == null)
            {
                return null;
            }

            return await CalculateFileHash(file);
        }

        public async Task<bool> IsFileHashValid(int fileIndex)
        {
            var hash = Hashes[fileIndex];
            var calculatedHash = await CalculateFileHash(fileIndex).ConfigureAwait(false);
            return hash.SequenceEqual(calculatedHash);
        }

        public async Task<bool> IsFileHashValid(ExeFsHeader header)
        {
            return await IsFileHashValid(Headers.IndexOf(header));
        }


        public async Task<bool> IsFileHashValid(string filename)
        {
            var file = Headers?.FirstOrDefault(h => string.Compare(h.Filename, filename, false) == 0);

            if (file == null)
            {
                return false;
            }

            return await IsFileHashValid(file);
        }

        public async Task<bool> AreAllHashesValid()
        {
            return (await Task.WhenAll(Headers
                .Select(h => IsFileHashValid(h))))
                .All(valid => valid);
        }

        public class ExeFsHeader
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
    }
}
