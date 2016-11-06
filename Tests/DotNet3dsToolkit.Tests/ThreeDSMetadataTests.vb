Imports System.IO
Imports System.Text
Imports Microsoft.VisualStudio.TestTools.UnitTesting

<TestClass> Public Class ThreeDSMetadataTests

    Const titleID = "000400000F800100"
    Const threeDS = "FBI.3ds"
    Const cxi = "FBI.cxi"
    Const cia = "FBI.cia"

    <TestInitialize> Public Sub CopyTestFiles()
        File.WriteAllBytes(threeDS, My.Resources.FBI_3ds)
        File.WriteAllBytes(cxi, My.Resources.FBI_cxi)
        File.WriteAllBytes(cia, My.Resources.FBI_cia)
    End Sub

    <TestMethod> Public Sub Test3DSROMTitleIDWithSystem()
        Dim experimental = MetadataReader.GetROMGameID(threeDS, SystemType.ThreeDS).Result
        Assert.AreEqual(titleID, experimental)
    End Sub

    <TestMethod> Public Sub Test3DSROMTitleIDAutoROM()
        Dim experimental = MetadataReader.GetROMGameID(threeDS).Result
        Assert.AreEqual(titleID, experimental)
    End Sub

    <TestMethod> Public Sub Test3DSROMTitleIDAuto()
        Dim experimental = MetadataReader.GetGameID(threeDS).Result
        Assert.AreEqual(titleID, experimental)
    End Sub

    <TestMethod> Public Sub TestCXIROMTitleIDWithSystem()
        Dim experimental = MetadataReader.GetROMGameID(cxi, SystemType.ThreeDS).Result
        Assert.AreEqual(titleID, experimental)
    End Sub

    <TestMethod> Public Sub TestCXIROMTitleIDAutoROM()
        Dim experimental = MetadataReader.GetROMGameID(cxi).Result
        Assert.AreEqual(titleID, experimental)
    End Sub

    <TestMethod> Public Sub TestCXIROMTitleIDAuto()
        Dim experimental = MetadataReader.GetGameID(cxi).Result
        Assert.AreEqual(titleID, experimental)
    End Sub

    <TestMethod> Public Sub TestCIAROMTitleIDWithSystem()
        Dim experimental = MetadataReader.GetROMGameID(cia, SystemType.ThreeDS).Result
        Assert.AreEqual(titleID, experimental)
    End Sub

    <TestMethod> Public Sub TestCIAROMTitleIDAutoROM()
        Dim experimental = MetadataReader.GetGameID(cia).Result
        Assert.AreEqual(titleID, experimental)
    End Sub

    <TestMethod> Public Sub TestCIAROMTitleIDAuto()
        Dim experimental = MetadataReader.GetGameID(cia).Result
        Assert.AreEqual(titleID, experimental)
    End Sub

End Class