using FluentAssertions;
using SkyEditor.Core.IO;
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
            foreach (var filename in Directory.GetFiles("../../../../TestData"))
            {
                yield return new object[] { filename };
            }
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public async void ReadsHeaders(string filename)
        {
            var rom = new ThreeDsRom();
            await rom.OpenFile(filename, new PhysicalIOProvider());
            rom.Header.Should().NotBeNull();
            rom.Header.Magic.Should().Be("NCSD");
            rom.Header.Partitions.Should().NotBeNull();

            foreach(var partition in rom.Partitions)
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

        //[Theory]
        //[MemberData(nameof(TestData))]
        //public async void ExtractPartitions(string filename)
        //{
        //    var rom = new ThreeDsRom();
        //    await rom.OpenFile(filename, new PhysicalIOProvider());

        //    for (int i = 0; i < rom.Header.Partitions.Length; i++) {
        //        var partition = rom.Header.Partitions[i];
        //        if (partition.Length > 0)
        //        {
        //            File.WriteAllBytes("partition" + i.ToString() + ".bin", await rom.ReadPartitionAsync(i));
        //        }
        //    }
        //}
    }
}
