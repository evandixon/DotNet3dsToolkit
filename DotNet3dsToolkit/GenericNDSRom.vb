Imports System.Collections.Concurrent
Imports System.IO
Imports Portable.Text
Imports SkyEditor.Core.IO
Imports SkyEditor.Core.Utilities

Public Class GenericNDSRom
    Inherits GenericFile
    'Implements IDetectableFileType
    Implements IReportProgress
    Implements IIOProvider

    Public Overrides Function GetDefaultExtension() As String
        Return "*.nds"
    End Function

    Public Sub New()
        MyBase.New()
        Me.EnableInMemoryLoad = True
        ResetWorkingDirectory()
    End Sub

    Public Overrides Async Function OpenFile(filename As String, provider As IIOProvider) As Task
        Await MyBase.OpenFile(filename, provider)
        Await Task.Run(Sub() CurrentFilenameTable = GetFNT())
        Await Task.Run(Async Function() As Task
                           CurrentFileAllocationTable = Await GetFAT()
                       End Function)
        CurrentArm9OverlayTable = ParseArm9OverlayTable()
        CurrentArm9OverlayTable = ParseArm7OverlayTable()
    End Function

#Region "Events"
    Public Event UnpackProgress As EventHandler(Of ProgressReportedEventArgs) Implements IReportProgress.ProgressChanged
    Public Event Completed As EventHandler Implements IReportProgress.Completed
#End Region

