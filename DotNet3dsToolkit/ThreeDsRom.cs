using SkyEditor.Core.IO;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DotNet3dsToolkit
{
    public class ThreeDsRom : IOpenableFile
    {

        public NcsdHeader Header { get; set; }

        public NcchPartition[] Partitions { get; set; }

        private GenericFile RawData { get; set; }

        public async Task OpenFile(string filename, IIOProvider provider)
        {
            RawData = new GenericFile(filename, provider);

            // To-do: determine which NCSD header to use
            Header = new CartridgeNcsdHeader(await RawData.ReadAsync(0, 0x1500));

            var partitions = new List<NcchPartition>();
            for (int i = 0; i < Header.Partitions.Length; i++)
            {
                NcchHeader header = null;
                if (Header.Partitions[i].Length > 0)
                {
                    header = new NcchHeader(await ReadPartitionAsync(i, 0, 0x200));
                }
                partitions.Add(new NcchPartition(this, i, header));
            }
            Partitions = partitions.ToArray();
        }

        public async Task<byte[]> ReadPartitionAsync(int partitionIndex)
        {
            var partitionStart = (long)Header.Partitions[partitionIndex].Offset * 0x200;
            var partitionLength = (long)Header.Partitions[partitionIndex].Length * 0x200;

            if (partitionLength == 0)
            {
                throw new IndexOutOfRangeException(Properties.Resources.ThreeDsRom_PartitionDoesNotExist);
            }

            return await RawData.ReadAsync(partitionStart, (int)partitionLength);
        }

        public async Task<byte> ReadPartitionAsync(int partitionIndex, long index)
        {
            var partitionStart = (long)Header.Partitions[partitionIndex].Offset * 0x200;
            var partitionLength = (long)Header.Partitions[partitionIndex].Length * 0x200;

            if (partitionLength == 0)
            {
                throw new IndexOutOfRangeException(Properties.Resources.ThreeDsRom_PartitionDoesNotExist);
            }

            if (partitionLength < index)
            {
                throw new IndexOutOfRangeException(Properties.Resources.ThreeDsRom_PartitionDataOutOfRange);
            }

            return await RawData.ReadAsync(partitionStart + partitionLength);
        }

        public async Task<byte[]> ReadPartitionAsync(int partitionIndex, long index, int length)
        {
            var partitionStart = (long)Header.Partitions[partitionIndex].Offset * 0x200;
            var partitionLength = (long)Header.Partitions[partitionIndex].Length * 0x200;

            if (partitionLength == 0)
            {
                throw new IndexOutOfRangeException(Properties.Resources.ThreeDsRom_PartitionDoesNotExist);
            }

            if (partitionLength < index || partitionLength < length)
            {
                throw new IndexOutOfRangeException(Properties.Resources.ThreeDsRom_PartitionDataOutOfRange);
            }

            return await RawData.ReadAsync(partitionStart + index, (int)length);
        }
    }
}
