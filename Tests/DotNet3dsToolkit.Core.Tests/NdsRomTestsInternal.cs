using Microsoft.VisualStudio.TestTools.UnitTesting;
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
    }
}
