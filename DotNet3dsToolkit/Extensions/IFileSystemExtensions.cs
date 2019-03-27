using SkyEditor.IO.FileSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNet3dsToolkit.Extensions
{
    public static class IFileSystemExtensions
    {
        public static ulong GetDirectoryLength(this IFileSystem fileSystem, string directory)
        {
            ulong size = 0;
            foreach (var file in fileSystem.GetFiles(directory, "*", false))
            {
                size += (ulong)fileSystem.GetFileLength(file);
            }
            return size;
        }
    }
}
