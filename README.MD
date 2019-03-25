# .Net 3DS Toolkit
.Net 3DS Toolkit is a code library intended to simplify extracting and rebuilding 3DS ROMs.

It is currently being rewritten in .Net Core 2.2 (and .Net Standard 2.1 once it's out). Please see the Old folder for the original source and details.

Current features:
* Extract decrypted CCI files
* Extract decrypted CIA files
* Extract decrypted CXI files
* Extract decrypted RomFS files
* Extract decrypted ExeFS files
* Allow reading data of individual files for all above formats without extraction

Planned features:
* Console interface
* GUI (using Avalonia for cross-platform support)
* Build ROMs
* Allow reading of individual partitions without extraction

## Building

Requires adding the following NuGet package:
```
https://www.myget.org/F/skyeditor/api/v3/index.json
```

## Credits

* 3dbrew.org for documentation
* SciresM for [RomFS-Builder](https://github.com/SciresM/RomFS-Builder), which was adapted for use in this project