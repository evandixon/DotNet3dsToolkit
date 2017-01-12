Imports System.Collections.Concurrent
Imports Portable.Text
Imports SkyEditor.Core.IO
Imports SkyEditor.Core.Utilities

Public Class GenericNDSRom
    Inherits GenericFile
    Implements IDetectableFileType
    Implements IReportProgress

    Public Overrides Function GetDefaultExtension() As String
        Return "*.nds"
    End Function

#Region "Constructors"

    Public Sub New()
        MyBase.New()
        Me.EnableInMemoryLoad = True
    End Sub

    Public Sub New(filename As String, isReadOnly As Boolean, enableInMemoryLoad As Boolean, provider As IIOProvider)
        MyBase.New(provider, filename, isReadOnly, enableInMemoryLoad)
    End Sub

#End Region

#Region "Events"
    Public Event UnpackProgress(sender As Object, e As ProgressReportedEventArgs) Implements IReportProgress.ProgressChanged
    Public Event Completed As IReportProgress.CompletedEventHandler Implements IReportProgress.Completed
#End Region


#Region "Properties"
    'Credit to http://nocash.emubase.de/gbatek.htm#dscartridgesencryptionfirmware (As of Jan 1 2014) for research
    'Later moved to http://problemkaputt.de/gbatek.htm#dscartridgeheader
    Public Property GameTitle As String
        Get
            Dim e As New ASCIIEncoding
            Return e.GetString(RawData(0, 12)).Trim
        End Get
        Set(value As String)
            Dim e As New ASCIIEncoding
            Dim buffer = e.GetBytes(value)
            For count = 0 To 11
                If buffer.Length > count Then
                    RawData(count) = buffer(count)
                Else
                    RawData(count) = 0
                End If
            Next
        End Set
    End Property
    Public Property GameCode As String
        Get
            Dim e As New ASCIIEncoding
            Return e.GetString(RawData(12, 4)).Trim
        End Get
        Set(value As String)
            Dim e As New ASCIIEncoding
            Dim buffer = e.GetBytes(value)
            For count = 0 To 3
                If buffer.Length > count Then
                    RawData(12 + count) = buffer(count)
                Else
                    RawData(12 + count) = 0
                End If
            Next
        End Set
    End Property
    Private Property MakerCode As String
        Get
            Dim e As New ASCIIEncoding
            Return e.GetString(RawData(16, 2)).Trim
        End Get
        Set(value As String)
            Dim e As New ASCIIEncoding
            Dim buffer = e.GetBytes(value)
            For count = 0 To 1
                If buffer.Length > count Then
                    RawData(16 + count) = buffer(count)
                Else
                    RawData(16 + count) = 0
                End If
            Next
        End Set
    End Property
    Private Property UnitCode As Byte
        Get
            Return RawData(&H12)
        End Get
        Set(value As Byte)
            RawData(&H12) = value
        End Set
    End Property
    Private Property EncryptionSeedSelect As Byte
        Get
            Return RawData(&H13)
        End Get
        Set(value As Byte)
            RawData(&H13) = value
        End Set
    End Property
    ''' <summary>
    ''' Gets or sets the capacity of the cartridge.  Cartridge size = 128KB * 2 ^ (DeviceCapacity)
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Property DeviceCapacity As Byte
        Get
            Return RawData(&H13)
        End Get
        Set(value As Byte)
            RawData(&H13) = value
        End Set
    End Property
    'Reserved: 9 bytes of 0
    Public Property RomVersion As Byte
        Get
            Return RawData(&H1E)
        End Get
        Set(value As Byte)
            RawData(&H1E) = value
        End Set
    End Property
    'Autostart: bit 2 skips menu
    Private Property Arm9RomOffset As Integer
        Get
            Return BitConverter.ToInt32(RawData(&H20, 4), 0)
        End Get
        Set(value As Integer)
            RawData(&H20, 4) = BitConverter.GetBytes(value)
        End Set
    End Property
    Private Property Arm9REntryAddress As Integer
        Get
            Return BitConverter.ToInt32(RawData(&H24, 4), 0)
        End Get
        Set(value As Integer)
            RawData(&H24, 4) = BitConverter.GetBytes(value)
        End Set
    End Property
    Private Property Arm9RamAddress As Integer
        Get
            Return BitConverter.ToInt32(RawData(&H28, 4), 0)
        End Get
        Set(value As Integer)
            RawData(&H28, 4) = BitConverter.GetBytes(value)
        End Set
    End Property
    Private Property Arm9Size As Integer
        Get
            Return BitConverter.ToInt32(RawData(&H2C, 4), 0)
        End Get
        Set(value As Integer)
            RawData(&H2C, 4) = BitConverter.GetBytes(value)
        End Set
    End Property
    Private Property Arm7RomOffset As Integer
        Get
            Return BitConverter.ToInt32(RawData(&H30, 4), 0)
        End Get
        Set(value As Integer)
            RawData(&H30, 4) = BitConverter.GetBytes(value)
        End Set
    End Property
    Private Property Arm7REntryAddress As Integer
        Get
            Return BitConverter.ToInt32(RawData(&H34, 4), 0)
        End Get
        Set(value As Integer)
            RawData(&H34, 4) = BitConverter.GetBytes(value)
        End Set
    End Property
    Private Property Arm7RamAddress As Integer
        Get
            Return BitConverter.ToInt32(RawData(&H38, 4), 0)
        End Get
        Set(value As Integer)
            RawData(&H38, 4) = BitConverter.GetBytes(value)
        End Set
    End Property
    Private Property Arm7Size As Integer
        Get
            Return BitConverter.ToInt32(RawData(&H3C, 4), 0)
        End Get
        Set(value As Integer)
            RawData(&H3C, 4) = BitConverter.GetBytes(value)
        End Set
    End Property
    Private Property FilenameTableOffset As Integer
        Get
            Return BitConverter.ToInt32(RawData(&H40, 4), 0)
        End Get
        Set(value As Integer)
            RawData(&H40, 4) = BitConverter.GetBytes(value)
        End Set
    End Property
    Private Property FilenameTableSize As Integer
        Get
            Return BitConverter.ToInt32(RawData(&H44, 4), 0)
        End Get
        Set(value As Integer)
            RawData(&H44, 4) = BitConverter.GetBytes(value)
        End Set
    End Property
    Private Property FileAllocationTableOffset As Integer
        Get
            Return BitConverter.ToInt32(RawData(&H48, 4), 0)
        End Get
        Set(value As Integer)
            RawData(&H48, 4) = BitConverter.GetBytes(value)
        End Set
    End Property
    Private Property FileAllocationTableSize As Integer
        Get
            Return BitConverter.ToInt32(RawData(&H4C, 4), 0)
        End Get
        Set(value As Integer)
            RawData(&H4C, 4) = BitConverter.GetBytes(value)
        End Set
    End Property
    Private Property FileArm9OverlayOffset As Integer
        Get
            Return BitConverter.ToInt32(RawData(&H50, 4), 0)
        End Get
        Set(value As Integer)
            RawData(&H50, 4) = BitConverter.GetBytes(value)
        End Set
    End Property
    Private Property FileArm9OverlaySize As Integer
        Get
            Return BitConverter.ToInt32(RawData(&H54, 4), 0)
        End Get
        Set(value As Integer)
            RawData(&H54, 4) = BitConverter.GetBytes(value)
        End Set
    End Property
    Private Property FileArm7OverlayOffset As Integer
        Get
            Return BitConverter.ToInt32(RawData(&H58, 4), 0)
        End Get
        Set(value As Integer)
            RawData(&H58, 4) = BitConverter.GetBytes(value)
        End Set
    End Property
    Private Property FileArm7OverlaySize As Integer
        Get
            Return BitConverter.ToInt32(RawData(&H5C, 4), 0)
        End Get
        Set(value As Integer)
            RawData(&H5C, 4) = BitConverter.GetBytes(value)
        End Set
    End Property
    '060h    4     Port 40001A4h setting for normal commands (usually 00586000h)
    '064h    4     Port 40001A4h setting for KEY1 commands   (usually 001808F8h)
    Private Property IconTitleOffset As Integer
        Get
            Return BitConverter.ToInt32(RawData(&H68, 4), 0)
        End Get
        Set(value As Integer)
            RawData(&H68, 4) = BitConverter.GetBytes(value)
        End Set
    End Property
    Private ReadOnly Property IconTitleLength As Integer
        Get
            Return &H840
        End Get
    End Property
    '06Ch    2     Secure Area Checksum, CRC-16 of [ [20h]..7FFFh]
    '06Eh    2     Secure Area Loading Timeout (usually 051Eh)
    '070h    4     ARM9 Auto Load List RAM Address (?)
    '074h    4     ARM7 Auto Load List RAM Address (?)
    '078h    8     Secure Area Disable (by encrypted "NmMdOnly") (usually zero)
    '080h    4     Total Used ROM size (remaining/unused bytes usually FFh-padded)
    '084h    4     ROM Header Size (4000h)
    '088h    38h   Reserved (zero filled)
    '0C0h    9Ch   Nintendo Logo (compressed bitmap, same as in GBA Headers)
    '15Ch    2     Nintendo Logo Checksum, CRC-16 of [0C0h-15Bh], fixed CF56h
    '15Eh    2     Header Checksum, CRC-16 of [000h-15Dh]
    '160h    4     Debug rom_offset   (0=none) (8000h and up)       ;only if debug
    '164h    4     Debug size         (0=none) (max 3BFE00h)        ;version with
    '168h    4     Debug ram_address  (0=none) (2400000h..27BFE00h) ;SIO and 8MB
    '16Ch    4     Reserved (zero filled) (transferred, and stored, but not used)
    '170h    90h   Reserved (zero filled) (transferred, but not stored in RAM)
