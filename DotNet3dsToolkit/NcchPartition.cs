using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DotNet3dsToolkit
{
    public class NcchPartition
    {
        public NcchPartition(ThreeDsRom parentRom, int partitionIndex, NcchHeader header)
        {
            ParentRom = parentRom ?? throw new ArgumentNullException(nameof(parentRom));
            PartitionIndex = partitionIndex;
            Header = header; // Could be null if this is an empty partition
        }

        private ThreeDsRom ParentRom { get; }

        private int PartitionIndex { get; }

        public NcchHeader Header { get; } // Could be null if this is an empty partition

        public async Task<byte[]> ReadAsync()
        {
            return await ParentRom.ReadPartitionAsync(PartitionIndex);
        }

        public async Task<byte> ReadAsync(long index)
        {
            return await ParentRom.ReadPartitionAsync(PartitionIndex, index);
        }

        public async Task<byte[]> ReadAsync(long index, int length)
        {
            return await ParentRom.ReadPartitionAsync(PartitionIndex, index, length);
        }
    }
}
