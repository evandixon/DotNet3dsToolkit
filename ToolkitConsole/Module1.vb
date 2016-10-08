Module Module1

    Sub Main()
        Try
            Using c As New DotNet3dsToolkit.Converter
                Console.WriteLine("Extracting...")
                Dim options As New DotNet3dsToolkit.ExtractionOptions
                options.SourceRom = IO.Path.Combine(Environment.CurrentDirectory, "rom.3ds")
                options.DestinationDirectory = IO.Path.Combine(Environment.CurrentDirectory, "RawFiles")
                options.DecompressCodeBin = False

                c.Extract(options).Wait()

                Console.WriteLine("Building decrypted...")
                Dim options2 As New DotNet3dsToolkit.BuildOptions
                options2.SourceDirectory = IO.Path.Combine(Environment.CurrentDirectory, "RawFiles")
                options2.DestinationROM = IO.Path.Combine(Environment.CurrentDirectory, "PatchedRom-Test-Decrypted.3ds")
                options2.CompressCodeBin = False

                c.Build3DSDecrypted(options2).Wait()

                Console.WriteLine("Building 0-key...")
                Dim options3 As New DotNet3dsToolkit.BuildOptions
                options3.SourceDirectory = IO.Path.Combine(Environment.CurrentDirectory, "RawFiles")
                options3.DestinationROM = IO.Path.Combine(Environment.CurrentDirectory, "PatchedRom-Test-Gateway.3ds")
                options3.CompressCodeBin = Nothing

                c.Build3DS0Key(options3).Wait()

                Console.WriteLine("Building cia...")
                Dim options4 As New DotNet3dsToolkit.BuildOptions
                options4.SourceDirectory = IO.Path.Combine(Environment.CurrentDirectory, "RawFiles")
                options4.DestinationROM = IO.Path.Combine(Environment.CurrentDirectory, "PatchedRom-Test-Decrypted.cia")
                options4.CompressCodeBin = Nothing

                c.BuildCia(options4).Wait()
            End Using
        Catch ex As Exception
            Throw
        End Try
    End Sub

End Module
