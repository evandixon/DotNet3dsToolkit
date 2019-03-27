using DotNet3dsToolkit.Ctr;
using FluentAssertions;
using SkyEditor.Core.IO;
using SkyEditor.Core.Utilities;
using SkyEditor.IO;
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
            var fs = new PhysicalFileSystem();

            using (var originalRom = new ThreeDsRom())
            {
                await originalRom.OpenFile(filename);

                var newRomFs = await RomFs.Build("/RomFS", originalRom);
                using (var newRom = new ThreeDsRom())
                {
                    await newRom.OpenFile(newRomFs.Data);
                    await AssertDirectoriesEqual("/RomFS", originalRom, "/RomFS", newRom);
                }
            }
        }

        private async Task AssertDirectoriesEqual(string directory1, ThreeDsRom fileSystem1, string directory2, ThreeDsRom filesystem2)
        {
            // Assume directory1 is good. It's sourced by a regular, non-rebuilt ROM, which should be covered by its own tests.
            await (fileSystem1 as IFileSystem).GetFiles(directory1, "*", false).RunAsyncForEach(file =>
            {
                var otherFile = Path.Combine(directory2, file.Replace(directory1, "").TrimStart('/'));

                var data1 = fileSystem1.GetFileReference(file);
                var data2 = filesystem2.GetFileReference(otherFile);

                data1.Length.Should().Be(data2.Length, $"because file '{file}' should have the same size as file '{otherFile}' in both directories");

                for (int i = 0; i < data1.Length - 4; i += 4)
                {
                    data1.ReadInt32(i).Should().Be(data2.ReadInt32(i), $"because file '{file}' should have the same data as '{otherFile}' in both directories, at index {i}");
                }

                Debug.WriteLine("Compared " + file);
            });
        }
    }
}
