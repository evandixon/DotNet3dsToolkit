Imports System.IO
Imports System.Reflection

Module Module1

    Sub PrintUsage()
        Console.WriteLine("Usage: ToolkitConsole.exe <source> <destination> [hans raw file name] [-source-cxi|-source-cia|-source-nds] [-key0|-hans]")
        Console.WriteLine("<source> can be a decrypted CCI/3DS ROM, a decrypted CIA, a decrypted CXI, or a directory created by ToolkitConsole.exe.")
        Console.WriteLine("<destination> can be a *.3DS, *.3DZ, *.CCI, *.CIA, *.NDS, or *.SRL file, a directory if the source is a ROM, or the root of your SD card if outputting files for HANS.")
        Console.WriteLine("[hans raw file name] is the future name of the raw files for HANS, if the ""-hans"" argument is present.  Shorter strings work better, but the exact requirements are unknown.")
        Console.WriteLine("Output format is detected by the extension.  *.CIA files are outputted as CIA files, *.3DZ files are outputted as 0-key encrypted CCI ROMs, *.NDS and *.SRL are outputted as NDS ROMs, all others are outputted as decrypted CCI ROMs.  Use the -key0 flag to output as a 0-key encrypted CCI ROM instead.")
        Console.WriteLine("")
        Console.WriteLine("Examples:")
        Console.WriteLine("Extract a CCI: ToolkitConsole.exe MyRom.3ds MyFiles")
        Console.WriteLine("Build a CIA: ToolkitConsole.exe MyFiles MyRom.cia")
        Console.WriteLine("Build files for HANS: ToolkitConsole.exe MyFiles G:/ MyHack")
    End Sub

    Sub Main()
        Dim args = Environment.GetCommandLineArgs
        Try
            Console.WriteLine(".Net 3DS Toolkit v{0}", Assembly.GetExecutingAssembly.GetName.Version.ToString(3))
            If args.Length < 3 Then
                PrintUsage()
            Else
                Dim key0 As Boolean = args.Contains("-key0")
                Dim hans As Boolean = args.Contains("-hans")
                Dim source As String = args(1)
                Dim destination As String = args(2)

                If Not Path.IsPathRooted(source) Then
                    source = Path.Combine(Environment.CurrentDirectory, source)
                End If

                If Not Path.IsPathRooted(destination) Then
                    destination = Path.Combine(Environment.CurrentDirectory, destination)
                End If

                If File.Exists(source) Then

                    'Extraction mode
                    Using c As New DotNet3dsToolkit.Converter
                        If Path.GetExtension(source).ToLower = ".cxi" OrElse args.Contains("-source-cxi") Then
                            Console.WriteLine("Extracting as CXI to ""{0}""...", destination)
                            c.ExtractCXI(source, destination).Wait()
                        ElseIf Path.GetExtension(source).ToLower = ".cia" OrElse args.Contains("-source-cia") Then
                            Console.WriteLine("Extracting as CIA to ""{0}""...", destination)
                            c.ExtractCIA(source, destination).Wait()
                        ElseIf Path.GetExtension(source).ToLower = ".nds" OrElse Path.GetExtension(source).ToLower = ".srl" OrElse args.Contains("-source-nds") Then
                            Console.WriteLine("Extracting as NDS to ""{0}""...", destination)
                            c.ExtractNDS(source, destination).Wait()
                        Else
                            Console.WriteLine("Extracting as CCI to ""{0}""...", destination)
                            c.ExtractCCI(source, destination).Wait()
                        End If

                        Console.WriteLine("Extraction complete!")
                    End Using

                ElseIf Directory.Exists(source) Then
                    'Building mode

                    Using c As New DotNet3dsToolkit.Converter
                        If hans Then
                            If args.Length > 4 Then
                                Console.WriteLine("Building files for HANS...")
                                c.BuildHans(source, destination, args(3)).Wait()
                            Else
                                Console.WriteLine("Invalid usage.")
                                PrintUsage()
                            End If
                        ElseIf key0 Then
                            Console.WriteLine("Building as 0-key encrypted CCI...")
                            c.Build3DS0Key(source, destination).Wait()
                        ElseIf Path.GetExtension(destination).ToLower = ".cia" Then
                            Console.WriteLine("Building as CIA...")
                            c.BuildCia(source, destination).Wait()
                        ElseIf Path.GetExtension(destination).ToLower = ".3dz" Then
                            Console.WriteLine("Building as 0-key encrypted CCI...")
                            c.Build3DS0Key(source, destination).Wait()
                        ElseIf Path.GetExtension(destination).ToLower = ".nds" OrElse Path.GetExtension(destination).ToLower = ".srl" Then
                            Console.WriteLine("Building as NDS...")
                            c.BuildNDS(source, destination).Wait()
                        Else
                            Console.WriteLine("Building as decrypted CCI...")
                            c.Build3DSDecrypted(source, destination).Wait()
                        End If

                        Console.WriteLine("Build complete!")
                    End Using

                Else
                    Console.WriteLine("Error: The given source is neither a file nor a directory.")
                    Console.WriteLine("Source: ""{0}""", source)
                End If

            End If
        Catch ex As Exception
            Console.WriteLine(ex.ToString)
        End Try

        If args.Contains("-noclose") Then
            Console.WriteLine("Press the enter key to exit...")
            Console.ReadLine()
        End If
    End Sub

End Module
