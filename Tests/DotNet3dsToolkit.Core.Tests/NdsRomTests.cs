using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Threading.Tasks;
using SkyEditor.Core.IO;

namespace DotNet3dsToolkit.Core.Tests
{
    [TestClass]
    public class NdsRomTests
    {
        public const string TestCategory = "NDS ROM";
        public const string EosUsPath = @"Resources/eosu.nds";
        public const string EosUsUnpackDir = @"RawFiles-EOSUS";
        public const string BrtUsPath = @"Resources/brtu.nds";
        public const string BrtUsUnpackDir = @"RawFiles-BRTUS";

        private IIOProvider Provider { get; set; }

        [TestInitialize]
        public void TestInit()
        {
            if (!File.Exists(EosUsPath))
            {
                Assert.Fail("Missing test ROM: Pokémon Mystery Dungeon: Explorers of Sky (US).  Place it at the following path: " + EosUsPath);
            }
            if (!File.Exists(BrtUsPath))
            {
                Assert.Fail("Missing test ROM: Pokémon Mystery Dungeon: Blue Rescue Team (US).  Place it at the following path: " + BrtUsPath);
            }
            Provider = new PhysicalIOProvider();
        }

        [TestMethod]
        [TestCategory(TestCategory)]
        public async Task TestOpen_EosUs()
        {
            // This test will be replaced with something else later.
            // Right now, it just ensures there's no exceptions
            using (var eosUS = new NdsRom())
            {
                await eosUS.OpenFile(EosUsPath, Provider);
                await eosUS.Unpack(EosUsUnpackDir, Provider);
            }

            // Cleanup
            Provider.DeleteDirectory(EosUsUnpackDir);
        }

        [TestMethod]
        [TestCategory(TestCategory)]
        public async Task TestOpen_BrtUs()
        {
            // This test will be replaced with something else later.
            // Right now, it just ensures there's no exceptions
            using (var eosUS = new NdsRom())
            {
                await eosUS.OpenFile(BrtUsPath, Provider);
                await eosUS.Unpack(BrtUsUnpackDir, Provider);
            }

            // Cleanup
            Provider.DeleteDirectory(BrtUsUnpackDir);
        }


    }
}
