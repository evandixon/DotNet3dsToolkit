using SkyEditor.Core.IO;
using System;
using System.Threading.Tasks;

namespace DotNet3dsToolkit
{
    public class ThreeDsRom : IOpenableFile
    {

        public NcsdHeader Header { get; set; }

        private GenericFile RawData { get; set; }

        public async Task OpenFile(string filename, IIOProvider provider)
        {
            RawData = new GenericFile(filename, provider);

            // To-do: determine which NCSD header to use
            Header = new CartridgeNcsdHeader(await RawData.ReadAsync(0, 0x1500));
        }
    }
}
