using SkyEditor.Core.IO;
using System;
using System.Threading.Tasks;

namespace DotNet3dsToolkit
{
    public class ThreeDsRom : IOpenableFile
    {

        private GenericFile RawData { get; set; }

        public Task OpenFile(string filename, IIOProvider provider)
        {
            RawData = new GenericFile(filename, provider);
            return Task.CompletedTask;
        }
    }
}
