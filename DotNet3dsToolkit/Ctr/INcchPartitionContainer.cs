using System;
using System.Collections.Generic;
using System.Text;

namespace DotNet3dsToolkit.Ctr
{
    public interface INcchPartitionContainer
    {
        /// <summary>
        /// The NCCH partitions contained within the file
        /// </summary>
        NcchPartition[] Partitions { get; }

        /// <summary>
        /// Whether or not the container is for DLC. If true, special partitions such as updates or download play are not applicable.
        /// </summary>
        bool IsDlcContainer { get; }
    }
}
