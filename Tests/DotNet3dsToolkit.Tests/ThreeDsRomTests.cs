using DotNet3dsToolkit.Ctr;
using FluentAssertions;
using SkyEditor.IO;
using SkyEditor.IO.Binary;
using SkyEditor.IO.FileSystem;
using SkyEditor.Utilities.AsyncFor;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

                using (var newRom = new ThreeDsRom(await RomFs.Build("/RomFS", originalRom)))
                {
                    await AssertDirectoriesEqual("/RomFS", originalRom, "/RomFS", newRom);
                }
            }
        }

        private async Task AssertDirectoriesEqual(string directory1, ThreeDsRom fileSystem1, string directory2, ThreeDsRom filesystem2)
        {
            // Assume directory1 is good. It's sourced by a regular, non-rebuilt ROM, which should be covered by its own tests.
            await (fileSystem1 as IFileSystem).GetFiles(directory1, "*", false).RunAsyncForEach(async file =>
            {
                var otherFile = Path.Combine(directory2, file.Replace(directory1, "").TrimStart('/'));

                var data1 = await fileSystem1.GetFileReference(file).ReadArrayAsync();
                var data2 = await filesystem2.GetFileReference(otherFile).ReadArrayAsync();

                data1.Length.Should().Be(data2.Length, $"because file '{file}' should have the same size as file '{otherFile}' in both directories");

                UnsafeCompare(data1, data2).Should().BeTrue();
            });
        }

        // Source: https://stackoverflow.com/a/8808245
        static unsafe bool UnsafeCompare(byte[] a1, byte[] a2)
        {
            if (a1 == a2)
            {
                return true;
            }

            if (a1 == null || a2 == null || a1.Length != a2.Length)
            {
                return false;
            }

            fixed (byte* p1 = a1, p2 = a2)
            {
                byte* x1 = p1, x2 = p2;
                int l = a1.Length;
                for (int i = 0; i < l / 8; i++, x1 += 8, x2 += 8)
                {
                    if (*((long*)x1) != *((long*)x2))
                    {
                        return false;
                    }
                }
                if ((l & 4) != 0)
                {
                    if (*((int*)x1) != *((int*)x2))
                    {
                        return false;
                    }
                    x1 += 4;
                    x2 += 4;
                }
                if ((l & 2) != 0)
                {
                    if (*((short*)x1) != *((short*)x2))
                    {
                        return false;
                    }
                    x1 += 2;
                    x2 += 2;
                }
                if ((l & 1) != 0)
                {
                    if (*((byte*)x1) != *((byte*)x2)) return false;
                }
                return true;
            }
        }
    }
}