#End Region

#Region "NitroRom Stuff"

#Region "Private Classes"
    Private Class OverlayTableEntry
        Public Property OverlayID As Integer
        Public Property RamAddress As Integer
        Public Property RamSize As Integer
        Public Property BssSize As Integer
        Public Property StaticInitStart As Integer
        Public Property StaticInitEnd As Integer
        Public Property FileID As Integer
        Public Sub New(RawData As Byte())
            OverlayID = BitConverter.ToInt32(RawData, 0)
            RamAddress = BitConverter.ToInt32(RawData, 4)
            RamSize = BitConverter.ToInt32(RawData, 8)
            BssSize = BitConverter.ToInt32(RawData, &HC)
            StaticInitStart = BitConverter.ToInt32(RawData, &H10)
            StaticInitEnd = BitConverter.ToInt32(RawData, &H14)
            FileID = BitConverter.ToInt32(RawData, &H18)
        End Sub
    End Class

    Private Class FileAllocationEntry
        Public Property Offset As Integer
        Public Property EndAddress As Integer
        Public Sub New(Offset As Integer, EndAddress As Integer)
            Me.Offset = Offset
            Me.EndAddress = EndAddress
        End Sub
    End Class

    Private Class DirectoryMainTable
        Public Property SubTableOffset As Integer
        Public Property FirstSubTableFileID As UInt16
        ''' <summary>
        ''' If this is the root directory, will contain the number of child directories.
        ''' Otherwise, the ID of the parent directory.
        ''' </summary>
        ''' <returns></returns>
        Public Property ParentDir As UInt16
        Public Sub New(RawData As Byte())
            SubTableOffset = BitConverter.ToUInt32(RawData, 0)
            FirstSubTableFileID = BitConverter.ToUInt16(RawData, 4)
            ParentDir = BitConverter.ToUInt16(RawData, 6)
        End Sub
    End Class

    Private Class FNTSubTable
        Public Property Length As Byte
        Public Property Name As String
        Public Property SubDirectoryID As UInt16 'Only for directories
        Public Property ParentFileID As UInt16
    End Class

    Private Class FilenameTable
        Public Property Name As String
        Public Property FileIndex As Integer
        Public ReadOnly Property IsDirectory As Boolean
            Get
                Return FileIndex < 0
            End Get
        End Property
        Public Property Children As List(Of FilenameTable)
        Public Overrides Function ToString() As String
            Return Name
        End Function
        Public Sub New()
            FileIndex = -1
            Children = New List(Of FilenameTable)
        End Sub
    End Class
#End Region

    Private Function ParseArm9OverlayTable() As List(Of OverlayTableEntry)
        Dim out As New List(Of OverlayTableEntry)
        For count = FileArm9OverlayOffset To FileArm9OverlayOffset + FileArm9OverlaySize - 1 Step 32
            out.Add(New OverlayTableEntry(RawData(count, 32)))
        Next
        Return out
    End Function

    Private Function ParseArm7OverlayTable() As List(Of OverlayTableEntry)
        Dim out As New List(Of OverlayTableEntry)
        For count = FileArm7OverlayOffset To FileArm7OverlayOffset + FileArm7OverlaySize - 1 Step 32
            out.Add(New OverlayTableEntry(RawData(count, 32)))
        Next
        Return out
    End Function

    Private Function GetFAT() As List(Of FileAllocationEntry)
        Dim out As New List(Of FileAllocationEntry)
        For count = FileAllocationTableOffset To FileAllocationTableOffset + FileAllocationTableSize - 1 Step 8
            Dim entry As New FileAllocationEntry(BitConverter.ToUInt32(RawData(count, 4), 0), BitConverter.ToUInt32(RawData(count + 4, 4), 0))
            If Not entry.Offset = 0 Then
                out.Add(entry)
            End If
        Next
        Return out
    End Function

    Private Function GetFNT() As FilenameTable
        Dim root As New DirectoryMainTable(RawData(Me.FilenameTableOffset, 8))
        Dim rootDirectories As New List(Of DirectoryMainTable)
        'In the root entry, ParentDir means number of directories
        For count = 8 To root.SubTableOffset - 1 Step 8
            rootDirectories.Add(New DirectoryMainTable(RawData(Me.FilenameTableOffset + count, 8)))
        Next
        'Todo: read the relationship between directories and files
        Dim out As New FilenameTable
        out.Name = "data"
        BuildFNT(out, root, rootDirectories)
        Return out
    End Function
    Private Sub BuildFNT(ParentFNT As FilenameTable, root As DirectoryMainTable, Directories As List(Of DirectoryMainTable))
        For Each item In ReadFNTSubTable(root.SubTableOffset, root.FirstSubTableFileID)
            Dim child As New FilenameTable With {.Name = item.Name}
            ParentFNT.Children.Add(child)
            If item.Length > 128 Then
                BuildFNT(child, Directories((item.SubDirectoryID And &HFFF) - 1), Directories)
            Else
                child.FileIndex = item.ParentFileID
            End If
        Next
    End Sub
    Private Function ReadFNTSubTable(RootSubTableOffset As Integer, ByVal ParentFileID As Integer) As List(Of FNTSubTable)
        Dim subTables As New List(Of FNTSubTable)
        Dim offset = RootSubTableOffset + Me.FilenameTableOffset
        Dim length As Integer = RawData(offset)
        While length > 0
            If length > 128 Then
                'Then it's a sub directory
                'Read the string
                Dim buffer As Byte() = RawData(offset + 1, length - 128)
                Dim s = (New ASCIIEncoding).GetString(buffer)
                'Read sub directory ID
                Dim subDirID As UInt16 = Me.UInt16(offset + 1 + length - 128)
                'Add the result to the list
                subTables.Add(New FNTSubTable With {.Length = length, .Name = s, .SubDirectoryID = subDirID})
                'Increment the offset
                offset += length - 128 + 1 + 2
            ElseIf length < 128 Then
                'Then it's a file
                'Read the string
                Dim buffer As Byte() = RawData(offset + 1, length)
                Dim s = (New ASCIIEncoding).GetString(buffer)
                'Add the result to the list
                subTables.Add(New FNTSubTable With {.Length = length, .Name = s, .ParentFileID = ParentFileID})
                ParentFileID += 1
                'Increment the offset
                offset += length + 1
            Else
                'Reserved.  I'm not sure what to do here.
                Throw New NotSupportedException("Subtable length of 0x80 not supported.")
            End If

            length = RawData(offset)
        End While
        Return subTables
    End Function
    ''' <summary>
    ''' Extracts the files contained within the ROMs.
    ''' Extractions either run synchronously or asynchrounously, depending on the value of IsThreadSafe.
    ''' </summary>
    ''' <param name="TargetDir">Directory to store the extracted files.</param>
    ''' <returns></returns>
    Public Async Function Unpack(TargetDir As String, Provider As IIOProvider) As Task
        Dim fat = GetFAT()

        'Set up extraction dependencies
        CurrentExtractProgress = 0
        CurrentExtractMax = fat.Count
        ExtractionTasks = New ConcurrentBag(Of Task)

        'Ensure directory exists
        If Not Provider.DirectoryExists(TargetDir) Then
            Provider.CreateDirectory(TargetDir)
        End If

        'Start extracting
        '-Header
        Dim headerTask = Task.Run(New Action(Sub()
                                                 Provider.WriteAllBytes(IO.Path.Combine(TargetDir, "header.bin"), RawData(0, &H200))
                                             End Sub))
        If Me.IsThreadSafe Then
            ExtractionTasks.Add(headerTask)
        Else
            Await headerTask
        End If

        '-Arm9
        Dim arm9Task = Task.Run(New Action(Sub()
                                               Dim arm9buffer As New List(Of Byte)
                                               arm9buffer.AddRange(RawData(Me.Arm9RomOffset, Me.Arm9Size))

                                               'Write an additional 0xC bytes if the next 4 equal: 21 06 C0 DE
                                               Dim footer As Int32 = Int32(Me.Arm9RomOffset + Me.Arm9Size)
                                               If footer = &HDEC00621 Then
                                                   arm9buffer.AddRange(RawData(Me.Arm9RomOffset + Me.Arm9Size, &HC))
                                               End If

                                               Provider.WriteAllBytes(IO.Path.Combine(TargetDir, "arm9.bin"), arm9buffer.ToArray)
                                           End Sub))
        If Me.IsThreadSafe Then
            ExtractionTasks.Add(arm9Task)
        Else
            Await arm9Task
        End If

        '-Arm7
        Dim arm7Task = Task.Run(New Action(Sub()
                                               Provider.WriteAllBytes(IO.Path.Combine(TargetDir, "arm7.bin"), RawData(Me.Arm7RomOffset, Me.Arm7Size))
                                           End Sub))
        If Me.IsThreadSafe Then
            ExtractionTasks.Add(arm7Task)
        Else
            Await arm7Task
        End If

        '-Arm9 overlay table (y9.bin)
        Dim y9Task = Task.Run(New Action(Sub()
                                             Provider.WriteAllBytes(IO.Path.Combine(TargetDir, "y9.bin"), RawData(Me.FileArm9OverlayOffset, Me.FileArm9OverlaySize))
                                         End Sub))

        If Me.IsThreadSafe Then
            ExtractionTasks.Add(y9Task)
        Else
            Await y9Task
        End If

        '-Extract arm7 overlay table (y7.bin)
        Dim y7Task = Task.Run(New Action(Sub()
                                             Provider.WriteAllBytes(IO.Path.Combine(TargetDir, "y7.bin"), RawData(Me.FileArm7OverlayOffset, Me.FileArm7OverlaySize))
                                         End Sub))

        If Me.IsThreadSafe Then
            ExtractionTasks.Add(y7Task)
        Else
            Await y7Task
        End If
        '-Extract overlays
        Dim overlay9 = ExtractOverlay(fat, ParseArm9OverlayTable, IO.Path.Combine(TargetDir, "overlay"), Provider)

        If Me.IsThreadSafe Then
            ExtractionTasks.Add(overlay9)
        Else
            Await overlay9
        End If

        Dim overlay7 = ExtractOverlay(fat, ParseArm7OverlayTable, IO.Path.Combine(TargetDir, "overlay7"), Provider)

        If Me.IsThreadSafe Then
            ExtractionTasks.Add(overlay7)
        Else
            Await overlay7
        End If
        '-Extract icon (banner.bin)
        Dim iconTask = Task.Run(Sub()
                                    If IconTitleOffset > 0 Then '0 means none
                                        Provider.WriteAllBytes(IO.Path.Combine(TargetDir, "banner.bin"), RawData(IconTitleOffset, IconTitleLength))
                                    End If
                                End Sub)

        If Me.IsThreadSafe Then
            ExtractionTasks.Add(iconTask)
        Else
            Await iconTask
        End If

        '- Extract files
        Dim filesExtraction = ExtractFiles(fat, GetFNT, TargetDir, Provider)
        If Me.IsThreadSafe Then
            ExtractionTasks.Add(filesExtraction)
        Else
            Await filesExtraction
        End If

        'Wait for everything to finish
        Await Task.WhenAll(ExtractionTasks)
    End Function

    ''' <summary>
    ''' Extracts contained files if the file is thread safe, otherwise, extracts files one at a time.
    ''' </summary>
    ''' <param name="FAT"></param>
    ''' <param name="Root"></param>
    ''' <param name="TargetDir"></param>
    ''' <returns></returns>
    Private Async Function ExtractFiles(fat As List(Of FileAllocationEntry), root As FilenameTable, targetDir As String, provider As IIOProvider) As Task
        Dim dest As String = IO.Path.Combine(targetDir, root.Name)
        Dim f As New AsyncFor
        f.RunSynchronously = Not Me.IsThreadSafe
        f.BatchSize = root.Children.Count
        Await (f.RunForEach(Async Function(item As FilenameTable) As Task
                                If item.IsDirectory Then
                                    Await ExtractFiles(fat, item, dest, provider)
                                Else
                                    Dim entry = fat(item.FileIndex)
                                    Dim parentDir = IO.Path.GetDirectoryName(IO.Path.Combine(dest, item.Name))
                                    If Not provider.DirectoryExists(parentDir) Then
                                        provider.CreateDirectory(parentDir)
                                    End If
                                    provider.WriteAllBytes(IO.Path.Combine(dest, item.Name), RawData(entry.Offset, entry.EndAddress - entry.Offset))
                                    System.Threading.Interlocked.Increment(CurrentExtractProgress)
                                End If
                            End Function, root.Children))
    End Function

    Private Async Function ExtractOverlay(FAT As List(Of FileAllocationEntry), overlayTable As List(Of OverlayTableEntry), targetDir As String, provider As IIOProvider) As Task
        If overlayTable.Count > 0 AndAlso Not provider.DirectoryExists(targetDir) Then
            provider.CreateDirectory(targetDir)
        End If
        Dim f As New AsyncFor
        f.RunSynchronously = Not Me.IsThreadSafe
        f.BatchSize = overlayTable.Count
        Await f.RunForEach(Sub(item As OverlayTableEntry)
                               Dim dest = IO.Path.Combine(targetDir, "overlay_" & item.FileID.ToString.PadLeft(4, "0"c) & ".bin")
                               Dim entry = FAT(item.FileID)
                               provider.WriteAllBytes(dest, RawData(entry.Offset, entry.EndAddress - entry.Offset))
                           End Sub, overlayTable)
    End Function

    ''' <summary>
    ''' Gets or sets the total number of files extracted in the current unpacking process.
    ''' </summary>
    ''' <returns></returns>
    Private Property CurrentExtractProgress As Integer
        Get
            Return _extractProgress
        End Get
        Set(value As Integer)
            _extractProgress = value
            RaiseEvent UnpackProgress(Me, New ProgressReportedEventArgs With {.IsIndeterminate = False, .Progress = ExtractionProgress, .Message = Message})
        End Set
    End Property
    Dim _extractProgress As Integer

    ''' <summary>
    ''' Gets or sets the total number of files in the current unpacking process.
    ''' </summary>
    ''' <returns></returns>
    Private Property CurrentExtractMax As Integer

    ''' <summary>
    ''' Currently running tasks that are part of the unpacking process.
    ''' </summary>
    ''' <returns></returns>
    Private Property ExtractionTasks As ConcurrentBag(Of Task)

    ''' <summary>
    ''' The progress of the current unpacking process.
    ''' </summary>
    ''' <returns></returns>
    Public ReadOnly Property ExtractionProgress As Single Implements IReportProgress.Progress
        Get
            Return CurrentExtractProgress / CurrentExtractMax
        End Get
    End Property

    Private ReadOnly Property IsExtractionIndeterminate As Boolean Implements IReportProgress.IsIndeterminate
        Get
            Return False
        End Get
    End Property

    Private ReadOnly Property IsCompleted As Boolean Implements IReportProgress.IsCompleted
        Get
            Return CurrentExtractProgress = 1
        End Get
    End Property

    Public ReadOnly Property Message As String Implements IReportProgress.Message
        Get
            If IsCompleted Then
                Return My.Resources.Language.Complete
            Else
                Return My.Resources.Language.LoadingUnpacking
            End If
        End Get
    End Property
#End Region

    Public Overridable Function IsFileOfType(file As GenericFile) As Task(Of Boolean) Implements IDetectableFileType.IsOfType
        Return Task.FromResult(file.Length > &H15D AndAlso file.RawData(&H15C) = &H56 AndAlso file.RawData(&H15D) = &HCF)
    End Function

End Class
