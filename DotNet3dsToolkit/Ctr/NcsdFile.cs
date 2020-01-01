using SkyEditor.IO;
using SkyEditor.IO.Binary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNet3dsToolkit.Ctr
{
    public class NcsdFile : INcchPartitionContainer, IDisposable
    {
        private const int MediaUnitSize = 0x200;

        public static async Task<bool> IsNcsd(IReadOnlyBinaryDataAccessor file)
        {
            try
            {
                if (file.Length < 0x104)
                {
                    return false;
                }

                return await file.ReadStringAsync(0x100, 4, Encoding.ASCII) == "NCSD";
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static async Task<NcsdFile> Load(IReadOnlyBinaryDataAccessor data)
        {
            var header = new CartridgeNcsdHeader(await data.ReadArrayAsync(0, 0x1500));
            var partitions = new NcchPartition[header.Partitions.Length];

            await Task.WhenAll(Enumerable.Range(0, header.Partitions.Length).Select(async i =>
            {
                var partitionStart = (long)header.Partitions[i].Offset * MediaUnitSize;
                var partitionLength = (long)header.Partitions[i].Length * MediaUnitSize;
                partitions[i] = await NcchPartition.Load(data.GetReadOnlyDataReference(partitionStart, partitionLength));
            }));

            return new NcsdFile(header, partitions);
        }

        public NcsdFile(NcsdHeader header, params NcchPartition[] partitions)
        {
            Header = header ?? throw new ArgumentNullException(nameof(partitions));

            if ((partitions?.Length ?? 0) == 0)
            {
                throw new ArgumentException("Must provider at least one partition", nameof(partitions));
            }

            if (partitions.Length > 8)
            {
                throw new ArgumentException("NCSD files cannot have more than 8 partitions", nameof(partitions));
            }

            Partitions = partitions;
        }

        public NcsdHeader Header { get; set; }

        public NcchPartition[] Partitions { get; set; }

        public bool IsDlcContainer => false;

        public void RecalculateCartridgeSize()
        {
            var romFsFileSizes = Partitions.Where(p => p.RomFs != null).Select(p => p.RomFs.GetTotalFileSize()).Sum();
            var exeFsFileSizes = Partitions.Where(P => P.ExeFs != null).Select(p => p.ExeFs.Files.Values.Select(f => f.RawData.Length).Sum()).Sum();
            var totalSize = romFsFileSizes + exeFsFileSizes;
            long cartridgeSize = (long)Math.Pow(2, Math.Ceiling(Math.Log(totalSize * 0x200) / Math.Log(2)));
            var cartridgeSizeInMediaUnits = (cartridgeSize + 0x200 - 1) / 0x200;
            Header.ImageSize = (int)cartridgeSizeInMediaUnits;
        }

        /// <summary>
        /// Writes the current state of the NCSD partition to the given binary data accessor
        /// </summary>
        /// <param name="data">Data accessor to receive the binary data</param>
        public async Task WriteBinary(IBinaryDataAccessor data)
        {
            long offset = 0x4000;
            var partitionHeaders = new List<NcsdPartitionInfo>();
            for (int i = 0; i < Partitions.Length; i++)
            {
                if (Partitions[i] != null)
                {
                    var bytesWritten = await Partitions[i].WriteBinary(data.GetDataReference(offset, data.Length));
                    partitionHeaders.Add(new NcsdPartitionInfo
                    {
                        CryptType = 0,
                        Length = (int)((bytesWritten + 0x200 - 1) / 0x200),
                        Offset = (int)((offset + 0x200 - 1) / 0x200)
                    });
                    offset += bytesWritten + (0x200 - (bytesWritten % 0x200));
                }
                else
                {
                    partitionHeaders.Add(new NcsdPartitionInfo
                    {
                        CryptType = 0,
                        Length = 0,
                        Offset = 0
                    });
                }
            }

            Header.Partitions = partitionHeaders.ToArray();

            var headerData = Header.ToByteArray();
            await data.WriteAsync(0, headerData);
            await data.WriteAsync(headerData.Length, Enumerable.Repeat<byte>(0xFF, 0x4000 - headerData.Length).ToArray());
        }

        public void Dispose()
        {
            if (Partitions != null)
            {
                foreach (var partition in Partitions)
                {
                    partition?.Dispose();
                }
            }
        }
    }
}
