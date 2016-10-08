# .Net 3DS Toolkit
.Net 3DS Toolkit is a code library intended to simplify extracting and rebuilding 3DS ROMs.  Requires the .Net Framework 4.6.

Supported formats for extraction:
- Decrypted CCI (aka .3DS)

Supported formats for building:
- Decrypted CCI (aka .3DS)
-- For use with Citra
-- Can be used for Gateway and Sky 3DS (with CFW) if encrypted using Decrypt9
- 0-Key Encrypted CCI
-- For use with Gateway
- CIA
-- For use with CFW

Planned Features
- Build files for HANS
- More supported source formats (more might work, but only decrypted CCI's have been tested)

Credits:
- SciresM for [3DS Builder](https://github.com/SciresM/3DS-Builder) ([modified to use command-line arguments](https://github.com/evandixon/3DS-Builder))
- profi200 for [Project_CTR](https://github.com/profi200/Project_CTR)'s makerom
- dnasdw for [3dstool](https://github.com/dnasdw/3dstool)
- Asia81 for HackingToolkit3DS, which provided the usage for 3dstool.