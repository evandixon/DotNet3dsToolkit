using FluentAssertions;
using SkyEditor.Core.IO;
using SkyEditor.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace DotNet3dsToolkit.Tests
{
    public class ThreeDsRomTests
    {
        public static IEnumerable<object[]> TestData()
        {
            // Assume we're in DotNet3dsToolkit/Tests/DotNet3dsToolkit.Tests/bin/Debug/netcoreapp2.0
            // We're looking for DotNet3dsToolkit/TestData
            foreach (var filename in Directory.GetFiles("../../../../TestData", "*.3ds"))
            {
                yield return new object[] { filename };
            }
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public async void ReadsHeaders(string filename)
        {
            using (var rom = new ThreeDsRom())
            {
                await rom.OpenFile(filename, new PhysicalIOProvider());
                rom.Header.Should().NotBeNull();
                rom.Header.Magic.Should().Be("NCSD");
                rom.Header.Partitions.Should().NotBeNull();

                foreach (var partition in rom.Partitions)
                {
                    if (partition.Header != null)
                    {
                        partition.Header.Magic.Should().Be("NCCH");
                        if (partition.RomFs != null)
                        {
                            partition.RomFs.Header.Should().NotBeNull();
                            partition.RomFs.Header.Magic.Should().Be("IVFC");
                            partition.RomFs.Header.MagicNumber.Should().Be(0x10000);
                        }
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public async Task ExtractRomFsFiles(string filename)
        {
            var progressReportToken = new ProgressReportToken();

            using (var rom = new ThreeDsRom())
            {
                var provider = new PhysicalIOProvider();
                await rom.OpenFile(filename, provider);
                var extractionTask = rom.ExtractFiles("./extracted-" + Path.GetFileNameWithoutExtension(filename), provider, progressReportToken);

                // Awaiting the task and handling the progressReportToken makes everything wait on Debug.WriteLine, slowing things down a lot
                // So we asynchronously poll
                while (!extractionTask.IsCompleted)
                {
                    Debug.WriteLine("Extraction progress: " + Math.Round(progressReportToken.Progress * 100, 2).ToString());
                    await Task.Delay(200);
                }
            }
            Debug.WriteLine("Extraction complete!");
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public async Task VerifyFileSystemInterface(string filename)
        {
            using (var rom = new ThreeDsRom())
            {
                var provider = new PhysicalIOProvider();
                await rom.OpenFile(filename, provider);

                var romAsProvider = rom as IIOProvider;
                var files = romAsProvider.GetFiles("/", "*", false);
                foreach (var file in files)
                {
                    romAsProvider.FileExists(file).Should().BeTrue("File '" + file + "' should exist");
                }

                var directories = romAsProvider.GetDirectories("/", false);
                foreach (var dir in directories)
                {
                    romAsProvider.DirectoryExists(dir).Should().BeTrue("File '" + dir + "' should exist");
                }
            }
        }
    }
}
