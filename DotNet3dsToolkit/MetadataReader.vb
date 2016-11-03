Imports System.IO

Public Class MetadataReader
    ''' <summary>
    ''' Gets the system corresponding to the given directory.
    ''' </summary>
    ''' <param name="path">The directory containing the unpacked ROM to check.</param>
    ''' <returns>A <see cref="SystemType"/> corresponding to the extracted files located in the directory <paramref name="path"/>.</returns>
    Public Shared Function GetDirectorySystem(path As String) As SystemType
        Throw New NotImplementedException
    End Function

    ''' <summary>
    ''' Gets the game ID from the unpacked ROM in the given directory.
    ''' </summary>
    ''' <param name="path">The directory containing the unpacked ROM to check.</param>
    ''' <param name="system">The type of system the unpacked ROM is for.</param>
    ''' <returns>The unpacked ROM's game code.</returns>
    Public Shared Function GetDirectoryGameID(path As String, system As SystemType) As String
        Throw New NotImplementedException
    End Function

    ''' <summary>
    ''' Gets the game ID from the unpacked ROM in the given directory.
    ''' </summary>
    ''' <param name="path">The directory containing the unpacked ROM to check.</param>
    ''' <returns>The unpacked ROM's game code.</returns>
    Public Shared Function GetDirectoryGameID(path As String) As String
        Return GetDirectoryGameID(path, GetDirectorySystem(path))
    End Function

    ''' <summary>
    ''' Gets the system corresponding to the given ROM.
    ''' </summary>
    ''' <param name="path">The filename of the ROM to check.</param>
    ''' <returns>A <see cref="SystemType"/> corresponding to ROM located at <paramref name="path"/>.</returns>
    Public Shared Function GetROMSystem(path As String) As SystemType
        Throw New NotImplementedException
    End Function

    ''' <summary>
    ''' Gets the game ID from a ROM.
    ''' </summary>
    ''' <param name="path">The filename of the ROM to check.</param>
    ''' <param name="system">The type of system the unpacked ROM is for.</param>
    ''' <returns>The ROM's game code.</returns>
    Public Shared Function GetROMGameID(path As String, system As SystemType) As String
        Throw New NotImplementedException
    End Function

    ''' <summary>
    ''' Gets the game ID from a ROM.
    ''' </summary>
    ''' <param name="path">The filename of the ROM ROM to check.</param>
    ''' <returns>The ROM's game code.</returns>
    Public Shared Function GetROMGameID(path As String) As String
        Return GetROMGameID(path, GetDirectorySystem(path))
    End Function

    ''' <summary>
    ''' Gets the system corresponding from a packed or unpacked ROM.
    ''' </summary>
    ''' <param name="path">The filename of the ROM to check.</param>
    ''' <returns>A <see cref="SystemType"/> corresponding to ROM located at <paramref name="path"/>.</returns>
    ''' <exception cref="IOException">Thrown when <paramref name="path"/> is neither a file nor a directory.</exception>
    Public Shared Function GetSystem(path As String) As SystemType
        If Directory.Exists(path) Then
            Return GetDirectorySystem(path)
        ElseIf File.Exists(path) Then
            Return GetROMSystem(path)
        Else
            Throw New IOException(String.Format(My.Resources.Language.ErrorFileDirNotFound, path))
        End If
    End Function

    ''' <summary>
    ''' Gets the game ID from a packed or unpacked ROM.
    ''' </summary>
    ''' <param name="path">The filename of the ROM to check.</param>
    ''' <param name="system">The type of system the unpacked ROM is for.</param>
    ''' <returns>The ROM's game code.</returns>
    ''' <exception cref="IOException">Thrown when <paramref name="path"/> is neither a file nor a directory.</exception>
    Public Shared Function GetGameID(path As String, system As SystemType) As String
        If Directory.Exists(path) Then
            Return GetDirectoryGameID(path, system)
        ElseIf File.Exists(path) Then
            Return GetROMGameID(path, system)
        Else
            Throw New IOException(String.Format(My.Resources.Language.ErrorFileDirNotFound, path))
        End If
    End Function

    ''' <summary>
    ''' Gets the game ID from from a packed or unpacked ROM.
    ''' </summary>
    ''' <param name="path">The filename of the ROM ROM to check.</param>
    ''' <returns>The ROM's game code.</returns>
    ''' <exception cref="IOException">Thrown when <paramref name="path"/> is neither a file nor a directory.</exception>
    Public Shared Function GetGameID(path As String) As String
        If Directory.Exists(path) Then
            Return GetDirectoryGameID(path, GetDirectorySystem(path))
        ElseIf File.Exists(path) Then
            Return GetROMGameID(path, GetDirectorySystem(path))
        Else
            Throw New IOException(String.Format(My.Resources.Language.ErrorFileDirNotFound, path))
        End If
    End Function
End Class
