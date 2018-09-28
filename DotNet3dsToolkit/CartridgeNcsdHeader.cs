using System;
using System.Collections.Generic;
using System.Text;

namespace DotNet3dsToolkit
{
    public class CartridgeNcsdHeader : NcsdHeader
    {
        public CartridgeNcsdHeader(byte[] header) : base(header)
        {
        }

        /// <summary>
        /// Exheader SHA-256 hash
        /// </summary>
        public byte[] ExheaderHash { get; private set; } // Offset: 0x160, Size: 0x20

        public int AdditionalHeaderSize { get; private set; } // Offset: 0x180, Size: 0x4

        public int SectorZeroOffset { get; private set; } // Offset: 0x184, Size: 0x4

        public long PartitionFlags { get; private set; } // Offset: 0x188, Size: 0x8

        public byte[] PartitionIdTable { get; private set; } // Offset: 0x190, Size: 8*8

        protected byte[] Reserved1 { get; set; } // Offset: 0x190, Size: 0x20

        // Documentation is unsure about the use of this
        protected byte[] Reserved2 { get; set; } // Offset: 0x1F0, Size: 0xE

        /// <summary>
        /// Support for this was implemented with 9.6.0-X FIRM.
        /// Bit0=1 enables using bits 1-2, it's unknown what these two bits are actually used for
        /// (the value of these two bits get compared with some other value during NCSD verification/loading).
        /// This appears to enable a new, likely hardware-based, antipiracy check on cartridges.
        /// </summary>
        protected byte Unknown1 { get; set; } // Offset: 0x1FE, Size: 1

        /// <summary>
        /// Support for this was implemented with 9.6.0-X FIRM, see docs regarding save crypto.
        /// </summary>
        protected byte Unknown2 { get; set; } // Offset: 0x1FF, Size: 1

        // To-Do: Move this to NcsdHeader maybe?

        /// <summary>
        /// Writable address of the CARD2 on-chip savedata, or -1 if the cartridge is CARD1
        /// </summary>
        public int Card2SaveAddress { get; private set; } // Offset: 0x200, Size: 4

        public int CardInfoBitmask { get; private set; } // Offset: 0x204, Size: 4

        // Called Reserved1 in 3dbrew
        protected byte[] Reserved3 { get; set; } // Offset: 0x208, size: 0x108

        public short TitleVersion { get; private set; } // Offset: 0x310, size: 2

        public short CardRevision { get; private set; } // Offset: 0x312, size: 2

        // Called Reserved2 in 3dbrew
        // 3dbrew lists same offset as Reserved1 (aka Reserved3). This offset is based on math and may be inaccurate.
        protected byte[] Reserved4 { get; set; } // Offset: 0x314, size: 0xCEE

        /// <summary>
        /// Card seed keyY (first u64 is Media ID (same as first NCCH partitionId))
        /// </summary>
        public byte[] CardSeedY { get; private set; } // Offset: 0x1000, size: 0x10

        /// <summary>
        /// Encrypted card seed (AES-CCM, keyslot 0x3B for retail cards, see CTRCARD_SECSEED)
        /// </summary>
        public byte[] EncryptedCardSeed { get; private set; } // Offset: 0x1010, size: 0x10

        /// <summary>
        /// Card seed AES-MAC
        /// </summary>
        public byte[] CardSeedAESMAC { get; private set; } // Offset: 0x1020, size: 0x10

        public byte[] CardSeedNonce { get; private set; } // Offset: 0x1030, size: 0xC

        // Called Reserved3 in 3dbrew
        protected byte[] Reserved5 { get; set; } // Offset: 0x103C, size: 0xC4

        /// <summary>
        /// Copy of first NCCH header (excluding RSA signature)
        /// </summary>
        protected byte[] FirstNcchHeader { get; set; } // Offset: 0x1100, Size: 0x100

        // "private headers" stored in this area

        public byte[] CardDeviceReserved1 { get; private set; } // Offset: 0x1200, Size: 0x200

        public byte[] TitleKey { get; private set; } // Offset: 0x1400, Size: 0x10

        public byte[] CardDeviceReserved2 { get; private set; } // Offset: 0x1410, Size: 0xF0
    }
}
