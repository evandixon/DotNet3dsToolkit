Imports System.IO

Public Class Converter
    Implements IDisposable

    Private Async Function RunProgram(program As String, arguments As String) As Task
        Dim p As New Process
        p.StartInfo.FileName = program
        p.StartInfo.WorkingDirectory = Path.GetDirectoryName(program)
        p.StartInfo.Arguments = arguments
        p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden

        p.Start()

        Await Task.Run(Sub() p.WaitForExit())
    End Function

#Region "Tool Management"
    Private Property ToolDirectory As String
    Private Property Path_3dstool As String

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

    Private Sub DeleteTools()
        If Directory.Exists(ToolDirectory) Then
            Directory.Delete(ToolDirectory, True)
        End If
    End Sub
#End Region

#Region "Extraction"
    Private Async Function ExtractPartitions(options As ExtractionOptions) As Task
        Dim headerNcchPath As String = Path.Combine(options.DestinationDirectory, "HeaderNCCH.bin")
        Await RunProgram(Path_3dstool, $"-xtf 3ds ""{options.SourceRom}"" --header ""{headerNcchPath}"" -0 DecryptedPartition0.bin -1 DecryptedPartition1.bin -2 DecryptedPartition2.bin -6 DecryptedPartition6.bin -7 DecryptedPartition7.bin")
    End Function

    Private Async Function ExtractPartition0(options As ExtractionOptions) As Task
        'Extract partitions
        Dim exheaderPath As String = Path.Combine(options.DestinationDirectory, "DecryptedExHeader.bin")
        Dim headerPath As String = Path.Combine(options.DestinationDirectory, "HeaderNCCH0.bin")
        Dim logoPath As String = Path.Combine(options.DestinationDirectory, "LogoLZ.bin")
        Dim plainPath As String = Path.Combine(options.DestinationDirectory, "PlainRGN.bin")
        Await RunProgram(Path_3dstool, $"-xtf cxi DecryptedPartition0.bin --header ""{headerPath}"" --exh ""{exheaderPath}"" --exefs DecryptedExeFS.bin --romfs DecryptedRomFS.bin --logo ""{logoPath}"" --plain ""{plainPath}""")

        'Extract romfs and exefs
        Dim romfsDir As String = Path.Combine(options.DestinationDirectory, "ExtractedRomFS")
        Dim exefsDir As String = Path.Combine(options.DestinationDirectory, "ExtractedExeFS")
        Dim exefsHeaderPath As String = Path.Combine(options.DestinationDirectory, "HeaderExeFS.bin")
        Dim tasks As New List(Of Task)

        '- romfs
        tasks.Add(RunProgram(Path_3dstool, $"-xtf romfs DecryptedRomFS.bin --romfs-dir ""{romfsDir}"""))

        '- exefs
        Dim exefsExtractionOptions As String
        If options.DecompressCodeBin Then
            exefsExtractionOptions = "-xutf"
        Else
            exefsExtractionOptions = "-xtf"
        End If
        tasks.Add(Task.Run(Async Function() As Task
                               '- exefs
                               Await RunProgram(Path_3dstool, $"{exefsExtractionOptions} exefs DecryptedExeFS.bin --exefs-dir ""{exefsDir}"" --header ""{exefsHeaderPath}""")

                               File.Move(Path.Combine(options.DestinationDirectory, "ExtractedExeFS", "banner.bnr"), Path.Combine(options.DestinationDirectory, "ExtractedExeFS", "banner.bin"))
                               File.Move(Path.Combine(options.DestinationDirectory, "ExtractedExeFS", "icon.icn"), Path.Combine(options.DestinationDirectory, "ExtractedExeFS", "icon.bin"))

                               '- banner
                               Await RunProgram(Path_3dstool, $"-x -t banner -f ""{Path.Combine(options.DestinationDirectory, "ExtractedExeFS", "banner.bin")}"" --banner-dir ""{Path.Combine(options.DestinationDirectory, "ExtractedBanner")}""")

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
            Dim headerPath As String = Path.Combine(options.DestinationDirectory, "HeaderNCCH1.bin")
            Dim extractedPath As String = Path.Combine(options.DestinationDirectory, "ExtractedManual")
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
            Dim headerPath As String = Path.Combine(options.DestinationDirectory, "HeaderNCCH2.bin")
            Dim extractedPath As String = Path.Combine(options.DestinationDirectory, "ExtractedDownloadPlay")
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
            Dim headerPath As String = Path.Combine(options.DestinationDirectory, "HeaderNCCH6.bin")
            Dim extractedPath As String = Path.Combine(options.DestinationDirectory, "ExtractedN3DSUpdate")
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
            Dim headerPath As String = Path.Combine(options.DestinationDirectory, "HeaderNCCH7.bin")
            Dim extractedPath As String = Path.Combine(options.DestinationDirectory, "ExtractedO3DSUpdate")
            Await RunProgram(Path_3dstool, $"-xtf cfa DecryptedPartition7.bin --header ""{headerPath}"" --romfs DecryptedO3DSUpdate.bin")
            Await RunProgram(Path_3dstool, $"-xtf romfs DecryptedO3DSUpdate.bin --romfs-dir ""{extractedPath}""")

            'Cleanup
            File.Delete(Path.Combine(ToolDirectory, "DecryptedPartition7.bin"))
            File.Delete(Path.Combine(ToolDirectory, "DecryptedO3DSUpdate.bin"))
        End If
    End Function
#End Region

    Public Async Function Extract(options As ExtractionOptions) As Task
        Copy3DSTool()

        If Not Directory.Exists(options.DestinationDirectory) Then
            Directory.CreateDirectory(options.DestinationDirectory)
        End If

        Await ExtractPartitions(options)

        Dim partitionExtractions As New List(Of Task)
        partitionExtractions.Add(ExtractPartition0(options))
        partitionExtractions.Add(ExtractPartition1(options))
        partitionExtractions.Add(ExtractPartition2(options))
        partitionExtractions.Add(ExtractPartition6(options))
        partitionExtractions.Add(ExtractPartition7(options))
        Await Task.WhenAll(partitionExtractions)
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
