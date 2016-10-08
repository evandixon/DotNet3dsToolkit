Imports System.IO
Imports System.Reflection

Module Module1

    Sub Main()
        Try
            Console.WriteLine(".Net 3DS Toolkit v{0}", Assembly.GetExecutingAssembly.GetName.Version.ToString(3))

            Dim args = Environment.GetCommandLineArgs
            If args.Length < 3 Then
                Console.WriteLine("Usage: ToolkitConsole.exe <source> <destination> [-key0]")
                Console.WriteLine("<source> can either be a decrypted CCI/3DS ROM, or a directory created by ToolkitConsole.exe")
                Console.WriteLine("<destination> can be a *.3DS, *.3DZ, *.CCI, or *.CIA file, or a directory if the source is a ROM.")
                Console.WriteLine("Output format is detected by the extension.  *.CIA files are outputted as CIA files, *.3DZ files are outputted as 0-key encrypted CCI ROMs, all others are outputted as decrypted CCI ROMs.  Use the -key0 flag to output as a 0-key encrypted CCI ROM instead.")
            Else
                Dim key0 As Boolean = args.Contains("key0")
                Dim source As String = args(1)
                Dim destination As String = args(2)

                If File.Exists(source) Then

                    'Extraction mode
                    Using c As New DotNet3dsToolkit.Converter
                        Dim options As New DotNet3dsToolkit.ExtractionOptions
                        options.SourceRom = source
                        options.DestinationDirectory = destination

                        Console.WriteLine("Extracting to ""{0}""...", destination)

                        c.Extract(options).Wait()

                        Console.WriteLine("Extraction complete!")
                    End Using

                ElseIf Directory.Exists(source) Then
                    'Building mode

                    If key0 Then
                        BuildKey0(source, destination)
                    ElseIf Path.GetExtension(destination).ToLower = "cia" Then
                        BuildCIA(source, destination)
                    ElseIf Path.GetExtension(destination).ToLower = "3dz" Then
                        BuildKey0(source, destination)
                    Else
                        BuildDecryptedCCI(source, destination)
                    End If

                Else
                    Console.WriteLine("Error: The given source is neither a file nor a directory.")
                    Console.WriteLine("Source: ""{0}""", source)
                End If

            End If
        Catch ex As Exception
            Console.WriteLine(ex.ToString)
        End Try
    End Sub

    Private Sub BuildKey0(source As String, destination As String)
        Using c As New DotNet3dsToolkit.Converter
            Dim options As New DotNet3dsToolkit.BuildOptions
            options.SourceDirectory = source
            options.DestinationROM = destination

            Console.WriteLine("Building as key-0 encrypted CCI to ""{0}""...", destination)

            c.Build3DS0Key(options).Wait()

            Console.WriteLine("Build complete!")
        End Using
    End Sub

    Private Sub BuildCIA(source As String, destination As String)
        Using c As New DotNet3dsToolkit.Converter
            Dim options As New DotNet3dsToolkit.BuildOptions
            options.SourceDirectory = source
            options.DestinationROM = destination

            Console.WriteLine("Building as CIA to ""{0}""...", destination)

            c.BuildCia(options).Wait()

            Console.WriteLine("Build complete!")
        End Using
    End Sub

    Private Sub BuildDecryptedCCI(source As String, destination As String)
        Using c As New DotNet3dsToolkit.Converter
            Dim options As New DotNet3dsToolkit.BuildOptions
            options.SourceDirectory = source
            options.DestinationROM = destination

            Console.WriteLine("Building as decrypted CCI to ""{0}""...", destination)

            c.Build3DSDecrypted(options).Wait()

            Console.WriteLine("Build complete!")
        End Using
    End Sub

End Module
