![Build Status](http://teamcity.skyeditor.org:90/app/rest/builds/buildType:(id:DotNet3dsToolkit_DotNet3dsToolkit)/statusIcon)

# .Net 3DS Toolkit
.Net 3DS Toolkit is a code library intended to simplify extracting and rebuilding 3DS ROMs.  Requires the .Net Framework 4.6.

Supported formats for extraction:
* Decrypted CCI (aka .3DS)
* Decrypted CIA
* Decrypted CXI (like what Braindump would give you)
* NDS ROM

Supported formats for building:
* Decrypted CCI (aka .3DS)
  * For use with Citra
  * Can be used for Gateway and Sky 3DS (with CFW) if encrypted using Decrypt9
* 0-Key Encrypted CCI
  * For use with Gateway
* CIA
  * For use with CFW
* HANS RomFS, Code.Bin, and Homebrew Launcher Shortcut
* NDS ROM

Planned Features
* Automatically add private header to 0-key encrypted CCI's.

Known Limitations
* DSi ROMs are not supported.

Using the library (VB.Net):
```
' To Extract
Using c As New DotNet3dsToolkit.Converter
	' To use the most appropriate format based on the extension
	Await c.ExtractAuto("C:/MyROM.cxi", "C:/RawFiles/")

    ' To extract a decrypted CCI
    Await c.ExtractCCI("C:/MyROM.3ds", "C:/RawFiles/") 'Do not run these functions concurrently.  If you need to do so, create another instance of DotNet3dsToolkit.Converter to build another directory.

	' To extract a decrypted CXI
	Await c.ExtractCXI("C:/MyROM.cxi", "C:/RawFiles/")

	' To extract a decrypted CIA
	Await c.ExtractCIA("C:/MyROM.cia", "C:/RawFiles/")

	' To extract an NDS ROM
	Await c.ExtractNDS("C:/MyROM.nds", "C:/RawFiles-NDS/")

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

	' To build an NDS ROM
	Await c.BuildNDS("C:/RawFiles-NDS/", "C:/MyRom-Rebuilt.nds")

	' To use the most appropriate format based on the extension
	Await c.BuildAuto("C:/RawFiles/", "C:/MyRom-Rebuilt.cia")

	' To build files for HANS (not supported by BuildAuto)
	Await c.BuildHans("C:/RawFiles/", "G:/", "MyHack")
End Using
```

Using the library (C#.Net):
```

// To Extract
using (DotNet3dsToolkit.Converter c = new DotNet3dsToolkit.Converter()) {
	// To use the most appropriate format based on the extension
	await c.ExtractAuto("C:/MyROM.cxi", "C:/RawFiles/");

    // To extract a decrypted CCI
    await c.ExtractCCI("C:/MyROM.3ds", "C:/RawFiles/"); 'Do not run these functions concurrently.  If you need to do so, create another instance of DotNet3dsToolkit.Converter to build another directory.

	// To extract a decrypted CXI
	await c.ExtractCXI("C:/MyROM.cxi", "C:/RawFiles/");

	// To extract a decrypted CIA
	await c.ExtractCIA("C:/MyROM.cia", "C:/RawFiles/");

	// To extract an NDS ROM
	await c.ExtractNDS("C:/MyROM.nds", "C:/RawFiles-NDS/");

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

	// To build an NDS ROM
	await c.BuildNDS("C:/RawFiles-NDS/", "C:/MyRom-Rebuilt.nds");

	// To use the most appropriate format based on the extension
	await c.BuildAuto("C:/RawFiles/", "C:/MyRom-Rebuilt.cia");

	// To build files for HANS (not supported by BuildAuto)
	await c.BuildHans("C:/RawFiles/", "G:/", "MyHack");
}

```

Using the ToolkitConsole.exe:
`ToolkitConsole.exe <source> <destination> [hans raw file name] [-source-cxi|-source-cia|-source-nds] [-key0]`
* <source> can be a decrypted CCI/3DS ROM, a decrypted CIA, a decrypted CXI, or a directory created by ToolkitConsole.exe.
* <destination> can be a *.3DS, *.3DZ, *.CCI, or *.CIA file, or a directory if the source is a ROM.
* [hans raw file name] is the future name of the raw files for HANS, if the ""-hans"" argument is present.  Shorter strings work better, but the exact requirements are unknown.
* Input format is detected by the extension.
  * *.CXI files are treated as decrypted CXI files.
  * *.CIA files are treated as decrypted CIA files.
  * *.NDS and *.SRL files are treated as NDS ROMs.
  * All others are treated as decrypted CCI files.  This can be overridden by using the -source-cxi flag to treat input as a CXI or the -source-cia flag to treat the input as a CIA.
* Output format is detected by the extension.
  * *.CIA files are outputted as CIA files.
  * *.3DZ files are outputted as 0-key encrypted CCI ROMs.
  * *.NDS and *.SRL files are outputted as NDS ROMs.
  * All others are outputted as decrypted CCI ROMs.  This can be overridden by using the -key0 flag to output as a 0-key encrypted CCI ROM instead.

Credits:
* SciresM for [3DS Builder](https://github.com/SciresM/3DS-Builder) ([modified to use command-line arguments](https://github.com/evandixon/3DS-Builder))
* profi200 for [Project_CTR](https://github.com/profi200/Project_CTR)'s ctrtool and makerom
* dnasdw for [3dstool](https://github.com/dnasdw/3dstool)
* DarkFader for ndstool
* Asia81 for HackingToolkit3DS, which provided the usage for 3dstool.
* Steveice10 for FBI, which I'm using in my unit tests to read title IDs.