#Region "Properties"
    'Credit to http://nocash.emubase.de/gbatek.htm#dscartridgesencryptionfirmware (As of Jan 1 2014) for research
    'Later moved to http://problemkaputt.de/gbatek.htm#dscartridgeheader
    Public Property GameTitle As String
        Get
            Dim e As New ASCIIEncoding
            Return e.GetString(Read(0, 12)).Trim
        End Get
        Set(value As String)
            Dim e As New ASCIIEncoding
            Dim buffer = e.GetBytes(value)
            For count = 0 To 11
                If buffer.Length > count Then
                    Write(count, buffer(count))
                Else
                    Write(count, 0)
                End If
            Next
        End Set
    End Property
    Public Property GameCode As String
        Get
            Dim e As New ASCIIEncoding
            Return e.GetString(Read(12, 4)).Trim
        End Get
        Set(value As String)
            Dim e As New ASCIIEncoding
            Dim buffer = e.GetBytes(value)
            For count = 0 To 3
                If buffer.Length > count Then
                    Write(12 + count, buffer(count))
                Else
                    Write(12 + count, 0)
                End If
            Next
        End Set
    End Property
    Private Property MakerCode As String
        Get
            Dim e As New ASCIIEncoding
            Return e.GetString(Read(16, 2)).Trim
        End Get
        Set(value As String)
            Dim e As New ASCIIEncoding
            Dim buffer = e.GetBytes(value)
            For count = 0 To 1
                If buffer.Length > count Then
                    Write(16 + count, buffer(count))
                Else
                    Write(16 + count, 0)
                End If
            Next
        End Set
    End Property
    Private Property UnitCode As Byte
        Get
            Return Read(&H12)
        End Get
        Set(value As Byte)
            Write(&H12, value)
        End Set
    End Property
    Private Property EncryptionSeedSelect As Byte
        Get
            Return Read(&H13)
        End Get
        Set(value As Byte)
            Write(&H13, value)
        End Set
    End Property

    ''' <summary>
    ''' Gets or sets the capacity of the cartridge.  Cartridge size = 128KB * 2 ^ (DeviceCapacity)
    ''' </summary>
    Public Property DeviceCapacity As Byte
        Get
            Return Read(&H14)
        End Get
        Set(value As Byte)
            Write(&H14, value)
        End Set
    End Property

    'Reserved: 8 bytes of 0

    'Region: 1 byte

    Public Property RomVersion As Byte
        Get
            Return Read(&H1E)
        End Get
        Set(value As Byte)
            Write(&H1E, value)
        End Set
    End Property

    'Autostart: bit 2 skips menu

    Private Property Arm9RomOffset As Integer
        Get
            Return BitConverter.ToInt32(Read(&H20, 4), 0)
        End Get
        Set(value As Integer)
            Write(&H20, 4, BitConverter.GetBytes(value))
        End Set
    End Property
    Private Property Arm9REntryAddress As Integer
        Get
            Return BitConverter.ToInt32(Read(&H24, 4), 0)
        End Get
        Set(value As Integer)
            Write(&H24, 4, BitConverter.GetBytes(value))
        End Set
    End Property
    Private Property Arm9RamAddress As Integer
        Get
            Return BitConverter.ToInt32(Read(&H28, 4), 0)
        End Get
        Set(value As Integer)
            Write(&H28, 4, BitConverter.GetBytes(value))
        End Set
    End Property
    Private Property Arm9Size As Integer
        Get
            Return BitConverter.ToInt32(Read(&H2C, 4), 0)
        End Get
        Set(value As Integer)
            Write(&H2C, 4, BitConverter.GetBytes(value))
        End Set
    End Property
    Private Property Arm7RomOffset As Integer
        Get
            Return BitConverter.ToInt32(Read(&H30, 4), 0)
        End Get
        Set(value As Integer)
            Write(&H30, 4, BitConverter.GetBytes(value))
        End Set
    End Property
    Private Property Arm7REntryAddress As Integer
        Get
            Return BitConverter.ToInt32(Read(&H34, 4), 0)
        End Get
        Set(value As Integer)
            Write(&H34, 4, BitConverter.GetBytes(value))
        End Set
    End Property
    Private Property Arm7RamAddress As Integer
        Get
            Return BitConverter.ToInt32(Read(&H38, 4), 0)
        End Get
        Set(value As Integer)
            Write(&H38, 4, BitConverter.GetBytes(value))
        End Set
    End Property
    Private Property Arm7Size As Integer
        Get
            Return BitConverter.ToInt32(Read(&H3C, 4), 0)
        End Get
        Set(value As Integer)
            Write(&H3C, 4, BitConverter.GetBytes(value))
        End Set
    End Property
    Private Property FilenameTableOffset As Integer
        Get
            Return BitConverter.ToInt32(Read(&H40, 4), 0)
        End Get
        Set(value As Integer)
            Write(&H40, 4, BitConverter.GetBytes(value))
        End Set
    End Property
    Private Property FilenameTableSize As Integer
        Get
            Return BitConverter.ToInt32(Read(&H44, 4), 0)
        End Get
        Set(value As Integer)
            Write(&H44, 4, BitConverter.GetBytes(value))
        End Set
    End Property
    Private Property FileAllocationTableOffset As Integer
        Get
            Return BitConverter.ToInt32(Read(&H48, 4), 0)
        End Get
        Set(value As Integer)
            Write(&H48, 4, BitConverter.GetBytes(value))
        End Set
    End Property
    Private Property FileAllocationTableSize As Integer
        Get
            Return BitConverter.ToInt32(Read(&H4C, 4), 0)
        End Get
        Set(value As Integer)
            Write(&H4C, 4, BitConverter.GetBytes(value))
        End Set
    End Property
    Private Property FileArm9OverlayOffset As Integer
        Get
            Return BitConverter.ToInt32(Read(&H50, 4), 0)
        End Get
        Set(value As Integer)
            Write(&H50, 4, BitConverter.GetBytes(value))
        End Set
    End Property
    Private Property FileArm9OverlaySize As Integer
        Get
            Return BitConverter.ToInt32(Read(&H54, 4), 0)
        End Get
        Set(value As Integer)
            Write(&H54, 4, BitConverter.GetBytes(value))
        End Set
    End Property
    Private Property FileArm7OverlayOffset As Integer
        Get
            Return BitConverter.ToInt32(Read(&H58, 4), 0)
        End Get
        Set(value As Integer)
            Write(&H58, 4, BitConverter.GetBytes(value))
        End Set
    End Property
    Private Property FileArm7OverlaySize As Integer
        Get
            Return BitConverter.ToInt32(Read(&H5C, 4), 0)
        End Get
        Set(value As Integer)
            Write(&H5C, 4, BitConverter.GetBytes(value))
        End Set
    End Property
    '060h    4     Port 40001A4h setting for normal commands (usually 00586000h)
    '064h    4     Port 40001A4h setting for KEY1 commands   (usually 001808F8h)
    Private Property IconTitleOffset As Integer
        Get
            Return BitConverter.ToInt32(Read(&H68, 4), 0)
        End Get
        Set(value As Integer)
            Write(&H68, 4, BitConverter.GetBytes(value))
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

    ''' <summary>
    ''' The ROM's filename table
    ''' </summary>
    Private Property CurrentFilenameTable As FilenameTable

    Private Property CurrentFileAllocationTable As List(Of FileAllocationEntry)

    Private Property CurrentArm9OverlayTable As List(Of OverlayTableEntry)

    Private Property CurrentArm7OverlayTable As List(Of OverlayTableEntry)
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
            out.Add(New OverlayTableEntry(Read(count, 32)))
        Next
        Return out
    End Function

    Private Function ParseArm7OverlayTable() As List(Of OverlayTableEntry)
        Dim out As New List(Of OverlayTableEntry)
        For count = FileArm7OverlayOffset To FileArm7OverlayOffset + FileArm7OverlaySize - 1 Step 32
            out.Add(New OverlayTableEntry(Read(count, 32)))
        Next
        Return out
    End Function

    Private Async Function GetFAT() As Task(Of List(Of FileAllocationEntry))
        Dim out As New ConcurrentDictionary(Of Integer, FileAllocationEntry)
        Dim f As New AsyncFor
        Await f.RunFor(Async Function(count As Integer) As Task
                           Dim offset = FileAllocationTableOffset + count * 8
                           Dim entry As New FileAllocationEntry(BitConverter.ToUInt32(Await ReadAsync(offset, 4), 0), BitConverter.ToUInt32(Await ReadAsync(offset + 4, 4), 0))
                           If Not entry.Offset = 0 Then
                               out(count) = entry
                           End If
                       End Function, 0, FileAllocationTableSize / 8 - 1)
        Return out.Keys.OrderBy(Function(x) x).Select(Function(x) out(x)).ToList()
    End Function

    Private Function GetFNT() As FilenameTable
        Dim root As New DirectoryMainTable(Read(Me.FilenameTableOffset, 8))
        Dim rootDirectories As New List(Of DirectoryMainTable)
        'In the root entry, ParentDir means number of directories
        For count = 8 To root.SubTableOffset - 1 Step 8
            rootDirectories.Add(New DirectoryMainTable(Read(Me.FilenameTableOffset + count, 8)))
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
        Dim length As Integer = Read(offset)
        While length > 0
            If length > 128 Then
                'Then it's a sub directory
                'Read the string
                Dim buffer As Byte() = Read(offset + 1, length - 128)
                Dim s = (New ASCIIEncoding).GetString(buffer)
                'Read sub directory ID
                Dim subDirID As UInt16 = Me.ReadUInt16(offset + 1 + length - 128)
                'Add the result to the list
                subTables.Add(New FNTSubTable With {.Length = length, .Name = s, .SubDirectoryID = subDirID})
                'Increment the offset
                offset += length - 128 + 1 + 2
            ElseIf length < 128 Then
                'Then it's a file
                'Read the string
                Dim buffer As Byte() = Read(offset + 1, length)
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

            length = Read(offset)
        End While
        Return subTables
    End Function

    ''' <summary>
    ''' Extracts the files contained within the ROMs.
    ''' Extractions either run synchronously or asynchrounously, depending on the value of IsThreadSafe.
    ''' </summary>
    ''' <param name="TargetDir">Directory to store the extracted files.</param>
    Public Async Function Unpack(TargetDir As String, Provider As IIOProvider) As Task
        Dim fat = CurrentFileAllocationTable

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
                                                 Provider.WriteAllBytes(Path.Combine(TargetDir, "header.bin"), Read(0, &H200))
                                             End Sub))
        If Me.IsThreadSafe Then
            ExtractionTasks.Add(headerTask)
        Else
            Await headerTask
        End If

        '-Arm9
        Dim arm9Task = Task.Run(New Action(Sub()
                                               Dim arm9buffer As New List(Of Byte)
                                               arm9buffer.AddRange(Read(Me.Arm9RomOffset, Me.Arm9Size))

                                               'Write an additional 0xC bytes if the next 4 equal: 21 06 C0 DE
                                               Dim footer As Int32 = ReadInt32(Me.Arm9RomOffset + Me.Arm9Size)
                                               If footer = &HDEC00621 Then
                                                   arm9buffer.AddRange(Read(Me.Arm9RomOffset + Me.Arm9Size, &HC))
                                               End If

                                               Provider.WriteAllBytes(Path.Combine(TargetDir, "arm9.bin"), arm9buffer.ToArray)
                                           End Sub))
        If Me.IsThreadSafe Then
            ExtractionTasks.Add(arm9Task)
        Else
            Await arm9Task
        End If

        '-Arm7
        Dim arm7Task = Task.Run(New Action(Sub()
                                               Provider.WriteAllBytes(Path.Combine(TargetDir, "arm7.bin"), Read(Me.Arm7RomOffset, Me.Arm7Size))
                                           End Sub))
        If Me.IsThreadSafe Then
            ExtractionTasks.Add(arm7Task)
        Else
            Await arm7Task
        End If

        '-Arm9 overlay table (y9.bin)
        Dim y9Task = Task.Run(New Action(Sub()
                                             Provider.WriteAllBytes(Path.Combine(TargetDir, "y9.bin"), Read(Me.FileArm9OverlayOffset, Me.FileArm9OverlaySize))
                                         End Sub))

        If Me.IsThreadSafe Then
            ExtractionTasks.Add(y9Task)
        Else
            Await y9Task
        End If

        '-Extract arm7 overlay table (y7.bin)
        Dim y7Task = Task.Run(New Action(Sub()
                                             Provider.WriteAllBytes(Path.Combine(TargetDir, "y7.bin"), Read(Me.FileArm7OverlayOffset, Me.FileArm7OverlaySize))
                                         End Sub))

        If Me.IsThreadSafe Then
            ExtractionTasks.Add(y7Task)
        Else
            Await y7Task
        End If
        '-Extract overlays
        Dim overlay9 = ExtractOverlay(fat, ParseArm9OverlayTable, Path.Combine(TargetDir, "overlay"), Provider)

        If Me.IsThreadSafe Then
            ExtractionTasks.Add(overlay9)
        Else
            Await overlay9
        End If

        Dim overlay7 = ExtractOverlay(fat, ParseArm7OverlayTable, Path.Combine(TargetDir, "overlay7"), Provider)

        If Me.IsThreadSafe Then
            ExtractionTasks.Add(overlay7)
        Else
            Await overlay7
        End If
        '-Extract icon (banner.bin)
        Dim iconTask = Task.Run(Sub()
                                    If IconTitleOffset > 0 Then '0 means none
                                        Provider.WriteAllBytes(Path.Combine(TargetDir, "banner.bin"), Read(IconTitleOffset, IconTitleLength))
                                    End If
                                End Sub)

        If Me.IsThreadSafe Then
            ExtractionTasks.Add(iconTask)
        Else
            Await iconTask
        End If

        '- Extract files
        Dim filesExtraction = ExtractFiles(fat, CurrentFilenameTable, TargetDir, Provider)
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
        Await (f.RunForEach(root.Children,
                           Async Function(item As FilenameTable) As Task
                               If item.IsDirectory Then
                                   Await ExtractFiles(fat, item, dest, provider)
                               Else
                                   Dim entry = fat(item.FileIndex)
                                   Dim parentDir = IO.Path.GetDirectoryName(IO.Path.Combine(dest, item.Name))
                                   If Not provider.DirectoryExists(parentDir) Then
                                       provider.CreateDirectory(parentDir)
                                   End If
                                   provider.WriteAllBytes(IO.Path.Combine(dest, item.Name), Await ReadAsync(entry.Offset, entry.EndAddress - entry.Offset))
                                   Threading.Interlocked.Increment(CurrentExtractProgress)
                               End If
                           End Function))
    End Function

    Private Async Function ExtractOverlay(FAT As List(Of FileAllocationEntry), overlayTable As List(Of OverlayTableEntry), targetDir As String, provider As IIOProvider) As Task
        If overlayTable.Count > 0 AndAlso Not provider.DirectoryExists(targetDir) Then
            provider.CreateDirectory(targetDir)
        End If
        Dim f As New AsyncFor
        f.RunSynchronously = Not Me.IsThreadSafe
        f.BatchSize = overlayTable.Count
        Await f.RunForEach(overlayTable,
                           Sub(item As OverlayTableEntry)
                               Dim dest = IO.Path.Combine(targetDir, "overlay_" & item.FileID.ToString.PadLeft(4, "0"c) & ".bin")
                               Dim entry = FAT(item.FileID)
                               provider.WriteAllBytes(dest, Read(entry.Offset, entry.EndAddress - entry.Offset))
                           End Sub)
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

    Public Overridable Async Function IsFileOfType(file As GenericFile) As Task(Of Boolean) ' Implements IDetectableFileType.IsOfType
        Return file.Length > &H15D AndAlso Await file.ReadAsync(&H15C) = &H56 AndAlso Await file.ReadAsync(&H15D) = &HCF
    End Function

#Region "IO Provider Implementation"

    Public Property WorkingDirectory() As String Implements IIOProvider.WorkingDirectory
        Get
            Return _workingDirectory
        End Get
        Set
            If Path.IsPathRooted(Value) Then
                _workingDirectory = Value
            Else
                For Each part In Value.Replace("\"c, "/"c).Split("/"c)
                    If part = "." Then
                        ' Do nothing
                    ElseIf part = ".." Then
                        _workingDirectory = Path.GetDirectoryName(_workingDirectory)
                    Else
                        _workingDirectory = Path.Combine(_workingDirectory, part)
                    End If
                Next
            End If
        End Set
    End Property
    Private _workingDirectory As String

    Public Sub ResetWorkingDirectory() Implements IIOProvider.ResetWorkingDirectory
        WorkingDirectory = "/"
    End Sub

    Protected Function FixPath(pathToFix As String) As String
        Dim fixed = pathToFix.Replace("\", "/")

        'Apply working directory
        If Path.IsPathRooted(pathToFix) Then
            Return fixed
        Else
            Return Path.Combine(WorkingDirectory, fixed)
        End If
    End Function

    Private Function GetFATEntry(path As String, Optional throwIfNotFound As Boolean = True) As FileAllocationEntry
        Dim fixedPath = FixPath(path)
        Dim parts = fixedPath.Split("/")
        Dim currentEntry As FilenameTable = Nothing

        If parts.Length < 2 Then
            Throw New ArgumentException(String.Format(My.Resources.Language.ErrorInvalidPathFormat, path), NameOf(path))
        End If

        Select Case parts(1).ToLower
            Case "data"
                currentEntry = CurrentFilenameTable
            Case "overlay"
                Dim index As Integer
                If Integer.TryParse(parts(2).ToLower.Substring(8, 4), index) Then
                    Return CurrentFileAllocationTable(CurrentArm9OverlayTable(index).FileID)
                End If
            Case "overlay7"
                Dim index As Integer
                If Integer.TryParse(parts(2).ToLower.Substring(8, 4), index) Then
                    Return CurrentFileAllocationTable(CurrentArm7OverlayTable(index).FileID)
                End If
            Case "arm7.bin"
                Return New FileAllocationEntry(Arm7RomOffset, Arm7RomOffset + Arm7Size)
            Case "arm9.bin"
                'Write an additional 0xC bytes if the next 4 equal: 21 06 C0 DE
                Dim footer As Int32 = ReadInt32(Me.Arm9RomOffset + Me.Arm9Size)
                If footer = &HDEC00621 Then
                    Return New FileAllocationEntry(Arm9RomOffset, Arm9RomOffset + Arm9Size + &HC)
                Else
                    Return New FileAllocationEntry(Arm9RomOffset, Arm9RomOffset + Arm9Size)
                End If
            Case "header.bin"
                Return New FileAllocationEntry(0, &H200)
            Case "icon.bin"
                Return New FileAllocationEntry(IconTitleOffset, IconTitleOffset + IconTitleLength)
            Case "y7.bin"
                Return New FileAllocationEntry(FileArm7OverlayOffset, FileArm7OverlayOffset + FileArm7OverlaySize)
            Case "y9.bin"
                Return New FileAllocationEntry(FileArm9OverlayOffset, FileArm9OverlayOffset + FileArm9OverlaySize)
            Case Else
                currentEntry = Nothing 'Throw FileNotFound
        End Select

        If currentEntry IsNot Nothing Then
            For count = 2 To parts.Length - 1
                Dim currentCount = count
                currentEntry = currentEntry?.Children.Where(Function(x) x.Name.ToLower = parts(currentCount)).FirstOrDefault
            Next
        End If

        If currentEntry Is Nothing Then
            If throwIfNotFound Then
                Throw New FileNotFoundException(My.Resources.Language.ErrorROMFileNotFound, path)
            Else
                Return Nothing
            End If
        Else
            Return CurrentFileAllocationTable(currentEntry.FileIndex)
        End If
    End Function

    Public Function GetFileLength(filename As String) As Long Implements IIOProvider.GetFileLength
        Dim entry = GetFATEntry(filename)
        Return entry.EndAddress - entry.Offset
    End Function

    Public Function FileExists(filename As String) As Boolean Implements IIOProvider.FileExists
        Return GetFATEntry(filename) IsNot Nothing
    End Function

    Public Function DirectoryExists(path As String) As Boolean Implements IIOProvider.DirectoryExists
        Throw New NotImplementedException()
    End Function

    Public Sub CreateDirectory(path As String) Implements IIOProvider.CreateDirectory
        Throw New NotImplementedException()
    End Sub

    Public Function GetFiles(path As String, searchPattern As String, topDirectoryOnly As Boolean) As String() Implements IIOProvider.GetFiles
        Throw New NotImplementedException()
    End Function

    Public Function GetDirectories(path As String, topDirectoryOnly As Boolean) As String() Implements IIOProvider.GetDirectories
        Throw New NotImplementedException()
    End Function

    Public Function ReadAllBytes(filename As String) As Byte() Implements IIOProvider.ReadAllBytes
        Dim entry = GetFATEntry(filename)
        Return Read(entry.Offset, entry.EndAddress - entry.Offset)
    End Function

    Public Function ReadAllText(filename As String) As String Implements IIOProvider.ReadAllText
        Throw New NotImplementedException()
    End Function

    Public Sub WriteAllBytes(filename As String, data() As Byte) Implements IIOProvider.WriteAllBytes
        Throw New NotImplementedException()
    End Sub

    Public Sub WriteAllText(filename As String, data As String) Implements IIOProvider.WriteAllText
        Throw New NotImplementedException()
    End Sub

    Public Sub CopyFile(sourceFilename As String, destinationFilename As String) Implements IIOProvider.CopyFile
        Throw New NotImplementedException()
    End Sub

    Public Sub DeleteFile(filename As String) Implements IIOProvider.DeleteFile
        Throw New NotImplementedException()
    End Sub

    Public Sub DeleteDirectory(path As String) Implements IIOProvider.DeleteDirectory
        Throw New NotImplementedException()
    End Sub

    Public Function GetTempFilename() As String Implements IIOProvider.GetTempFilename
        Throw New NotImplementedException()
    End Function

    Public Function GetTempDirectory() As String Implements IIOProvider.GetTempDirectory
        Throw New NotImplementedException()
    End Function

    Public Function OpenFileReadWrite(filename As String) As Stream Implements IIOProvider.OpenFile
        Throw New NotImplementedException()
    End Function

    Public Function OpenFileReadOnly(filename As String) As Stream Implements IIOProvider.OpenFileReadOnly
        Throw New NotImplementedException()
    End Function

    Public Function OpenFileWriteOnly(filename As String) As Stream Implements IIOProvider.OpenFileWriteOnly
        Throw New NotImplementedException()
    End Function

#End Region
End Class
