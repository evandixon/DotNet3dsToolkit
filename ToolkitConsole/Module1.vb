Module Module1

    Sub Main()
        Using c As New DotNet3dsToolkit.Converter
            Console.WriteLine("Extracting...")
            Dim options As New DotNet3dsToolkit.ExtractionOptions
            options.SourceRom = IO.Path.Combine(Environment.CurrentDirectory, "rom.3ds")
            options.DestinationDirectory = IO.Path.Combine(Environment.CurrentDirectory, "RawFiles")
            options.DecompressCodeBin = False

            c.Extract(options).Wait()

            Console.WriteLine("Building...")
            Dim options2 As New DotNet3dsToolkit.BuildOptions
            options2.SourceDirectory = IO.Path.Combine(Environment.CurrentDirectory, "RawFiles")
            options2.DestinationROM = IO.Path.Combine(Environment.CurrentDirectory, "PatchedRom-Test.3ds")
            options2.CompressCodeBin = False

            c.Build3DSDecrypted(options2).Wait()
        End Using
    End Sub

End Module
