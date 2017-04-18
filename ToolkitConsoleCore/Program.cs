using DotNet3dsToolkit.Core;
using SkyEditor.Core.IO;
using System;

namespace ToolkitConsoleCore
{
    class Program
    {
        static void Main(string[] args)
        {
            var filename = args[0];
            var dir = args[1];
            using (var file = new NdsRom())
            {
                file.OpenFile(filename, new PhysicalIOProvider()).Wait();
                file.Unpack(dir, new PhysicalIOProvider()).Wait();
            }
        }
    }
}