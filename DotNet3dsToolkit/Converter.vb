Imports System.IO
Imports System.Text
Imports System.Text.RegularExpressions
Imports DotNetNdsToolkit
Imports SkyEditor.Core.IO
Imports SkyEditor.Core.Utilities

Public Class Converter
    Implements IDisposable
    Implements IReportProgress

    Public Event ConsoleOutputReceived(sender As Object, e As DataReceivedEventArgs)

    ''' <summary>
    ''' Whether or not to forward console output of child processes to the current process.
    ''' </summary>
    ''' <returns></returns>
    Public Property OutputConsoleOutput As Boolean = True

    Private Async Function RunProgram(program As String, arguments As String) As Task
        Dim handlersRegistered As Boolean = False

        Dim p As New Process
        p.StartInfo.FileName = program
        p.StartInfo.WorkingDirectory = Path.GetDirectoryName(program)
        p.StartInfo.Arguments = arguments
        p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden
        p.StartInfo.CreateNoWindow = True
        p.StartInfo.RedirectStandardOutput = OutputConsoleOutput
        p.StartInfo.RedirectStandardError = p.StartInfo.RedirectStandardOutput
        p.StartInfo.UseShellExecute = False

        If p.StartInfo.RedirectStandardOutput Then
            AddHandler p.OutputDataReceived, AddressOf OnInputRecieved
            AddHandler p.ErrorDataReceived, AddressOf OnInputRecieved
            handlersRegistered = True
        End If

        p.Start()

        p.BeginOutputReadLine()
        p.BeginErrorReadLine()

        Await Task.Run(Sub() p.WaitForExit())

        If handlersRegistered Then
            RemoveHandler p.OutputDataReceived, AddressOf OnInputRecieved
            RemoveHandler p.ErrorDataReceived, AddressOf OnInputRecieved
        End If
    End Function

    Private Sub OnInputRecieved(sender As Object, e As DataReceivedEventArgs)
        If TypeOf sender Is Process AndAlso Not String.IsNullOrEmpty(e.Data) Then
            Console.Write($"[{Path.GetFileNameWithoutExtension(DirectCast(sender, Process).StartInfo.FileName)}] ")
            Console.WriteLine(e.Data)
            RaiseEvent ConsoleOutputReceived(Me, e)
        End If
    End Sub

    Private Sub EnsureInputIsNotInOutputBeforeDeleting(inputFile As String, outputPath As String)
        If Not outputPath.EndsWith("/") Then
            outputPath &= "/"
        End If

        Dim input As New Uri(inputFile)
        Dim output As New Uri(outputPath)
        If output.IsBaseOf(input) Then
            Throw New InputInsideOutputException
        End If
    End Sub

    Public Event UnpackProgressed As EventHandler(Of ProgressReportedEventArgs) Implements IReportProgress.ProgressChanged
    Public Event Completed As EventHandler Implements IReportProgress.Completed

