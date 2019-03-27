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
            var file = new NcsdFile(data);
            await file.Initalize();
            return file;
        }

        public NcsdFile(IReadOnlyBinaryDataAccessor data)
        {
            NcsdData = data ?? throw new ArgumentNullException(nameof(data));
        }

        public async Task Initalize()
        {
            // To-do: determine which NCSD header to use
            Header = new CartridgeNcsdHeader(await NcsdData.ReadArrayAsync(0, 0x1500));

            Partitions = new NcchPartition[Header.Partitions.Length];

            await Task.WhenAll(Enumerable.Range(0, Header.Partitions.Length).Select(async i =>
            {
                var partitionStart = (long)Header.Partitions[i].Offset * MediaUnitSize;
                var partitionLength = (long)Header.Partitions[i].Length * MediaUnitSize;
                Partitions[i] = await NcchPartition.Load(NcsdData.GetReadOnlyDataReference(partitionStart, partitionLength));
            }));
        }

        private IReadOnlyBinaryDataAccessor NcsdData { get; set; }

        public NcsdHeader Header { get; set; }

        public NcchPartition[] Partitions { get; set; }

        public bool IsDlcContainer => false;

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
