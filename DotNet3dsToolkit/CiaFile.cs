using SkyEditor.Core.IO;
using System;
using System.Collections.Generic;
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

            throw new NotImplementedException("Loading CIA files is currently not supported.");
        }

        private IBinaryDataAccessor CiaData { get; set; }
        
        public CiaHeader CiaHeader { get; set; }

        public NcchPartition[] Partitions => throw new NotImplementedException();
    }
}
