Imports System.IO
Imports System.Text
Imports SkyEditor.Core.IO

''' <summary>
''' Reads metadata from packed or unpacked ROMs.
''' </summary>
Public Class MetadataReader

#Region "CIA"
    ''' <remarks>From CTRTool
    ''' https://github.com/profi200/Project_CTR/blob/d32f096e3ea8d6cacc2f8e8f43d4eec51394eca2/ctrtool/utils.c </remarks>
    Private Shared Function Align(offset As Integer, alignment As Integer)
        Dim mask As Integer = Not (alignment - 1)
        Return offset + (alignment - 1) And mask
    End Function

    ''' <summary>
    ''' Gets the offset of the content section of the given CIA file.
    ''' </summary>
    Friend Shared Function GetCIAContentOffset(cia As GenericFile) As Integer
        Dim offsetCerts = Align(cia.ReadInt32(0), 64)
        Dim offsetTik = Align(cia.ReadInt32(&H8) + offsetCerts, 64)
        Dim offsetTmd = Align(cia.ReadInt32(&HC) + offsetTik, 64)
        Dim offsetContent = Align(cia.ReadInt32(&H10) + offsetTmd, 64)
        Return offsetContent
    End Function
#End Region

    ''' <summary>
    ''' Gets the system corresponding to the given directory.
    ''' </summary>
    ''' <param name="path">The directory containing the unpacked ROM to check.</param>
    ''' <returns>A <see cref="SystemType"/> corresponding to the extracted files located in the directory <paramref name="path"/>.</returns>
    Public Shared Function GetDirectorySystem(path As String) As SystemType
        If File.Exists(IO.Path.Combine(path, "arm9.bin")) AndAlso File.Exists(IO.Path.Combine(path, "arm7.bin")) AndAlso File.Exists(IO.Path.Combine(path, "header.bin")) AndAlso Directory.Exists(IO.Path.Combine(path, "data")) Then
            Return SystemType.NDS
        ElseIf File.Exists(IO.Path.Combine(path, "exheader.bin")) AndAlso Directory.Exists(IO.Path.Combine(path, "exefs")) AndAlso Directory.Exists(IO.Path.Combine(path, "romfs")) Then
            Return SystemType.ThreeDS
        Else
            Return SystemType.Unknown
        End If
    End Function

    ''' <summary>
    ''' Gets the game ID from the unpacked ROM in the given directory.
    ''' </summary>
    ''' <param name="path">The directory containing the unpacked ROM to check.</param>
    ''' <param name="system">The type of system the unpacked ROM is for.</param>
    ''' <returns>The unpacked ROM's game code.</returns>
    Public Shared Function GetDirectoryGameID(path As String, system As SystemType) As String
        Select Case system
            Case SystemType.NDS
                Dim header = File.ReadAllBytes(IO.Path.Combine(path, "header.bin"))
                Dim e As New ASCIIEncoding
                Return e.GetString(header, &HC, 4)
            Case SystemType.ThreeDS
                Dim exheader = File.ReadAllBytes(IO.Path.Combine(path, "exheader.bin"))
                Return BitConverter.ToUInt64(exheader, &H200).ToString("X").PadLeft(16, "0"c)
            Case Else
                Throw New NotSupportedException(String.Format(My.Resources.Language.ErrorSystemNotSupported, system.ToString))
        End Select
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
    Public Shared Async Function GetROMSystem(path As String) As Task(Of SystemType)
        Dim e As New ASCIIEncoding
        Using file As New GenericFile
            file.EnableInMemoryLoad = False
            file.IsReadOnly = True
            Await file.OpenFile(path, New PhysicalIOProvider)

            Dim n As New GenericNDSRom

            If Await n.IsFileOfType(file) Then
                Return SystemType.NDS
            ElseIf file.Length > 104 AndAlso e.GetString(Await file.ReadAsync(&H100, 4)) = "NCSD" Then
                'CCI
                Return SystemType.ThreeDS
            ElseIf file.Length > 104 AndAlso e.GetString(Await file.ReadAsync(&H100, 4)) = "NCCH" Then
                'CXI
                Return SystemType.ThreeDS
            ElseIf file.Length > Await file.ReadInt32Async(0) AndAlso e.GetString(Await file.ReadAsync(&H100 + GetCIAContentOffset(file), 4)) = "NCCH" Then
                'CIA
                Return SystemType.ThreeDS
            Else
                Return SystemType.Unknown
            End If
        End Using
    End Function

    ''' <summary>
    ''' Gets the game ID from a ROM.
    ''' </summary>
    ''' <param name="path">The filename of the ROM to check.</param>
    ''' <param name="system">The type of system the unpacked ROM is for.</param>
    ''' <returns>The ROM's game code.</returns>
    Public Shared Async Function GetROMGameID(path As String, system As SystemType) As Task(Of String)
        Select Case system
            Case SystemType.NDS
                Dim code As String

                Using n As New GenericNDSRom
                    n.EnableInMemoryLoad = False 'In-memory load would be overkill for simply reading the game code
                    n.IsReadOnly = True
                    Await n.OpenFile(path, New PhysicalIOProvider)
                    code = n.GameCode
                End Using

                Return code
            Case SystemType.ThreeDS
                Dim e As New ASCIIEncoding
                Using file As New GenericFile
                    file.EnableInMemoryLoad = False
                    file.IsReadOnly = True
                    Await file.OpenFile(path, New PhysicalIOProvider)

                    If file.Length > 104 AndAlso e.GetString(Await file.ReadAsync(&H100, 4)) = "NCSD" Then
                        'CCI
                        Return BitConverter.ToUInt64(Await file.ReadAsync(&H108, 8), 0).ToString("X").PadLeft(16, "0"c)
                    ElseIf file.Length > 104 AndAlso e.GetString(Await file.ReadAsync(&H100, 4)) = "NCCH" Then
                        'CXI
                        Return BitConverter.ToUInt64(Await file.ReadAsync(&H108, 8), 0).ToString("X").PadLeft(16, "0"c)
                    ElseIf file.Length > Await file.ReadInt32Async(0) AndAlso e.GetString(Await file.ReadAsync(&H100 + GetCIAContentOffset(file), 4)) = "NCCH" Then
                        'CIA
                        Return BitConverter.ToUInt64(Await file.ReadAsync(&H108 + GetCIAContentOffset(file), 8), 0).ToString("X").PadLeft(16, "0"c)
                    Else
                        Throw New NotSupportedException(My.Resources.Language.ErrorInvalidFileFormat)
                    End If
                End Using
            Case Else
                Throw New NotSupportedException(String.Format(My.Resources.Language.ErrorSystemNotSupported, system.ToString))
        End Select
    End Function

    ''' <summary>
    ''' Gets the game ID from a ROM.
    ''' </summary>
    ''' <param name="path">The filename of the ROM ROM to check.</param>
    ''' <returns>The ROM's game code.</returns>
    Public Shared Async Function GetROMGameID(path As String) As Task(Of String)
        Return Await GetROMGameID(path, Await GetROMSystem(path))
    End Function

    ''' <summary>
    ''' Gets the system corresponding from a packed or unpacked ROM.
    ''' </summary>
    ''' <param name="path">The filename of the ROM to check.</param>
    ''' <returns>A <see cref="SystemType"/> corresponding to ROM located at <paramref name="path"/>.</returns>
    ''' <exception cref="IOException">Thrown when <paramref name="path"/> is neither a file nor a directory.</exception>
    Public Shared Async Function GetSystem(path As String) As Task(Of SystemType)
        If Directory.Exists(path) Then
            Return GetDirectorySystem(path)
        ElseIf File.Exists(path) Then
            Return Await GetROMSystem(path)
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
    Public Shared Async Function GetGameID(path As String, system As SystemType) As Task(Of String)
        If Directory.Exists(path) Then
            Return GetDirectoryGameID(path, system)
        ElseIf File.Exists(path) Then
            Return Await GetROMGameID(path, system)
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
    Public Shared Async Function GetGameID(path As String) As Task(Of String)
        If Directory.Exists(path) Then
            Return GetDirectoryGameID(path, GetDirectorySystem(path))
        ElseIf File.Exists(path) Then
            Return Await GetROMGameID(path, Await GetROMSystem(path))
        Else
            Throw New IOException(String.Format(My.Resources.Language.ErrorFileDirNotFound, path))
        End If
    End Function
End Class
