using SkyEditor.Core.IO;
using SkyEditor.Core.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNet3dsToolkit
{
    public class ExeFs
    {
        public static async Task<ExeFs> Load(IBinaryDataAccessor exeFsData)
        {
            var exefs = new ExeFs(exeFsData);
            await exefs.Initialize();
            return exefs;
        }

        public ExeFs(IBinaryDataAccessor exeFsData)
        {
            ExeFsData = exeFsData ?? throw new ArgumentNullException(nameof(exeFsData));
        }

        public async Task Initialize()
        {
            Headers = new ExeFsHeader[10];
            Hashes = new byte[10][];
            var a = new AsyncFor();
            await a.RunFor(async (i) =>
            {
                Headers[i] = new ExeFsHeader(await ExeFsData.ReadAsync(i * 16, 16));
                Hashes[i] = await ExeFsData.ReadAsync(0xC0 + ((9-i) * 32), 32); // Hashes are stored in reverse order from headers
            }, 0, 9);
        }

        private IBinaryDataAccessor ExeFsData { get; set; }

        /// <summary>
        /// Headers of the file data
        /// </summary>
        public ExeFsHeader[] Headers { get; private set; }

        /// <summary>
        /// SHA256 hashes of the file data
        /// </summary>
        public byte[][] Hashes { get; set; }

        public async Task ExtractFiles(string directoryName, IIOProvider provider, ExtractionProgressedToken progressReportToken = null)
        {
            if (progressReportToken != null)
            {
                progressReportToken.TotalFileCount = Headers.Count(h => h != null && !string.IsNullOrEmpty(h.Filename));
            }

            if (!provider.DirectoryExists(directoryName))
            {
                provider.CreateDirectory(directoryName);
            }

            var a = new AsyncFor();
            await a.RunForEach(Headers, async (header) =>
            {
                if (string.IsNullOrEmpty(header.Filename))
                {
                    return;
                }

                provider.WriteAllBytes(Path.Combine(directoryName, header.Filename), await ExeFsData.ReadAsync(0x200 + header.Offset, header.FileSize));

                if (progressReportToken != null)
                {
                    progressReportToken.IncrementExtractedFileCount();
                }
            });            
        }

        public IBinaryDataAccessor GetDataReference(string filename)
        {
            var file = Headers?.FirstOrDefault(h => string.Compare(h.Filename, filename, false) == 0);

            if (file == null)
            {
                return null;
            }

            return ExeFsData.GetDataReference(file.Offset, file.FileSize);
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
