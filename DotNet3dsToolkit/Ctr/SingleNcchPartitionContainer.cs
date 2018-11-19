using System;
using System.Collections.Generic;
using System.Text;

namespace DotNet3dsToolkit.Ctr
{
    public class SingleNcchPartitionContainer : INcchPartitionContainer
    {
        public SingleNcchPartitionContainer(NcchPartition partition, int partitionIndex = 0)
        {
            if (partitionIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(partitionIndex), "Partition index must be 0 or greater");
            }

            Partitions = new NcchPartition[partitionIndex + 1];
            Partitions[partitionIndex] = partition ?? throw new ArgumentNullException(nameof(partition));
        }

        public NcchPartition[] Partitions { get; }
    }
}
