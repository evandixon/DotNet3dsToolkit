using System;
using System.Collections.Generic;
using System.Text;

namespace DotNet3dsToolkit
{
    public interface INcchPartitionContainer
    {
        NcchPartition[] Partitions { get; }
    }
}
