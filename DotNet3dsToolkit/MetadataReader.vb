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
    ''' Gets the system corresponding to the given directory.
    ''' </summary>
    ''' <param name="path">The filename of the ROM to check.</param>
    ''' <returns>A <see cref="SystemType"/> corresponding to ROM located at <paramref name="path"/>.</returns>
    Public Shared Function GetROMSystem(path As String) As SystemType
        Throw New NotImplementedException
    End Function

    ''' <summary>
    ''' Gets the game ID from the unpacked ROM in the given directory.
    ''' </summary>
    ''' <param name="path">The filename of the ROM to check.</param>
    ''' <param name="system">The type of system the unpacked ROM is for.</param>
    ''' <returns>The ROM's game code.</returns>
    Public Shared Function GetROMGameID(path As String, system As SystemType) As String
        Throw New NotImplementedException
    End Function

    ''' <summary>
    ''' Gets the game ID from the unpacked ROM in the given directory.
    ''' </summary>
    ''' <param name="path">The filename of the ROM ROM to check.</param>
    ''' <returns>The ROM's game code.</returns>
    Public Shared Function GetROMGameID(path As String) As String
        Return GetROMGameID(path, GetDirectorySystem(path))
        End
End Class
