using DotNet3dsToolkit.Ctr;
using FluentAssertions;
using SkyEditor.Core.IO;
using SkyEditor.Core.Utilities;
using SkyEditor.IO.Binary;
using SkyEditor.IO.FileSystem;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace DotNet3dsToolkit.Tests
{
    [Collection("3DS ROM Tests")] // Required to run tests synchronously (can't open multiple memory mapped file instances with the current implementation)
    public class ThreeDsRomTests
    {        
        public static IEnumerable<object[]> NcsdTestData()
        {
            // Assume we're in DotNet3dsToolkit/Tests/DotNet3dsToolkit.Tests/bin/Debug/netcoreapp2.0
            // We're looking for DotNet3dsToolkit/TestData
            foreach (var filename in Directory.GetFiles("../../../../TestData", "*.3ds"))
            {
                yield return new object[] { filename };
            }
        }

        public static IEnumerable<object[]> CiaTestData()
        {
            // Assume we're in DotNet3dsToolkit/Tests/DotNet3dsToolkit.Tests/bin/Debug/netcoreapp2.0
            // We're looking for DotNet3dsToolkit/TestData
            foreach (var filename in Directory.GetFiles("../../../../TestData", "*.cia"))
            {
                yield return new object[] { filename };
            }
        }

        [Theory]
        [MemberData(nameof(NcsdTestData))]
        [MemberData(nameof(CiaTestData))]
        public async void ReadsNcchPartitions(string filename)
        {
            using (var rom = new ThreeDsRom())
            {
                await rom.OpenFile(filename);
                rom.Partitions.Should().NotBeNull();

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
        [MemberData(nameof(NcsdTestData))]
        [MemberData(nameof(CiaTestData))]
        public async Task ExtractRomFsFiles(string filename)
        {
            var progressReportToken = new ProgressReportToken();

            using (var rom = new ThreeDsRom())
            {
                await rom.OpenFile(filename);
                var extractionTask = rom.ExtractFiles("./extracted-" + Path.GetFileName(filename), progressReportToken);

                //// Awaiting the task and handling the progressReportToken makes everything wait on Debug.WriteLine, slowing things down a lot
                //// So we asynchronously poll
                //while (!extractionTask.IsCompleted && !extractionTask.IsFaulted)
                //{
                //    Debug.WriteLine("Extraction progress: " + Math.Round(progressReportToken.Progress * 100, 2).ToString());
                //    await Task.Delay(200);
                //}

                await extractionTask;
            }
            Debug.WriteLine("Extraction complete!");
        }

        [Theory]
        [MemberData(nameof(NcsdTestData))]
        [MemberData(nameof(CiaTestData))]
        public async Task VerifyFileSystemInterface(string filename)
        {
            using (var rom = new ThreeDsRom())
            {
                await rom.OpenFile(filename);

                var romAsProvider = rom as IFileSystem;
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

        [Theory]
        [MemberData(nameof(NcsdTestData))]
        [MemberData(nameof(CiaTestData))]
        public async Task CanRebuildRomfs(string filename)
        {
            var romfsDirectory = "./CanRebuildRomfs-" + Path.GetFileName(filename);
            var fs = new PhysicalFileSystem();

            if (Directory.Exists(romfsDirectory))
            {
                Directory.Delete(romfsDirectory, true);
            }

            Directory.CreateDirectory(romfsDirectory);

            using (var originalRom = new ThreeDsRom())
            {
                await originalRom.OpenFile(filename);

                var newRomFs = await RomFs.Build("/RomFS", originalRom);
                var newRomFsData = await (await newRomFs.GetRawData()).ReadArrayAsync();
                using (var newBinaryFile = new BinaryFile(newRomFsData))
                using (var newRom = new ThreeDsRom())
                {
                    await newRom.OpenFile(newBinaryFile);
                    AreDirectoriesEqual("/RomFS", originalRom, "/RomFS", newRom).Should().BeTrue();
                }
            }

            Directory.Delete(romfsDirectory, true);
        }

        private bool AreDirectoriesEqual(string directory1, IFileSystem fileSystem1, string directory2, IFileSystem filesystem2)
        {
            throw new NotImplementedException("Comparing directories is not implemented");
        }
    }
}
