Imports System.IO

Public Class Converter
    Implements IDisposable

    Private Async Function RunProgram(program As String, arguments As String) As Task
        Dim p As New Process
        p.StartInfo.FileName = program
        p.StartInfo.WorkingDirectory = Path.GetDirectoryName(program)
        p.StartInfo.Arguments = arguments
        p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden
        p.StartInfo.RedirectStandardOutput = True
        p.StartInfo.RedirectStandardError = True
        p.StartInfo.UseShellExecute = False

        AddHandler p.OutputDataReceived, AddressOf OnInputRecieved
        AddHandler p.ErrorDataReceived, AddressOf OnInputRecieved

        p.Start()

        Await Task.Run(Sub() p.WaitForExit())

        RemoveHandler p.OutputDataReceived, AddressOf OnInputRecieved
        RemoveHandler p.ErrorDataReceived, AddressOf OnInputRecieved
    End Function

    Private Sub OnInputRecieved(sender As Object, e As DataReceivedEventArgs)
        If TypeOf sender Is Process Then
            Console.Write($"[{Path.GetFileNameWithoutExtension(DirectCast(sender, Process).StartInfo.FileName)}] ")
            Console.WriteLine(e.Data)
        End If
    End Sub

#Region "Tool Management"
    Private Property ToolDirectory As String
    Private Property Path_3dstool As String
    Private Property Path_3dsbuilder As String
    Private Property Path_makerom As String

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

    Private Sub DeleteTools()
        If Directory.Exists(ToolDirectory) Then
            Directory.Delete(ToolDirectory, True)
        End If
    End Sub
#End Region

#Region "Extraction"
    Private Async Function ExtractPartitions(options As ExtractionOptions) As Task
        Dim headerNcchPath As String = Path.Combine(options.DestinationDirectory, options.RootHeaderName)
        Await RunProgram(Path_3dstool, $"-xtf 3ds ""{options.SourceRom}"" --header ""{headerNcchPath}"" -0 DecryptedPartition0.bin -1 DecryptedPartition1.bin -2 DecryptedPartition2.bin -6 DecryptedPartition6.bin -7 DecryptedPartition7.bin")
    End Function

    Private Async Function ExtractPartition0(options As ExtractionOptions, partitionFilename As String) As Task
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
        tasks.Add(RunProgram(Path_3dstool, $"-xtf romfs DecryptedRomFS.bin --romfs-dir ""{romfsDir}"""))

        '- exefs
        Dim exefsExtractionOptions As String
        'If options.DecompressCodeBin Then
        exefsExtractionOptions = "-xutf"
        'Else
        '    exefsExtractionOptions = "-xtf"
        'End If

        tasks.Add(Task.Run(Async Function() As Task
                               '- exefs
                               Await RunProgram(Path_3dstool, $"{exefsExtractionOptions} exefs DecryptedExeFS.bin --exefs-dir ""{exefsDir}"" --header ""{exefsHeaderPath}""")

                               File.Move(Path.Combine(options.DestinationDirectory, options.ExeFSDirName, "banner.bnr"), Path.Combine(options.DestinationDirectory, options.ExeFSDirName, "banner.bin"))
                               File.Move(Path.Combine(options.DestinationDirectory, options.ExeFSDirName, "icon.icn"), Path.Combine(options.DestinationDirectory, options.ExeFSDirName, "icon.bin"))

                               '- banner
                               Await RunProgram(Path_3dstool, $"-x -t banner -f ""{Path.Combine(options.DestinationDirectory, options.ExeFSDirName, "banner.bin")}"" --banner-dir ""{Path.Combine(options.DestinationDirectory, "ExtractedBanner")}""")

                               File.Move(Path.Combine(options.DestinationDirectory, "ExtractedBanner", "banner0.bcmdl"), Path.Combine(options.DestinationDirectory, "ExtractedBanner", "banner.cgfx"))
                           End Function))

        'Cleanup while we're waiting
        File.Delete(Path.Combine(ToolDirectory, "DecryptedPartition0.bin"))

        'Wait for all extractions
        Await Task.WhenAll(tasks)

        'Cleanup the rest
        File.Delete(Path.Combine(ToolDirectory, "DecryptedRomFS.bin"))
        File.Delete(Path.Combine(ToolDirectory, "DecryptedExeFS.bin"))
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

#Region "Building"
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
        Await RunProgram(Path_3dstool, $" -ctf romfs CustomRomFS.bin --romfs-dir ""{romfsDir}""")
    End Function

    Private Async Function BuildExeFS(options As BuildOptions) As Task
        File.Move(Path.Combine(options.SourceDirectory, options.ExeFSDirName, "banner.bin"), Path.Combine(options.SourceDirectory, options.ExeFSDirName, "banner.bnr"))
        File.Move(Path.Combine(options.SourceDirectory, options.ExeFSDirName, "icon.bin"), Path.Combine(options.SourceDirectory, options.ExeFSDirName, "icon.icn"))

        If options.CompressCodeBin Then
            Throw New NotImplementedException
            '"3dstool -zvf code-patched.bin --compress-type blz --compress-out exefs/code.bin"
        End If

        Dim headerPath As String = Path.Combine(options.SourceDirectory, options.ExeFSHeaderName)
        Dim exefsPath As String = Path.Combine(options.SourceDirectory, options.ExeFSDirName)
        Await RunProgram(Path_3dstool, $"-ctf exefs CustomExeFS.bin --exefs-dir ""{exefsPath}"" --header ""{headerPath}""")

        File.Move(Path.Combine(options.SourceDirectory, options.ExeFSDirName, "banner.bnr"), Path.Combine(options.SourceDirectory, options.ExeFSDirName, "banner.bin"))
        File.Move(Path.Combine(options.SourceDirectory, options.ExeFSDirName, "icon.icn"), Path.Combine(options.SourceDirectory, options.ExeFSDirName, "icon.bin"))
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