#Region "Tool Management"
    Private Property ToolDirectory As String
    Private Property Path_3dstool As String
    Private Property Path_3dsbuilder As String
    Private Property Path_makerom As String
    Private Property Path_ctrtool As String
    Private Property Path_ndstool As String

    Public Property Progress As Single Implements IReportProgress.Progress
        Get
            Return _progress
        End Get
        Private Set(value As Single)
            If value <> _progress Then
                _progress = value
                RaiseEvent UnpackProgressed(Me, New ProgressReportedEventArgs With {.Progress = Progress, .IsIndeterminate = IsIndeterminate, .Message = Message})
            End If
        End Set
    End Property
    Dim _progress As Single

    Public Property Message As String Implements IReportProgress.Message
        Get
            Return _message
        End Get
        Protected Set(value As String)
            If value <> _message Then
                _message = value
                RaiseEvent UnpackProgressed(Me, New ProgressReportedEventArgs With {.Progress = Progress, .IsIndeterminate = IsIndeterminate, .Message = Message})
            End If
        End Set
    End Property
    Dim _message As String

    Public Property IsIndeterminate As Boolean Implements IReportProgress.IsIndeterminate
        Get
            Return _isIndeterminate
        End Get
        Protected Set(value As Boolean)
            If value <> _isIndeterminate Then
                _isIndeterminate = value
                RaiseEvent UnpackProgressed(Me, New ProgressReportedEventArgs With {.Progress = Progress, .IsIndeterminate = IsIndeterminate, .Message = Message})
            End If
        End Set
    End Property
    Dim _isIndeterminate As Boolean

    Public Property IsCompleted As Boolean Implements IReportProgress.IsCompleted
        Get
            Return _isCompleted
        End Get
        Protected Set(value As Boolean)
            If value <> _isCompleted Then
                _isCompleted = value

                If value Then
                    Progress = 1
                    IsIndeterminate = False
                    RaiseEvent Completed(Me, New EventArgs)
                End If
            End If
        End Set
    End Property
    Dim _isCompleted As Boolean

    Private Sub ResetToolDirectory()
        ToolDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DotNet3DSToolkit-" & Guid.NewGuid.ToString)
        If Directory.Exists(ToolDirectory) Then
            ResetToolDirectory()
        Else
            Directory.CreateDirectory(ToolDirectory)
        End If
    End Sub

    ''' <summary>
    ''' Copies 3dstool.exe to the the tools directory if it's not already there.
    ''' </summary>
    Private Sub Copy3DSTool()
        If String.IsNullOrEmpty(ToolDirectory) Then
            ResetToolDirectory()
        End If

        Dim exePath = Path.Combine(ToolDirectory, "3dstool.exe")
        Dim txtPath = Path.Combine(ToolDirectory, "ignore_3dstool.txt")

        If Not File.Exists(exePath) Then
            File.WriteAllBytes(exePath, My.Resources._3dstool)
            Path_3dstool = exePath
        End If

        If Not File.Exists(txtPath) Then
            File.WriteAllText(Path.Combine(ToolDirectory, "ignore_3dstool.txt"), My.Resources.ignore_3dstool)
        End If
    End Sub

    Private Sub Copy3DSBuilder()
        If String.IsNullOrEmpty(ToolDirectory) Then
            ResetToolDirectory()
        End If

        Dim exePath = Path.Combine(ToolDirectory, "3DS Builder.exe")
        If Not File.Exists(exePath) Then
            File.WriteAllBytes(exePath, My.Resources._3DS_Builder)
            Path_3dsbuilder = exePath
        End If
    End Sub

    Private Sub CopyCtrTool()
        If String.IsNullOrEmpty(ToolDirectory) Then
            ResetToolDirectory()
        End If

        Dim exePath = Path.Combine(ToolDirectory, "ctrtool.exe")
        If Not File.Exists(exePath) Then
            File.WriteAllBytes(exePath, My.Resources.ctrtool)
            Path_ctrtool = exePath
        End If
    End Sub

    Private Sub CopyMakeRom()
        If String.IsNullOrEmpty(ToolDirectory) Then
            ResetToolDirectory()
        End If

        Dim exePath = Path.Combine(ToolDirectory, "makerom.exe")
        If Not File.Exists(exePath) Then
            File.WriteAllBytes(exePath, My.Resources.makerom)
            Path_makerom = exePath
        End If
    End Sub

    Private Sub CopyNDSTool()
        If String.IsNullOrEmpty(ToolDirectory) Then
            ResetToolDirectory()
        End If

        Dim exePath = Path.Combine(ToolDirectory, "ndstool.exe")
        If Not File.Exists(exePath) Then
            File.WriteAllBytes(exePath, My.Resources.ndstool)
            Path_ndstool = exePath
        End If
    End Sub

    Private Sub DeleteTools()
        If Directory.Exists(ToolDirectory) Then
            Directory.Delete(ToolDirectory, True)
        End If
    End Sub
#End Region

#Region "Extraction"
    Public Sub ExtractPrivateHeader(sourceCCI As String, outputFile As String)
        Dim onlineHeaderBinPath = outputFile
        Using f As New FileStream(sourceCCI, FileMode.Open, FileAccess.Read)
            Dim buffer(&H2E00 + 1) As Byte
            f.Seek(&H1200, SeekOrigin.Begin)
            f.Read(buffer, 0, &H2E00)
            File.WriteAllBytes(onlineHeaderBinPath, buffer)
        End Using
    End Sub
    Private Async Function ExtractCCIPartitions(options As ExtractionOptions) As Task
        Dim headerNcchPath As String = Path.Combine(options.DestinationDirectory, options.RootHeaderName)
        Await RunProgram(Path_3dstool, $"-xtf 3ds ""{options.SourceRom}"" --header ""{headerNcchPath}"" -0 DecryptedPartition0.bin -1 DecryptedPartition1.bin -2 DecryptedPartition2.bin -6 DecryptedPartition6.bin -7 DecryptedPartition7.bin")
    End Function

    Private Async Function ExtractCIAPartitions(options As ExtractionOptions) As Task
        Dim headerNcchPath As String = Path.Combine(options.DestinationDirectory, options.RootHeaderName)
        Await RunProgram(Path_ctrtool, $"--content=Partition ""{options.SourceRom}""")

        Dim partitionRegex As New Regex("Partition\.000([0-9])\.[0-9]{8}")
        Dim replace As String = "DecryptedPartition$1.bin"
        For Each item In Directory.GetFiles(ToolDirectory)
            If partitionRegex.IsMatch(item) Then
                File.Move(item, partitionRegex.Replace(item, replace))
            End If
        Next
    End Function

    Private Async Function ExtractPartition0(options As ExtractionOptions, partitionFilename As String, ctrTool As Boolean) As Task
        'Extract partitions
        Dim exheaderPath As String = Path.Combine(options.DestinationDirectory, options.ExheaderName)
        Dim headerPath As String = Path.Combine(options.DestinationDirectory, options.Partition0HeaderName)
        Dim logoPath As String = Path.Combine(options.DestinationDirectory, options.LogoLZName)
        Dim plainPath As String = Path.Combine(options.DestinationDirectory, options.PlainRGNName)
        Await RunProgram(Path_3dstool, $"-xtf cxi ""{partitionFilename}"" --header ""{headerPath}"" --exh ""{exheaderPath}"" --exefs DecryptedExeFS.bin --romfs DecryptedRomFS.bin --logo ""{logoPath}"" --plain ""{plainPath}""")

        'Extract romfs and exefs
        Dim romfsDir As String = Path.Combine(options.DestinationDirectory, options.RomFSDirName)
        Dim exefsDir As String = Path.Combine(options.DestinationDirectory, options.ExeFSDirName)
        Dim exefsHeaderPath As String = Path.Combine(options.DestinationDirectory, options.ExeFSHeaderName)
        Dim tasks As New List(Of Task)

        '- romfs
        If ctrTool Then
            Await RunProgram(Path_ctrtool, $"-t romfs --romfsdir ""{romfsDir}"" DecryptedRomFS.bin")
        Else
            tasks.Add(RunProgram(Path_3dstool, $"-xtf romfs DecryptedRomFS.bin --romfs-dir ""{romfsDir}"""))
        End If

        '- exefs
        Dim exefsExtractionOptions As String
        'If options.DecompressCodeBin Then
        exefsExtractionOptions = "-xutf"
        'Else
        '    exefsExtractionOptions = "-xtf"
        'End If

        If ctrTool Then
            Await RunProgram(Path_ctrtool, $"-t exefs --exefsdir=""{exefsDir}"" DecryptedExeFS.bin --decompresscode")
        Else
            tasks.Add(Task.Run(Async Function() As Task
                                   '- exefs
                                   Await RunProgram(Path_3dstool, $"{exefsExtractionOptions} exefs DecryptedExeFS.bin --exefs-dir ""{exefsDir}"" --header ""{exefsHeaderPath}""")

                                   File.Move(Path.Combine(options.DestinationDirectory, options.ExeFSDirName, "banner.bnr"), Path.Combine(options.DestinationDirectory, options.ExeFSDirName, "banner.bin"))
                                   File.Move(Path.Combine(options.DestinationDirectory, options.ExeFSDirName, "icon.icn"), Path.Combine(options.DestinationDirectory, options.ExeFSDirName, "icon.bin"))

                                   '- banner
                                   Await RunProgram(Path_3dstool, $"-x -t banner -f ""{Path.Combine(options.DestinationDirectory, options.ExeFSDirName, "banner.bin")}"" --banner-dir ""{Path.Combine(options.DestinationDirectory, "ExtractedBanner")}""")

                                   File.Move(Path.Combine(options.DestinationDirectory, "ExtractedBanner", "banner0.bcmdl"), Path.Combine(options.DestinationDirectory, "ExtractedBanner", "banner.cgfx"))
                               End Function))
        End If

        'Cleanup while we're waiting
        File.Delete(Path.Combine(ToolDirectory, "DecryptedPartition0.bin"))

        'Wait for all extractions
        Await Task.WhenAll(tasks)

        'Cleanup the rest
        'File.Delete(Path.Combine(ToolDirectory, "DecryptedRomFS.bin"))
        'File.Delete(Path.Combine(ToolDirectory, "DecryptedExeFS.bin"))
    End Function

    Private Async Function ExtractPartition1(options As ExtractionOptions) As Task
        If File.Exists(Path.Combine(ToolDirectory, "DecryptedPartition1.bin")) Then
            'Extract
            Dim headerPath As String = Path.Combine(options.DestinationDirectory, options.Partition1HeaderName)
            Dim extractedPath As String = Path.Combine(options.DestinationDirectory, options.ExtractedManualDirName)
            Await RunProgram(Path_3dstool, $"-xtf cfa DecryptedPartition1.bin --header ""{headerPath}"" --romfs DecryptedManual.bin")
            Await RunProgram(Path_3dstool, $"-xtf romfs DecryptedManual.bin --romfs-dir ""{extractedPath}""")

            'Cleanup
            File.Delete(Path.Combine(ToolDirectory, "DecryptedPartition1.bin"))
            File.Delete(Path.Combine(ToolDirectory, "DecryptedManual.bin"))
        End If
    End Function

    Private Async Function ExtractPartition2(options As ExtractionOptions) As Task
        If File.Exists(Path.Combine(ToolDirectory, "DecryptedPartition2.bin")) Then
            'Extract
            Dim headerPath As String = Path.Combine(options.DestinationDirectory, options.Partition2HeaderName)
            Dim extractedPath As String = Path.Combine(options.DestinationDirectory, options.ExtractedDownloadPlayDirName)
            Await RunProgram(Path_3dstool, $"-xtf cfa DecryptedPartition2.bin --header ""{headerPath}"" --romfs DecryptedDownloadPlay.bin")
            Await RunProgram(Path_3dstool, $"-xtf romfs DecryptedDownloadPlay.bin --romfs-dir ""{extractedPath}""")

            'Cleanup
            File.Delete(Path.Combine(ToolDirectory, "DecryptedPartition2.bin"))
            File.Delete(Path.Combine(ToolDirectory, "DecryptedDownloadPlay.bin"))
        End If
    End Function

    Private Async Function ExtractPartition6(options As ExtractionOptions) As Task
        If File.Exists(Path.Combine(ToolDirectory, "DecryptedPartition6.bin")) Then
            'Extract
            Dim headerPath As String = Path.Combine(options.DestinationDirectory, options.Partition6HeaderName)
            Dim extractedPath As String = Path.Combine(options.DestinationDirectory, options.N3DSUpdateDirName)
            Await RunProgram(Path_3dstool, $"-xtf cfa DecryptedPartition6.bin --header ""{headerPath}"" --romfs DecryptedN3DSUpdate.bin")
            Await RunProgram(Path_3dstool, $"-xtf romfs DecryptedN3DSUpdate.bin --romfs-dir ""{extractedPath}""")

            'Cleanup
            File.Delete(Path.Combine(ToolDirectory, "DecryptedPartition6.bin"))
            File.Delete(Path.Combine(ToolDirectory, "DecryptedN3DSUpdate.bin"))
        End If
    End Function

    Private Async Function ExtractPartition7(options As ExtractionOptions) As Task
        If File.Exists(Path.Combine(ToolDirectory, "DecryptedPartition7.bin")) Then
            'Extract
            Dim headerPath As String = Path.Combine(options.DestinationDirectory, options.Partition7HeaderName)
            Dim extractedPath As String = Path.Combine(options.DestinationDirectory, options.O3DSUpdateDirName)
            Await RunProgram(Path_3dstool, $"-xtf cfa DecryptedPartition7.bin --header ""{headerPath}"" --romfs DecryptedO3DSUpdate.bin")
            Await RunProgram(Path_3dstool, $"-xtf romfs DecryptedO3DSUpdate.bin --romfs-dir ""{extractedPath}""")

            'Cleanup
            File.Delete(Path.Combine(ToolDirectory, "DecryptedPartition7.bin"))
            File.Delete(Path.Combine(ToolDirectory, "DecryptedO3DSUpdate.bin"))
        End If
    End Function
#End Region

#Region "Building Parts"
    Private Sub UpdateExheader(options As BuildOptions, isCia As Boolean)
        Using f As New FileStream(Path.Combine(options.SourceDirectory, options.ExheaderName), FileMode.Open, FileAccess.ReadWrite)
            f.Seek(&HD, SeekOrigin.Begin)
            Dim sciD = f.ReadByte

            If options.CompressCodeBin Then
                sciD = sciD Or 1     'We want to set bit 1 to 1 to force using a compressed code.bin
            Else
                sciD = sciD And &HFE 'We want to set bit 1 to 0 to avoid using a compressed code.bin
            End If

            If isCia Then
                sciD = sciD Or 2 'Set bit 2 to 1
            Else
                sciD = sciD And &HFD 'Set bit 2 to 0
            End If


            f.Seek(&HD, IO.SeekOrigin.Begin)
            f.WriteByte(sciD)
            f.Flush()
        End Using
    End Sub

    Private Async Function BuildRomFS(options As BuildOptions) As Task
        Dim romfsDir = Path.Combine(options.SourceDirectory, options.RomFSDirName)
        Await BuildRomFS(romfsDir, "CustomRomFS.bin")
    End Function

    Public Async Function BuildRomFS(sourceDirectory As String, outputFile As String) As Task
        Await RunProgram(Path_3dstool, $"-ctf romfs ""{outputFile}"" --romfs-dir ""{sourceDirectory}""")
    End Function

    Private Async Function BuildExeFS(options As BuildOptions) As Task
        Dim bannerBin = Path.Combine(options.SourceDirectory, options.ExeFSDirName, "banner.bin")
        Dim bannerBnr = Path.Combine(options.SourceDirectory, options.ExeFSDirName, "banner.bnr")
        Dim iconBin = Path.Combine(options.SourceDirectory, options.ExeFSDirName, "icon.bin")
        Dim iconIco = Path.Combine(options.SourceDirectory, options.ExeFSDirName, "icon.icn")

        'Rename banner
        If File.Exists(bannerBin) AndAlso Not File.Exists(bannerBnr) Then
            File.Move(bannerBin, bannerBnr)
        ElseIf File.Exists(bannerBin) AndAlso File.Exists(bannerBnr) Then
            File.Delete(bannerBnr)
            File.Move(bannerBin, bannerBnr)
        ElseIf Not File.Exists(bannerBin) AndAlso File.Exists(bannerBnr) Then
            'Do nothing
        Else 'Both files don't exist
            Throw New MissingFileException(bannerBin)
        End If

        'Rename icon
        If File.Exists(iconBin) AndAlso Not File.Exists(iconIco) Then
            File.Move(iconBin, iconIco)
        ElseIf File.Exists(iconBin) AndAlso File.Exists(iconIco) Then
            File.Delete(iconIco)
            File.Move(iconBin, iconIco)
        ElseIf Not File.Exists(iconBin) AndAlso File.Exists(iconIco) Then
            'Do nothing
        Else 'Both files don't exist
            Throw New MissingFileException(iconBin)
        End If

        'Compress code.bin if applicable
        If options.CompressCodeBin Then
            Throw New NotImplementedException
            '"3dstool -zvf code-patched.bin --compress-type blz --compress-out exefs/code.bin"
        End If

        Dim headerPath As String = Path.Combine(options.SourceDirectory, options.ExeFSHeaderName)
        Dim exefsPath As String = Path.Combine(options.SourceDirectory, options.ExeFSDirName)
        Await RunProgram(Path_3dstool, $"-ctf exefs CustomExeFS.bin --exefs-dir ""{exefsPath}"" --header ""{headerPath}""")

        'Rename files back
        File.Move(bannerBnr, bannerBin)
        File.Move(iconIco, iconBin)
    End Function

    Private Async Function BuildPartition0(options As BuildOptions) As Task
        'Build romfs and exefs
        Dim romfsTask = BuildRomFS(options)
        Dim exefsTask = BuildExeFS(options)
        Await romfsTask
        Await exefsTask

        'Build cxi
        Dim exheaderPath As String = Path.Combine(options.SourceDirectory, options.ExheaderName)
        Dim headerPath As String = Path.Combine(options.SourceDirectory, options.Partition0HeaderName)
        Dim logoPath As String = Path.Combine(options.SourceDirectory, options.LogoLZName)
        Dim plainPath As String = Path.Combine(options.SourceDirectory, options.PlainRGNName)
        Await RunProgram(Path_3dstool, $"-ctf cxi CustomPartition0.bin --header ""{headerPath}"" --exh ""{exheaderPath}"" --exefs CustomExeFS.bin --romfs CustomRomFS.bin --logo ""{logoPath}"" --plain ""{plainPath}""")

        'Cleanup
        File.Delete(Path.Combine(ToolDirectory, "CustomExeFS.bin"))
        File.Delete(Path.Combine(ToolDirectory, "CustomRomFS.bin"))
    End Function

    Private Async Function BuildPartition1(options As BuildOptions) As Task
        Dim headerPath As String = Path.Combine(options.SourceDirectory, options.Partition1HeaderName)
        Dim extractedPath As String = Path.Combine(options.SourceDirectory, options.ExtractedManualDirName)
        Await RunProgram(Path_3dstool, $"-ctf romfs CustomManual.bin --romfs-dir ""{extractedPath}""")
        Await RunProgram(Path_3dstool, $"-ctf cfa CustomPartition1.bin --header ""{headerPath}"" --romfs CustomManual.bin")
        File.Delete(Path.Combine(ToolDirectory, "CustomManual.bin"))
    End Function

    Private Async Function BuildPartition2(options As BuildOptions) As Task
        Dim headerPath As String = Path.Combine(options.SourceDirectory, options.Partition2HeaderName)
        Dim extractedPath As String = Path.Combine(options.SourceDirectory, options.ExtractedDownloadPlayDirName)
        Await RunProgram(Path_3dstool, $"-ctf romfs CustomDownloadPlay.bin --romfs-dir ""{extractedPath}""")
        Await RunProgram(Path_3dstool, $"-ctf cfa CustomPartition2.bin --header ""{headerPath}"" --romfs CustomDownloadPlay.bin")
        File.Delete(Path.Combine(ToolDirectory, "CustomDownloadPlay.bin"))
    End Function

    Private Async Function BuildPartition6(options As BuildOptions) As Task
        Dim headerPath As String = Path.Combine(options.SourceDirectory, options.Partition6HeaderName)
        Dim extractedPath As String = Path.Combine(options.SourceDirectory, options.N3DSUpdateDirName)
        Await RunProgram(Path_3dstool, $"-ctf romfs CustomN3DSUpdate.bin --romfs-dir ""{extractedPath}""")
        Await RunProgram(Path_3dstool, $"-ctf cfa CustomPartition6.bin --header ""{headerPath}"" --romfs CustomN3DSUpdate.bin")
        File.Delete(Path.Combine(ToolDirectory, "CustomN3DSUpdate.bin"))
    End Function

    Private Async Function BuildPartition7(options As BuildOptions) As Task
        Dim headerPath As String = Path.Combine(options.SourceDirectory, options.Partition7HeaderName)
        Dim extractedPath As String = Path.Combine(options.SourceDirectory, options.O3DSUpdateDirName)
        Await RunProgram(Path_3dstool, $"-ctf romfs CustomO3DSUpdate.bin --romfs-dir ""{extractedPath}""")
        Await RunProgram(Path_3dstool, $"-ctf cfa CustomPartition7.bin --header ""{headerPath}"" --romfs CustomO3DSUpdate.bin")
        File.Delete(Path.Combine(ToolDirectory, "CustomO3DSUpdate.bin"))
    End Function

    Private Async Function BuildPartitions(options As BuildOptions) As Task
        Copy3DSTool()

        Dim partitionTasks As New List(Of Task)
        partitionTasks.Add(BuildPartition0(options))
        partitionTasks.Add(BuildPartition1(options))
        partitionTasks.Add(BuildPartition2(options))
        partitionTasks.Add(BuildPartition6(options))
        partitionTasks.Add(BuildPartition7(options))
        Await Task.WhenAll(partitionTasks)

        If Not File.Exists(Path.Combine(ToolDirectory, "CustomPartition0.bin")) Then
            Throw New MissingFileException(Path.Combine(ToolDirectory, "CustomPartition0.bin"))
        End If

    End Function


#End Region

#Region "Top Level Public Methods"
    ''' <summary>
    ''' Extracts a decrypted CCI ROM.
    ''' </summary>
    ''' <param name="filename">Full path of the ROM to extract.</param>
    ''' <param name="outputDirectory">Directory into which to extract the files.</param>
    Public Async Function ExtractCCI(filename As String, outputDirectory As String) As Task
        EnsureInputIsNotInOutputBeforeDeleting(filename, outputDirectory)

        Dim options As New ExtractionOptions
        options.SourceRom = filename
        options.DestinationDirectory = outputDirectory
        Await ExtractCCI(options)
    End Function

    ''' <summary>
    ''' Extracts a CCI ROM.
    ''' </summary>
    Public Async Function ExtractCCI(options As ExtractionOptions) As Task
        EnsureInputIsNotInOutputBeforeDeleting(options.SourceRom, options.DestinationDirectory)

        IsIndeterminate = True
        IsCompleted = False

        Copy3DSTool()

        If Not Directory.Exists(options.DestinationDirectory) Then
            Directory.CreateDirectory(options.DestinationDirectory)
        End If

        Await ExtractCCIPartitions(options)

        Dim partitionExtractions As New List(Of Task)
        partitionExtractions.Add(ExtractPartition0(options, "DecryptedPartition0.bin", False))
        partitionExtractions.Add(ExtractPartition1(options))
        partitionExtractions.Add(ExtractPartition2(options))
        partitionExtractions.Add(ExtractPartition6(options))
        partitionExtractions.Add(ExtractPartition7(options))
        Await Task.WhenAll(partitionExtractions)

        IsCompleted = True
    End Function

    ''' <summary>
    ''' Extracts a decrypted CXI ROM.
    ''' </summary>
    ''' <param name="filename">Full path of the ROM to extract.</param>
    ''' <param name="outputDirectory">Directory into which to extract the files.</param>
    Public Async Function ExtractCXI(filename As String, outputDirectory As String) As Task
        EnsureInputIsNotInOutputBeforeDeleting(filename, outputDirectory)

        Dim options As New ExtractionOptions
        options.SourceRom = filename
        options.DestinationDirectory = outputDirectory
        Await ExtractCXI(options)
    End Function

    ''' <summary>
    ''' Extracts a CXI partition.
    ''' </summary>
    Public Async Function ExtractCXI(options As ExtractionOptions) As Task
        EnsureInputIsNotInOutputBeforeDeleting(options.SourceRom, options.DestinationDirectory)

        IsIndeterminate = True
        IsCompleted = False

        Copy3DSTool()
        CopyCtrTool()

        If Not Directory.Exists(options.DestinationDirectory) Then
            Directory.CreateDirectory(options.DestinationDirectory)
        End If

        'Extract partition 0, which is the only partition we have
        Await ExtractPartition0(options, options.SourceRom, True)

        IsCompleted = True
    End Function

    ''' <summary>
    ''' Extracts a decrypted CIA.
    ''' </summary>
    ''' <param name="filename">Full path of the ROM to extract.</param>
    ''' <param name="outputDirectory">Directory into which to extract the files.</param>
    Public Async Function ExtractCIA(filename As String, outputDirectory As String) As Task
        EnsureInputIsNotInOutputBeforeDeleting(filename, outputDirectory)

        Dim options As New ExtractionOptions
        options.SourceRom = filename
        options.DestinationDirectory = outputDirectory
        Await ExtractCIA(options)
    End Function

    ''' <summary>
    ''' Extracts a CIA.
    ''' </summary>
    Public Async Function ExtractCIA(options As ExtractionOptions) As Task
        EnsureInputIsNotInOutputBeforeDeleting(options.SourceRom, options.DestinationDirectory)

        IsIndeterminate = True
        IsCompleted = False

        Copy3DSTool()
        CopyCtrTool()

        If Not Directory.Exists(options.DestinationDirectory) Then
            Directory.CreateDirectory(options.DestinationDirectory)
        End If

        Await ExtractCIAPartitions(options)

        Dim partitionExtractions As New List(Of Task)
        partitionExtractions.Add(ExtractPartition0(options, "DecryptedPartition0.bin", False))
        partitionExtractions.Add(ExtractPartition1(options))
        partitionExtractions.Add(ExtractPartition2(options))
        partitionExtractions.Add(ExtractPartition6(options))
        partitionExtractions.Add(ExtractPartition7(options))
        Await Task.WhenAll(partitionExtractions)

        IsCompleted = True
    End Function

    ''' <summary>
    ''' Extracts an NDS ROM.
    ''' </summary>
    ''' <param name="filename">Full path of the ROM to extract.</param>
    ''' <param name="outputDirectory">Directory into which to extract the files.</param>
    Public Async Function ExtractNDS(filename As String, outputDirectory As String) As Task
        EnsureInputIsNotInOutputBeforeDeleting(filename, outputDirectory)

        Progress = 0
        IsIndeterminate = False
        IsCompleted = False
        Dim reportProgress = Sub(sender As Object, e As ProgressReportedEventArgs)
                                 RaiseEvent UnpackProgressed(Me, e)
                             End Sub

        If Not Directory.Exists(outputDirectory) Then
            Directory.CreateDirectory(outputDirectory)
        End If

        Dim r As New NdsRom
        Dim p As New PhysicalIOProvider

        AddHandler r.ProgressChanged, reportProgress

        Await r.OpenFile(filename, p)
        Await r.Unpack(outputDirectory, p)

        RemoveHandler r.ProgressChanged, reportProgress
        IsCompleted = True
    End Function

    ''' <summary>
    ''' Extracts a decrypted CCI or CXI ROM.
    ''' </summary>
    ''' <param name="filename">Full path of the ROM to extract.</param>
    ''' <param name="outputDirectory">Directory into which to extract the files.</param>
    ''' <remarks>Extraction type is determined by file extension.  Extensions of ".cxi" are extracted as CXI, all others are extracted as CCI.  To override this behavior, use a more specific extraction function.</remarks>
    ''' <exception cref="NotSupportedException">Thrown when the input file is not a supported file.</exception>
    Public Async Function ExtractAuto(filename As String, outputDirectory As String) As Task
        EnsureInputIsNotInOutputBeforeDeleting(filename, outputDirectory)

        Select Case Await MetadataReader.GetROMSystem(filename)
            Case SystemType.NDS
                Await ExtractNDS(filename, outputDirectory)
            Case SystemType.ThreeDS
                Dim e As New ASCIIEncoding
                Using file As New GenericFile
                    file.EnableInMemoryLoad = False
                    file.IsReadOnly = True
                    Await file.OpenFile(filename, New PhysicalIOProvider)
                    If file.Length > 104 AndAlso e.GetString(Await file.ReadAsync(&H100, 4)) = "NCSD" Then
                        'CCI
                        Await ExtractCCI(filename, outputDirectory)
                    ElseIf file.Length > 104 AndAlso e.GetString(Await file.ReadAsync(&H100, 4)) = "NCCH" Then
                        'CXI
                        Await ExtractCXI(filename, outputDirectory)
                    ElseIf file.Length > Await file.ReadInt32Async(0) AndAlso e.GetString(Await file.ReadAsync(&H100 + MetadataReader.GetCIAContentOffset(file), 4)) = "NCCH" Then
                        'CIA
                        Await ExtractCIA(filename, outputDirectory)
                    Else
                        Throw New NotSupportedException(My.Resources.Language.ErrorInvalidFileFormat)
                    End If
                End Using
            Case SystemType.Unknown
                Throw New NotSupportedException(My.Resources.Language.ErrorInvalidFileFormat)
        End Select
    End Function

    ''' <summary>
    ''' Builds a decrypted CCI/3DS file, for use with Citra or Decrypt9
    ''' </summary>
    ''' <param name="options"></param>
    Public Async Function Build3DSDecrypted(options As BuildOptions) As Task
        IsIndeterminate = True
        IsCompleted = False

        UpdateExheader(options, False)

        Dim headerPath As String = Path.Combine(options.SourceDirectory, options.RootHeaderName)
        Dim outputPath As String = Path.Combine(options.SourceDirectory, options.Destination)
        Dim partitionArgs As String = ""

        If Not File.Exists(headerPath) Then
            Throw New IOException($"NCCH header not found.  This can happen if you extracted a CXI and are trying to rebuild a decrypted CCI.  Try building as a key-0 encrypted CCI instead.  Path of missing header: ""{headerPath}"".")
        End If

        Await BuildPartitions(options)

        'Delete partitions that are too small
        For Each item In {0, 1, 2, 6, 7}
            Dim info As New FileInfo(Path.Combine(ToolDirectory, "CustomPartition" & item.ToString & ".bin"))
            If info.Length <= 20000 Then
                File.Delete(info.FullName)
            Else
                If info.Exists Then
                    Dim num = item.ToString
                    partitionArgs &= $" -{num} CustomPartition{num}.bin"
                End If
            End If
        Next

        Await RunProgram(Path_3dstool, $"-ctf 3ds ""{outputPath}"" --header ""{headerPath}""{partitionArgs}")

        'Cleanup
        For Each item In {0, 1, 2, 6, 7}
            Dim partition = Path.Combine(ToolDirectory, "CustomPartition" & item.ToString & ".bin")
            If File.Exists(partition) Then
                File.Delete(partition)
            End If
        Next

        IsCompleted = True
    End Function

    ''' <summary>
    ''' Builds a decrypted CCI from the given files.
    ''' </summary>
    ''' <param name="sourceDirectory">Path of the files to build.  Must have been created with <see cref="ExtractAuto(String, String)"/> or equivalent function using default settings.</param>
    ''' <param name="outputROM">Destination of the output ROM.</param>
    Public Async Function Build3DSDecrypted(sourceDirectory As String, outputROM As String) As Task
        Dim options As New BuildOptions
        options.SourceDirectory = sourceDirectory
        options.Destination = outputROM

        Await Build3DSDecrypted(options)
    End Function

    ''' <summary>
    ''' Builds a CCI/3DS file encrypted with a 0-key, for use with Gateway.  Excludes update partitions, download play, and manual.
    ''' </summary>
    ''' <param name="options"></param>
    Public Async Function Build3DS0Key(options As BuildOptions) As Task
        IsIndeterminate = True
        IsCompleted = False

        Copy3DSBuilder()

        UpdateExheader(options, False)

        Dim exHeader As String = Path.Combine(options.SourceDirectory, options.ExheaderName)
        Dim exeFS As String = Path.Combine(options.SourceDirectory, options.ExeFSDirName)
        Dim romFS As String = Path.Combine(options.SourceDirectory, options.RomFSDirName)

        If options.CompressCodeBin Then
            Await RunProgram(Path_3dsbuilder, $"""{exeFS}"" ""{romFS}"" ""{exHeader}"" ""{options.Destination}""-compressCode")
            Console.WriteLine("WARNING: .code.bin is still compressed, and other operations may be affected.")
        Else
            Await RunProgram(Path_3dsbuilder, $"""{exeFS}"" ""{romFS}"" ""{exHeader}"" ""{options.Destination}""")
            Dim dotCodeBin = Path.Combine(options.SourceDirectory, options.ExeFSDirName, ".code.bin")
            Dim codeBin = Path.Combine(options.SourceDirectory, options.ExeFSDirName, "code.bin")
            If File.Exists(dotCodeBin) Then
                File.Move(dotCodeBin, codeBin)
            End If
        End If

        IsCompleted = True
    End Function

    ''' <summary>
    ''' Builds a 0-key encrypted CCI from the given files.
    ''' </summary>
    ''' <param name="sourceDirectory">Path of the files to build.  Must have been created with <see cref="ExtractAuto(String, String)"/> or equivalent function using default settings.</param>
    ''' <param name="outputROM">Destination of the output ROM.</param>
    Public Async Function Build3DS0Key(sourceDirectory As String, outputROM As String) As Task
        Dim options As New BuildOptions
        options.SourceDirectory = sourceDirectory
        options.Destination = outputROM

        Await Build3DS0Key(options)
    End Function

    ''' <summary>
    ''' Builds a CCI/3DS file encrypted with a 0-key, for use with Gateway.  Excludes update partitions, download play, and manual.
    ''' </summary>
    ''' <param name="options"></param>
    Public Async Function BuildCia(options As BuildOptions) As Task
        IsIndeterminate = True
        IsCompleted = False

        UpdateExheader(options, True)
        CopyMakeRom()
        Await BuildPartitions(options)

        Dim partitionArgs As String = ""

        'Delete partitions that are too small
        For Each item In {0, 1, 2, 6, 7}
            Dim info As New FileInfo(Path.Combine(ToolDirectory, "CustomPartition" & item.ToString & ".bin"))
            If info.Length <= 20000 Then
                File.Delete(info.FullName)
            Else
                Dim num = item.ToString
                partitionArgs &= $" -content CustomPartition{num}.bin:{num}"
            End If
        Next

        Dim headerPath As String = Path.Combine(options.SourceDirectory, options.RootHeaderName)
        Dim outputPath As String = Path.Combine(options.SourceDirectory, options.Destination)

        Await RunProgram(Path_makerom, $"-f cia -o ""{outputPath}""{partitionArgs}")

        'Cleanup
        For Each item In {0, 1, 2, 6, 7}
            Dim partition = Path.Combine(ToolDirectory, "CustomPartition" & item.ToString & ".bin")
            If File.Exists(partition) Then
                File.Delete(partition)
            End If
        Next

        IsCompleted = True
    End Function

    ''' <summary>
    ''' Builds a CIA from the given files.
    ''' </summary>
    ''' <param name="sourceDirectory">Path of the files to build.  Must have been created with <see cref="ExtractAuto(String, String)"/> or equivalent function using default settings.</param>
    ''' <param name="outputROM">Destination of the output ROM.</param>
    Public Async Function BuildCia(sourceDirectory As String, outputROM As String) As Task
        Dim options As New BuildOptions
        options.SourceDirectory = sourceDirectory
        options.Destination = outputROM

        Await BuildCia(options)
    End Function

    ''' <summary>
    ''' Builds an NDS ROM from the given files.
    ''' </summary>
    ''' <param name="sourceDirectory">Path of the files to build.  Must have been created with <see cref="ExtractAuto(String, String)"/> or equivalent function using default settings.</param>
    ''' <param name="outputROM">Destination of the output ROM.</param>
    Public Async Function BuildNDS(sourceDirectory As String, outputROM As String) As Task
        IsIndeterminate = True
        IsCompleted = False

        CopyNDSTool()

        Await RunProgram(Path_ndstool, String.Format("-c ""{0}"" -9 ""{1}/arm9.bin"" -7 ""{1}/arm7.bin"" -y9 ""{1}/y9.bin"" -y7 ""{1}/y7.bin"" -d ""{1}/data"" -y ""{1}/overlay"" -t ""{1}/banner.bin"" -h ""{1}/header.bin""", outputROM, sourceDirectory))

        IsCompleted = True
    End Function

    ''' <summary>
    ''' Builds files for use with HANS.
    ''' </summary>
    ''' <param name="options">Options to use for the build.  <see cref="BuildOptions.Destination"/> should be the SD card root.</param>
    ''' <param name="shortcutName">Name of the shortcut.  Should not contain spaces nor special characters.</param>
    ''' <param name="rawName">Raw name for the destination RomFS and Code files.  Should be short, but the exact requirements are unknown.</param>
    Public Async Function BuildHans(options As BuildOptions, shortcutName As String, rawName As String) As Task
        IsIndeterminate = True
        IsCompleted = False

        'Validate input.  Never trust the user.
        shortcutName = shortcutName.Replace(" ", "").Replace("é", "e")

        Copy3DSTool()

        'Create variables
        Dim romfsDir = Path.Combine(options.SourceDirectory, options.RomFSDirName)
        Dim romfsFile = Path.Combine(ToolDirectory, "romfsRepacked.bin")
        Dim codeFile = Path.Combine(options.SourceDirectory, options.ExeFSDirName, "code.bin")
        Dim smdhSourceFile = Path.Combine(options.SourceDirectory, options.ExeFSDirName, "icon.bin")
        Dim exheaderFile = Path.Combine(options.SourceDirectory, options.ExheaderName)
        Dim titleID As String

        If File.Exists(exheaderFile) Then
            Dim exheader = File.ReadAllBytes(exheaderFile)
            titleID = BitConverter.ToUInt64(exheader, &H200).ToString("X").PadLeft(16, "0"c)
        Else
            Throw New IOException($"Could not find exheader at the path ""{exheaderFile}"".")
        End If

        'Repack romfs
        Await BuildRomFS(romfsDir, romfsFile)

        'Copy the files
        '- Create non-existant directories
        If Not Directory.Exists(options.Destination) Then
            Directory.CreateDirectory(options.Destination)
        End If
        If Not Directory.Exists(Path.Combine(options.Destination, "hans")) Then
            Directory.CreateDirectory(Path.Combine(options.Destination, "hans"))
        End If

        '- Copy files if they exist
        If File.Exists(romfsFile) Then
            File.Copy(romfsFile, IO.Path.Combine(options.Destination, "hans", rawName & ".romfs"), True)
        End If
        If File.Exists(codeFile) Then
            File.Copy(codeFile, IO.Path.Combine(options.Destination, "hans", rawName & ".code"), True)
        End If

        'Create the homebrew launcher shortcut
        If Not Directory.Exists(IO.Path.Combine(options.Destination, "3ds")) Then
            Directory.CreateDirectory(IO.Path.Combine(options.Destination, "3ds"))
        End If

        '- Copy smdh
        Dim iconExists As Boolean = False
        If File.Exists(smdhSourceFile) Then
            iconExists = True
            File.Copy(smdhSourceFile, IO.Path.Combine(options.Destination, "3ds", shortcutName & ".smdh"), True)
        End If

        '- Write hans shortcut
        Dim shortcut As New Text.StringBuilder
        shortcut.AppendLine("<shortcut>")
        shortcut.AppendLine("	<executable>/3ds/hans/hans.3dsx</executable>")
        If iconExists Then
            shortcut.AppendLine($"	<icon>/3ds/{shortcutName}.smdh</icon>")
        End If
        shortcut.AppendLine($"	<arg>-f/3ds/hans/titles/{rawName}.txt</arg>")
        shortcut.AppendLine("</shortcut>")
        shortcut.AppendLine("<targets selectable=""false"">")
        shortcut.AppendLine($"	<title mediatype=""2"">{titleID}</title>")
        shortcut.AppendLine($"	<title mediatype=""1"">{titleID}</title>")
        shortcut.AppendLine("</targets>")
        File.WriteAllText(Path.Combine(options.Destination, "3ds", shortcutName & ".xml"), shortcut.ToString)

        '- Write hans title settings
        Dim preset As New Text.StringBuilder
        preset.Append("region : -1")
        preset.Append(vbLf)
        preset.Append("language : -1")
        preset.Append(vbLf)
        preset.Append("clock : 0")
        preset.Append(vbLf)
        preset.Append("romfs : 0")
        preset.Append(vbLf)
        preset.Append("code : 0")
        preset.Append(vbLf)
        preset.Append("nim_checkupdate : 1")
        preset.Append(vbLf)
        If Not Directory.Exists(Path.Combine(options.Destination, "3ds", "hans", "titles")) Then
            Directory.CreateDirectory(Path.Combine(options.Destination, "3ds", "hans", "titles"))
        End If
        File.WriteAllText(Path.Combine(options.Destination, "3ds", "hans", "titles", rawName & ".txt"), preset.ToString)

        IsCompleted = True
    End Function

    ''' <summary>
    ''' Builds files for use with HANS.
    ''' </summary>
    ''' <param name="sourceDirectory">Path of the files to build.  Must have been created with <see cref="ExtractAuto(String, String)"/> or equivalent function using default settings.</param>
    ''' <param name="sdRoot">Root of the SD card</param>
    ''' <param name="rawName">Raw name for the destination RomFS, Code, and shortcut files.  Should be short, but the exact requirements are unknown.  To use a different name for shortcut files, use <see cref="BuildHans(BuildOptions, String, String)"/>.</param>
    Public Async Function BuildHans(sourceDirectory As String, sdRoot As String, rawName As String) As Task
        Dim options As New BuildOptions
        options.SourceDirectory = sourceDirectory
        options.Destination = sdRoot
        Await BuildHans(options, rawName, rawName)
    End Function

    ''' <summary>
    ''' Builds a ROM from the given files.
    ''' </summary>
    ''' <param name="sourceDirectory">Path of the files to build.  Must have been created with <see cref="ExtractAuto(String, String)"/> or equivalent function using default settings.</param>
    ''' <param name="outputROM">Destination of the output ROM.</param>
    ''' <remarks>Output format is determined by file extension.  Extensions of ".cia" build a CIA, extensions of ".3dz" build a 0-key encrypted CCI, and all others build a decrypted CCI.  To force a different behavior, use a more specific Build function.</remarks>
    Public Async Function BuildAuto(sourceDirectory As String, outputROM As String) As Task
        Dim ext = Path.GetExtension(outputROM).ToLower
        Select Case ext
            Case ".cia"
                Await BuildCia(sourceDirectory, outputROM)
            Case ".3dz"
                Await Build3DS0Key(sourceDirectory, outputROM)
            Case ".nds", ".srl"
                Await BuildNDS(sourceDirectory, outputROM)
            Case Else
                Await Build3DSDecrypted(sourceDirectory, outputROM)
        End Select
    End Function
#End Region

#Region "IDisposable Support"
    Private disposedValue As Boolean ' To detect redundant calls

    ' IDisposable
    Protected Overridable Sub Dispose(disposing As Boolean)
        If Not disposedValue Then
            If disposing Then
                ' TODO: dispose managed state (managed objects).

                DeleteTools()
            End If

            ' TODO: free unmanaged resources (unmanaged objects) and override Finalize() below.
            ' TODO: set large fields to null.
        End If
        disposedValue = True
    End Sub

    ' TODO: override Finalize() only if Dispose(disposing As Boolean) above has code to free unmanaged resources.
    'Protected Overrides Sub Finalize()
    '    ' Do not change this code.  Put cleanup code in Dispose(disposing As Boolean) above.
    '    Dispose(False)
    '    MyBase.Finalize()
    'End Sub

    ' This code added by Visual Basic to correctly implement the disposable pattern.
    Public Sub Dispose() Implements IDisposable.Dispose
        ' Do not change this code.  Put cleanup code in Dispose(disposing As Boolean) above.
        Dispose(True)
        ' TODO: uncomment the following line if Finalize() is overridden above.
        ' GC.SuppressFinalize(Me)
    End Sub
#End Region

End Class
