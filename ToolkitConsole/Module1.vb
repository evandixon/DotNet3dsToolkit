Module Module1

    Sub Main()
        Using c As New DotNet3dsToolkit.Converter
            Dim options As New DotNet3dsToolkit.ExtractionOptions
            options.SourceRom = IO.Path.Combine(Environment.CurrentDirectory, "rom.3ds")
            options.DestinationDirectory = IO.Path.Combine(Environment.CurrentDirectory, "RawFiles")
            options.DecompressCodeBin = False

            c.Extract(options).Wait()
        End Using
    End Sub

End Module
