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
            string dataOverride = null;

            for (int i = 2;i<args.Length;i+=1)
            {
                switch (args[i])
                {
                    case "--datapath":
                        if (i<args.Length-1)
                        {
                            dataOverride = args[i + 1];
                        }
                        break;
                }
            }

            using (var file = new NdsRom())
            {
                if (!string.IsNullOrEmpty(dataOverride))
                {
                    file.DataPath = dataOverride;
                }

                file.OpenFile(filename, new PhysicalIOProvider()).Wait();
                file.Unpack(dir, new PhysicalIOProvider()).Wait();
            }
        }
    }
}