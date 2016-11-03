Imports System.IO
Imports System.Text
Imports Microsoft.VisualStudio.TestTools.UnitTesting

<TestClass> Public Class ThreeDSMetadataTests

    <TestInitialize> Public Sub CopyTestFiles()
        File.WriteAllBytes("FBI.3ds", My.Resources.FBI_3ds)
        File.WriteAllBytes("FBI.cia", My.Resources.FBI_cia)
    End Sub

    <TestMethod> Public Sub Test3DSROMTitleID()
        Dim experimental = MetadataReader.GetROMGameID("FBI.3ds", SystemType.ThreeDS).Result
        Assert.AreEqual("000400000F800100", experimental)
    End Sub

    <TestMethod> Public Sub Test3DSROMTitleIDAuto()
        Dim experimental = MetadataReader.GetGameID("FBI.3ds").Result
        Assert.AreEqual("000400000F800100", experimental)
    End Sub

    <TestMethod> Public Sub TestCIAROMTitleID()
        Dim experimental = MetadataReader.GetROMGameID("FBI.cia", SystemType.ThreeDS).Result
        Assert.AreEqual("000400000F800100", experimental)
    End Sub

    <TestMethod> Public Sub TestCIAROMTitleIDAuto()
        Dim experimental = MetadataReader.GetGameID("FBI.cia").Result
        Assert.AreEqual("000400000F800100", experimental)
    End Sub

End Class