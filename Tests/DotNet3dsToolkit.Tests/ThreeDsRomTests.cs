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
        public async void ReadsHeader(string filename)
        {
            var rom = new ThreeDsRom();
            await rom.OpenFile(filename, new PhysicalIOProvider());
            rom.Header.Should().NotBeNull();
        }
    }
}
