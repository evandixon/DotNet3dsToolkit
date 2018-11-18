using System;
using System.Collections.Generic;
using System.Text;

namespace DotNet3dsToolkit.Ctr
{
    public interface INcchPartitionContainer
    {
        NcchPartition[] Partitions { get; }
    }
}
