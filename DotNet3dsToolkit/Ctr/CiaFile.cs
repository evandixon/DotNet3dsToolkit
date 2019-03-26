using SkyEditor.IO;
using SkyEditor.IO.Binary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DotNet3dsToolkit.Ctr
{
    public class CiaFile : INcchPartitionContainer
    {
        public static Task<bool> IsCia(BinaryFile file)
        {
            // To-do: look at the actual data
            return Task.FromResult(file.Filename.ToLower().EndsWith(".cia"));
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
            CiaHeader = new CiaHeader(await CiaData.ReadArrayAsync(0, headerSize));

            var certOffset = BitMath.Align(headerSize, 64);
            var ticketOffset = BitMath.Align(certOffset + CiaHeader.CertificateChainSize, 64);
            var tmdOffset = BitMath.Align(ticketOffset + CiaHeader.TicketSize, 64);
            var contentOffset = BitMath.Align(tmdOffset + CiaHeader.TmdFileSize, 64);
            var metaOffset = BitMath.Align(contentOffset + CiaHeader.ContentSize, 64);

            TmdMetadata = await TmdMetadata.Load(CiaData.GetDataReference(tmdOffset, CiaHeader.TmdFileSize));

            Partitions = new NcchPartition[TmdMetadata.ContentChunkRecords.Length];
            long partitionStart = contentOffset;
            for (var i = 0; i < TmdMetadata.ContentChunkRecords.Length; i++)
            {
                var chunkRecord = TmdMetadata.ContentChunkRecords[i];
                var partitionLength = chunkRecord.ContentSize;
                int contentIndex = chunkRecord.ContentIndex;

                Partitions[i] = await NcchPartition.Load(CiaData.GetDataReference(partitionStart, partitionLength));

                partitionStart += partitionLength;
            }

            IsDlcContainer = TmdMetadata.TitleId >> 32 == 0x0004008C;
        }

        private IBinaryDataAccessor CiaData { get; set; }

        public CiaHeader CiaHeader { get; private set; }

        public TmdMetadata TmdMetadata { get; private set; }

        public NcchPartition[] Partitions { get; private set; }

        public bool IsDlcContainer { get; private set; }
    }
}
