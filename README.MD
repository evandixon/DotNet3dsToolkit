# .Net 3DS Toolkit
.Net 3DS Toolkit is a code library intended to simplify extracting and rebuilding 3DS ROMs.

It is currently being rewritten in .Net Core 2.2 (and .Net Standard 2.1 once it's out). Please see the Old folder for the original source and details.

Current features:
* Extract & Rebuild decrypted CCI (aka .3ds) files
* Extract decrypted CIA files
* Extract & Rebuild decrypted CXI files
* Extract & Rebuild decrypted RomFS files
* Extract & Rebuild decrypted ExeFS files
* Allow reading data of individual files for all above formats without extraction

Planned features:
* Virtual file system to allow (the illusion of) in-place editing of ROMs through IFileSystem interface
* Console interface
* GUI
* Rebuild CIA files
* Allow converting between CCI/CIA 
* Extract & Rebuild CSU files

## Credits

* 3dbrew.org for documentation
* SciresM for [RomFS-Builder](https://github.com/SciresM/RomFS-Builder), which was adapted for use in this project
