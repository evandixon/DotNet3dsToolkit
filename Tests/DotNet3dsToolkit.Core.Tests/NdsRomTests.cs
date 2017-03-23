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
        public const string EosUsPath = @"Resources\eosu.nds";

        private IIOProvider Provider { get; set; }

        [TestInitialize]
        public void TestInit()
        {
            if (!File.Exists(EosUsPath))
            {
                Assert.Inconclusive("Missing test ROM: Pokémon Mystery Dungeon: Explorers of Sky (US).  Place it at the following path: " + EosUsPath);
            }
            Provider = new PhysicalIOProvider();
        }

        [TestMethod]
        [TestCategory(TestCategory)]
        public async Task TestOpen_EosUs()
        {
            // This test will be replaced with something else later.
            // Right now, it just ensures there's no exceptions and that the EosUsPath exists
            using (var eosUS = new NdsRom())
            {
                await eosUS.OpenFile(EosUsPath, Provider);
                //await eosUS.Unpack("RawFiles-EOSUS", Provider);
            }
        }
    }
}
