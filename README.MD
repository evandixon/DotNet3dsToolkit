# .Net 3DS Toolkit
.Net 3DS Toolkit is a code library intended to simplify extracting and rebuilding 3DS ROMs.  Requires the .Net Framework 4.6.

Supported formats for extraction:
* Decrypted CCI (aka .3DS)
* Decrypted CXI (like what Braindump would give you)

Supported formats for building:
* Decrypted CCI (aka .3DS)
  * For use with Citra
  * Can be used for Gateway and Sky 3DS (with CFW) if encrypted using Decrypt9
* 0-Key Encrypted CCI
  * For use with Gateway
* CIA
  * For use with CFW

Planned Features
* Build files for HANS
* More supported source formats (more might work, but only decrypted CCI's have been tested)
* Automatically add private header to 0-key encrypted CCI's.

Using the library (VB.Net):
```
' To Extract
Using c As New DotNet3dsToolkit.Converter
    Dim options As New DotNet3dsToolkit.ExtractionOptions
    options.SourceRom = "C:/MyROM.3ds"
    options.DestinationDirectory = "C:/RawFiles/"
	
	' To extract a decrypted CCI
    Await c.ExtractCCI(options) 'Do not run these functions concurrently.  If you need to do so, create another instance of DotNet3dsToolkit.Converter to build another directory.

	' To extract a decrypted CXI
	Await c.ExtractCXI(options)
End Using 'It is important to dispose of the converter when finished.  This will delete temporary files created during extraction.

' To Build
Using c As New DotNet3dsToolkit.Converter
    Dim options As New DotNet3dsToolkit.BuildOptions
    options.SourceDirectory = "C:/RawFiles/"    

	' To build a CIA
	options.DestinationROM = "C:/MyRom-Rebuilt.cia" 'Extensions don't matter, but incorrect extensions may cause confusion later.
    Await c.BuildCia(options) 'Do not run these functions concurrently.  If you need to do so, create another instance of DotNet3dsToolkit.Converter to build another directory.

	' To build a CCI for use with Gateway
	options.DestinationROM = "C:/MyRom-Rebuilt.3dz"
	Await c.Build3DS0Key(options)

	' To build a decrypted CIA for use with Citra or Decrypt9
	options.DestinationROM = "C:/MyRom-Rebuilt.3ds"
	Await c.Build3DSDecrypted(options)
End Using
```

Using the library (C#.Net):
```

// To Extract
using (DotNet3dsToolkit.Converter c = new DotNet3dsToolkit.Converter()) {
	DotNet3dsToolkit.ExtractionOptions options = new DotNet3dsToolkit.ExtractionOptions();
	options.SourceRom = "C:/MyROM.3ds";
	options.DestinationDirectory = "C:/RawFiles/";

	// To extract a decrypted CCI
    await c.ExtractCCI(options); 'Do not run these functions concurrently.  If you need to do so, create another instance of DotNet3dsToolkit.Converter to build another directory.

	// To extract a decrypted CXI
	await c.ExtractCXI(options);
} // It is important to dispose of the converter when finished.  This will delete temporary files created during extraction.

// To Build
using (DotNet3dsToolkit.Converter c = new DotNet3dsToolkit.Converter()) {
	DotNet3dsToolkit.BuildOptions options = new DotNet3dsToolkit.BuildOptions();
	options.SourceDirectory = "C:/RawFiles/";

	// To build a CIA
	options.DestinationROM = "C:/MyRom-Rebuilt.cia";  // Extensions don't matter, but incorrect extensions may cause confusion later.
	await c.BuildCia(options);  // Do not run these functions concurrently.  If you need to do so, create another instance of DotNet3dsToolkit.Converter to build another directory.

	// To build a CCI for use with Gateway
	options.DestinationROM = "C:/MyRom-Rebuilt.3dz";
	await c.Build3DS0Key(options);

	// To build a decrypted CIA for use with Citra or Decrypt9
	options.DestinationROM = "C:/MyRom-Rebuilt.3ds";
	await c.Build3DSDecrypted(options);
}

```

Using the ToolkitConsole.exe:
`ToolkitConsole.exe <source> [source-cxi] <destination> [-key0]`
* <source> can be a decrypted CCI/3DS ROM or a decrypted CXI, or a directory created by ToolkitConsole.exe.
* <destination> can be a *.3DS, *.3DZ, *.CCI, or *.CIA file, or a directory if the source is a ROM.
* Input format is detected by the extension.
  * *.CXI files are treated as decrypted CXI files.
  * All others are treated as decrypted CCI files.  This can be overridden by using the -source-cxi flag to treat input as a CXI.
* Output format is detected by the extension.
  * *.CIA files are outputted as CIA files.
  * *.3DZ files are outputted as 0-key encrypted CCI ROMs.
  * All others are outputted as decrypted CCI ROMs.  This can be overridden by using the -key0 flag to output as a 0-key encrypted CCI ROM instead.

Credits:
* SciresM for [3DS Builder](https://github.com/SciresM/3DS-Builder) ([modified to use command-line arguments](https://github.com/evandixon/3DS-Builder))
* profi200 for [Project_CTR](https://github.com/profi200/Project_CTR)'s makerom
* dnasdw for [3dstool](https://github.com/dnasdw/3dstool)
* Asia81 for HackingToolkit3DS, which provided the usage for 3dstool.