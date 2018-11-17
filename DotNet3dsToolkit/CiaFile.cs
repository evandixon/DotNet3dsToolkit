using SkyEditor.Core.IO;
using SkyEditor.Core.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNet3dsToolkit
{
    public class CiaFile : INcchPartitionContainer
    {
        public static Task<bool> IsCia(string filename, GenericFile file)
        {
            // To-do: look at the actual data
            return Task.FromResult(filename.ToLower().EndsWith(".cia"));
        }

        public static async Task<CiaFile> Load(IBinaryDataAccessor data)
        {
            var file = new CiaFile(data);
            await file.Initalize();
            return file;
        }

        public CiaFile(IBinaryDataAccessor data)
        {
            CiaData = data ?? throw new ArgumentNullException(nameof(data));
        }

        public async Task Initalize()
        {
            var headerSize = await CiaData.ReadInt32Async(0);
            CiaHeader = new CiaHeader(await CiaData.ReadAsync(0, headerSize));

            var certOffset = Util.Align(headerSize, 64);
            var ticketOffset = Util.Align(certOffset + CiaHeader.CertificateChainSize, 64);
            var tmdOffset = Util.Align(ticketOffset + CiaHeader.TicketSize, 64);
            var contentOffset = Util.Align(tmdOffset + CiaHeader.TmdFileSize, 64);
            var metaOffset = Util.Align64(contentOffset + CiaHeader.ContentSize, 64);

            TmdMetadata = await TmdMetadata.Load(CiaData.GetDataReference(tmdOffset, CiaHeader.TmdFileSize));

            ContentPartitions = new Dictionary<int, List<NcchPartition>>();
            foreach (var chunkRecord in TmdMetadata.ContentChunkRecords)
            {
                var partitionStart = contentOffset + TmdMetadata.ContentInfoRecords[chunkRecord.ContentId].ContentIndexOffset;
                var partitionLength = chunkRecord.ContentSize;

                if (!ContentPartitions.ContainsKey(chunkRecord.ContentIndex))
                {
                    ContentPartitions.Add(chunkRecord.ContentIndex, new List<NcchPartition>());
                }
                ContentPartitions[chunkRecord.ContentIndex].Add(await NcchPartition.Load(CiaData.GetDataReference(partitionStart, (int)partitionLength)));
            }

            Partitions = new NcchPartition[ContentPartitions.Keys.Max() + 1];
            foreach (var chunkKey in ContentPartitions.Keys)
            {
                Partitions[chunkKey] = ContentPartitions[chunkKey].First();
            }
        }

        private IBinaryDataAccessor CiaData { get; set; }
        
        public CiaHeader CiaHeader { get; private set; }

        public TmdMetadata TmdMetadata { get; private set; }

        /// <summary>
        /// All partitions grouped by content index
        /// </summary>
        public Dictionary<int, List<NcchPartition>> ContentPartitions { get; set; }

        /// <summary>
        /// The first partition of each content type
        /// </summary>
        public NcchPartition[] Partitions { get; private set; }
    }
}
