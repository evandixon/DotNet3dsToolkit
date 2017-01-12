Imports System.IO
Imports Microsoft.VisualBasic.Devices
Imports SkyEditor.Core.IO

Namespace Misc
    Public Class WindowsIOProvider
        Implements IIOProvider

        Public Sub New()
        End Sub

        Public Overridable Sub CopyFile(sourceFilename As String, destinationFilename As String) Implements IIOProvider.CopyFile
            File.Copy(sourceFilename, destinationFilename, True)
        End Sub

        Public Overridable Sub CreateDirectory(path As String) Implements IIOProvider.CreateDirectory
            Directory.CreateDirectory(path)
        End Sub

        Public Overridable Sub DeleteDirectory(path As String) Implements IIOProvider.DeleteDirectory
            Directory.Delete(path, True)
        End Sub

        Public Overridable Sub DeleteFile(filename As String) Implements IIOProvider.DeleteFile
            File.Delete(filename)
        End Sub

        Public Overridable Sub WriteAllBytes(filename As String, data() As Byte) Implements IIOProvider.WriteAllBytes
            File.WriteAllBytes(filename, data)
        End Sub

        Public Overridable Sub WriteAllText(filename As String, data As String) Implements IIOProvider.WriteAllText
            File.WriteAllText(filename, data)
        End Sub

        Public Overridable Function CanLoadFileInMemory(fileSize As Long) As Boolean Implements IIOProvider.CanLoadFileInMemory
            Return (New ComputerInfo).AvailablePhysicalMemory > (fileSize + 500 * 1024 * 1024)
        End Function

        Public Overridable Function DirectoryExists(directoryPath As String) As Boolean Implements IIOProvider.DirectoryExists
            Return Directory.Exists(directoryPath)
        End Function

        Public Overridable Function FileExists(Filename As String) As Boolean Implements IIOProvider.FileExists
            Return File.Exists(Filename)
        End Function

        Public Overridable Function GetDirectories(path As String, topDirectoryOnly As Boolean) As String() Implements IIOProvider.GetDirectories
            If topDirectoryOnly Then
                Return Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly)
            Else
                Return Directory.GetDirectories(path, "*", SearchOption.AllDirectories)
            End If
        End Function

        Public Overridable Function GetFileLength(filename As String) As Long Implements IIOProvider.GetFileLength
            Return (New FileInfo(filename)).Length
        End Function

        Public Overridable Function GetFiles(path As String, searchPattern As String, topDirectoryOnly As Boolean) As String() Implements IIOProvider.GetFiles
            If topDirectoryOnly Then
                Return Directory.GetFiles(path, searchPattern, SearchOption.TopDirectoryOnly)
            Else
                Return Directory.GetFiles(path, searchPattern, SearchOption.AllDirectories)
            End If
        End Function

        Public Overridable Function GetTempDirectory() As String Implements IIOProvider.GetTempDirectory
            Dim tempDir = Path.Combine(Path.GetTempPath, "SkyEditor", Guid.NewGuid.ToString)
            If Not DirectoryExists(tempDir) Then
                CreateDirectory(tempDir)
            End If
            Return tempDir
        End Function

        Public Overridable Function GetTempFilename() As String Implements IIOProvider.GetTempFilename
            Return Path.GetTempFileName
        End Function

        Public Overridable Function OpenFile(filename As String) As Stream Implements IIOProvider.OpenFile
            Return File.Open(filename, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read)
        End Function

        Public Overridable Function OpenFileReadOnly(filename As String) As Stream Implements IIOProvider.OpenFileReadOnly
            Return File.Open(filename, FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite)
        End Function

        Public Overridable Function OpenFileWriteOnly(filename As String) As Stream Implements IIOProvider.OpenFileWriteOnly
            Return File.Open(filename, FileMode.OpenOrCreate, FileAccess.Write)
        End Function

        Public Overridable Function ReadAllBytes(filename As String) As Byte() Implements IIOProvider.ReadAllBytes
            Return File.ReadAllBytes(filename)
        End Function

        Public Overridable Function ReadAllText(filename As String) As String Implements IIOProvider.ReadAllText
            Return File.ReadAllText(filename)
        End Function
    End Class

End Namespace