using FluentAssertions;
using SkyEditor.Core.IO;
using SkyEditor.Core.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
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
        public async void ExtractPartitions(string filename)
        {
            using (var rom = new ThreeDsRom())
            {
                await rom.OpenFile(filename, new PhysicalIOProvider());

                var a = new AsyncFor();
                a.RunSynchronously = false;
                await a.RunFor(async (i) =>
                {
                    var partition = rom.Header.Partitions[i];
                    if (partition.Length > 0)
                    {
                        File.WriteAllBytes("partition" + i.ToString() + ".bin", await rom.Partitions[i].Data.ReadAsync());
                    }
                }, 0, rom.Header.Partitions.Length - 1);
            }
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public async void ExtractRomFsBin(string filename)
        {
            using (var rom = new ThreeDsRom())
            {
                await rom.OpenFile(filename, new PhysicalIOProvider());

                var a = new AsyncFor();
                a.RunSynchronously = false;
                await a.RunFor(async (i) =>
                {
                    var partition = rom.Partitions[i];
                    if (partition.RomFs != null)
                    {
                        File.WriteAllBytes("romfs" + i.ToString() + ".bin", await partition.RomFs.Data.ReadAsync());
                    }
                }, 0, rom.Header.Partitions.Length - 1);
            }
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public async void ExtractRomFsFiles(string filename)
        {
            using (var rom = new ThreeDsRom())
            {
                var provider = new PhysicalIOProvider();
                await rom.OpenFile(filename, provider);
                await rom.ExtractFiles("./extracted-" + Path.GetFileNameWithoutExtension(filename), provider);                
            }
        }
    }
}