#End Region

    ''' <summary>
    ''' Extracts a CCI ROM.
    ''' </summary>
    ''' <param name="filename">Full path of the ROM to extract.</param>
    ''' <param name="outputDirectory">Directory into which to extract the files.</param>
    Public Async Function ExtractCCI(filename As String, outputDirectory As String) As Task
        Dim options As New ExtractionOptions
        options.SourceRom = filename
        options.DestinationDirectory = outputDirectory
        Await ExtractCCI(options)
    End Function

    ''' <summary>
    ''' Extracts a CCI ROM.
    ''' </summary>
    Public Async Function ExtractCCI(options As ExtractionOptions) As Task
        Copy3DSTool()

        If Directory.Exists(options.DestinationDirectory) Then
            Directory.Delete(options.DestinationDirectory, True)
        End If
        Directory.CreateDirectory(options.DestinationDirectory)

        Await ExtractPartitions(options)

        Dim partitionExtractions As New List(Of Task)
        partitionExtractions.Add(ExtractPartition0(options, "DecryptedPartition0.bin"))
        partitionExtractions.Add(ExtractPartition1(options))
        partitionExtractions.Add(ExtractPartition2(options))
        partitionExtractions.Add(ExtractPartition6(options))
        partitionExtractions.Add(ExtractPartition7(options))
        Await Task.WhenAll(partitionExtractions)
    End Function

    ''' <summary>
    ''' Extracts a CXI partition.
    ''' </summary>
    Public Async Function ExtractCXI(options As ExtractionOptions) As Task
        Copy3DSTool()

        If Directory.Exists(options.DestinationDirectory) Then
            Directory.Delete(options.DestinationDirectory, True)
        End If
        Directory.CreateDirectory(options.DestinationDirectory)

        'Extract partition 0, which is the only partition we have
        Await ExtractPartition0(options, options.SourceRom)
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

    End Function

    ''' <summary>
    ''' Builds a decrypted CCI/3DS file, for use with Citra or Decrypt9
    ''' </summary>
    ''' <param name="options"></param>
    ''' <returns></returns>
    Public Async Function Build3DSDecrypted(options As BuildOptions) As Task
        UpdateExheader(options, False)

        Dim headerPath As String = Path.Combine(options.SourceDirectory, options.RootHeaderName)
        Dim outputPath As String = Path.Combine(options.SourceDirectory, options.DestinationROM)
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
                Dim num = item.ToString
                partitionArgs &= $" -{num} CustomPartition{num}.bin"
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
    End Function

    ''' <summary>
    ''' Builds a CCI/3DS file encrypted with a 0-key, for use with Gateway.  Excludes update partitions, download play, and manual.
    ''' </summary>
    ''' <param name="options"></param>
    ''' <returns></returns>
    Public Async Function Build3DS0Key(options As BuildOptions) As Task
        Copy3DSBuilder()

        UpdateExheader(options, False)

        Dim exHeader As String = Path.Combine(options.SourceDirectory, options.ExheaderName)
        Dim exeFS As String = Path.Combine(options.SourceDirectory, options.ExeFSDirName)
        Dim romFS As String = Path.Combine(options.SourceDirectory, options.RomFSDirName)

        If options.CompressCodeBin Then
            Await RunProgram(Path_3dsbuilder, $"""{exeFS}"" ""{romFS}"" ""{exHeader}"" ""{options.DestinationROM}""-compressCode")
            Console.WriteLine("WARNING: .code.bin is still compressed, and other operations may be affected.")
        Else
            Await RunProgram(Path_3dsbuilder, $"""{exeFS}"" ""{romFS}"" ""{exHeader}"" ""{options.DestinationROM}""")
            Dim dotCodeBin = Path.Combine(options.SourceDirectory, options.ExeFSDirName, ".code.bin")
            Dim codeBin = Path.Combine(options.SourceDirectory, options.ExeFSDirName, "code.bin")
            If File.Exists(dotCodeBin) Then
                File.Move(dotCodeBin, codeBin)
            End If
        End If
    End Function

    Public Async Function BuildCia(options As BuildOptions) As Task
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
        Dim outputPath As String = Path.Combine(options.SourceDirectory, options.DestinationROM)

        Await RunProgram(Path_makerom, $"-f cia -o ""{outputPath}""{partitionArgs}")

        'Cleanup
        For Each item In {0, 1, 2, 6, 7}
            Dim partition = Path.Combine(ToolDirectory, "CustomPartition" & item.ToString & ".bin")
            If File.Exists(partition) Then
                File.Delete(partition)
            End If
        Next
    End Function


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
