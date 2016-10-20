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
	' To use the most appropriate format based on the extension
	Await c.ExtractCXI("C:/MyROM.cxi", "C:/RawFiles/")

    ' To extract a decrypted CCI
    Await c.ExtractCCI("C:/MyROM.3ds", "C:/RawFiles/") 'Do not run these functions concurrently.  If you need to do so, create another instance of DotNet3dsToolkit.Converter to build another directory.

	' To extract a decrypted CXI
	Await c.ExtractCXI("C:/MyROM.cxi", "C:/RawFiles/")
End Using 'It is important to dispose of the converter when finished.  This will delete temporary files created during extraction.

' To Build
Using c As New DotNet3dsToolkit.Converter
	' To build a CIA
	' Extensions don't matter, but incorrect extensions may cause confusion later.
    Await c.BuildCia("C:/RawFiles/", "C:/MyRom-Rebuilt.cia") 'Do not run these functions concurrently.  If you need to do so, create another instance of DotNet3dsToolkit.Converter to build another directory.

	' To build a CCI for use with Gateway
	Await c.Build3DS0Key("C:/RawFiles/", "C:/MyRom-Rebuilt.3dz")

	' To build a decrypted CIA for use with Citra or Decrypt9
	Await c.Build3DSDecrypted("C:/RawFiles/", "C:/MyRom-Rebuilt.3ds")

	' To use the most appropriate format based on the extension
	Await c.BuildAuto("C:/RawFiles/", "C:/MyRom-Rebuilt.cia")
End Using
```

Using the library (C#.Net):
```

// To Extract
using (DotNet3dsToolkit.Converter c = new DotNet3dsToolkit.Converter()) {
	// To use the most appropriate format based on the extension
	await c.ExtractCXI("C:/MyROM.cxi", "C:/RawFiles/");

    // To extract a decrypted CCI
    await c.ExtractCCI("C:/MyROM.3ds", "C:/RawFiles/"); 'Do not run these functions concurrently.  If you need to do so, create another instance of DotNet3dsToolkit.Converter to build another directory.

	// To extract a decrypted CXI
	await c.ExtractCXI("C:/MyROM.cxi", "C:/RawFiles/");
} // It is important to dispose of the converter when finished.  This will delete temporary files created during extraction.

// To Build
using (DotNet3dsToolkit.Converter c = new DotNet3dsToolkit.Converter()) {
	// To build a CIA
	// Extensions don't matter, but incorrect extensions may cause confusion later.
    await c.BuildCia("C:/RawFiles/", "C:/MyRom-Rebuilt.cia"); 'Do not run these functions concurrently.  If you need to do so, create another instance of DotNet3dsToolkit.Converter to build another directory.

	// To build a CCI for use with Gateway
	await c.Build3DS0Key("C:/RawFiles/", "C:/MyRom-Rebuilt.3dz");

	// To build a decrypted CIA for use with Citra or Decrypt9
	await c.Build3DSDecrypted("C:/RawFiles/", "C:/MyRom-Rebuilt.3ds");

	// To use the most appropriate format based on the extension
	await c.BuildAuto("C:/RawFiles/", "C:/MyRom-Rebuilt.cia");
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