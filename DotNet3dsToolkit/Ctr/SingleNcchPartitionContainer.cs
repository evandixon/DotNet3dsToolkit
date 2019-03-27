using System;
using System.Collections.Generic;
using System.Text;

namespace DotNet3dsToolkit.Ctr
{
    public class SingleNcchPartitionContainer : INcchPartitionContainer, IDisposable
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
