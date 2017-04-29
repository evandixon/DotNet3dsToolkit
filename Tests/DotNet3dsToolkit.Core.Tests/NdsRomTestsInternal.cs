using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkyEditor.Core.IO;
using System;
using System.Collections.Generic;
using System.Text;

namespace DotNet3dsToolkit.Core.Tests
{
    [TestClass]
    public class NdsRomTestsInternal
    {
        public const string TestCategory = "NDS ROM (Internal)";

        public class TestNdsRom : NdsRom
        {
            public new string[] GetPathParts(string path)
            {
                return base.GetPathParts(path);
            }
        }

        [TestMethod]
        [TestCategory(TestCategory)]
        public void GetPathParts_Root()
        {
            var testRom = new TestNdsRom();
            var parts = testRom.GetPathParts("/");
            Assert.AreEqual(1, parts.Length);
            Assert.AreEqual("", parts[0]);
        }

        [TestMethod]
        [TestCategory(TestCategory)]
        public void GetPathParts_OverlayX_Absolute()
        {
            var testRom = new TestNdsRom();
            var parts = testRom.GetPathParts("/overlay/overlay_0000.bin");
            Assert.AreEqual(2, parts.Length);
            Assert.AreEqual("overlay", parts[0]);
            Assert.AreEqual("overlay_0000.bin", parts[1]);
        }

        [TestMethod]
        [TestCategory(TestCategory)]
        public void GetPathParts_OverlayX_RelativeToRoot()
        {
            var testRom = new TestNdsRom();
            var parts = testRom.GetPathParts("overlay/overlay_0000.bin");
            Assert.AreEqual(2, parts.Length);
            Assert.AreEqual("overlay", parts[0]);
            Assert.AreEqual("overlay_0000.bin", parts[1]);
        }

        [TestMethod]
        [TestCategory(TestCategory)]
        public void GetPathParts_OverlayX_RelativeToOverlay()
        {
            var testRom = new TestNdsRom();
            (testRom as IIOProvider).WorkingDirectory = "/overlay";
            var parts = testRom.GetPathParts("overlay_0000.bin");
            Assert.AreEqual(2, parts.Length);
            Assert.AreEqual("overlay", parts[0]);
            Assert.AreEqual("overlay_0000.bin", parts[1]);
        }
    }
}
