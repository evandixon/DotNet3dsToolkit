using SkyEditor.Core.IO;
using SkyEditor.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DotNet3dsToolkit.Ctr
{
    public class NcsdFile : INcchPartitionContainer
    {
        private const int MediaUnitSize = 0x200;

        public static async Task<bool> IsNcsd(IBinaryDataAccessor file)
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

        public static async Task<NcsdFile> Load(IBinaryDataAccessor data)
        {
            var file = new NcsdFile(data);
            await file.Initalize();
            return file;
        }

        public NcsdFile(IBinaryDataAccessor data)
        {
            NcsdData = data ?? throw new ArgumentNullException(nameof(data));
        }

        public async Task Initalize()
        {
            // To-do: determine which NCSD header to use
            Header = new CartridgeNcsdHeader(await NcsdData.ReadAsync(0, 0x1500));

            Partitions = new NcchPartition[Header.Partitions.Length];

            var a = new AsyncFor();
            await a.RunFor(async i =>
            {
                var partitionStart = (long)Header.Partitions[i].Offset * MediaUnitSize;
                var partitionLength = (long)Header.Partitions[i].Length * MediaUnitSize;
                Partitions[i] = await NcchPartition.Load(NcsdData.GetDataReference(partitionStart, (int)partitionLength));
            }, 0, Header.Partitions.Length - 1);
        }

        private IBinaryDataAccessor NcsdData { get; set; }

        public NcsdHeader Header { get; set; }

        public NcchPartition[] Partitions { get; set; }
    }
}
