using SkyEditor.Core.IO;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DotNet3dsToolkit
{
    public class TmdMetadata
    {
        public static async Task<TmdMetadata> Load(IBinaryDataAccessor data)
        {
            var tmd = new TmdMetadata(data);
            await tmd.Initalize();
            return tmd;
        }

        public TmdMetadata(IBinaryDataAccessor tmdData)
        {
            TmdData = tmdData ?? throw new ArgumentNullException(nameof(tmdData));
        }

        private async Task Initalize()
        {
            SignatureType = await TmdData.ReadInt32Async(0);
            int signatureLength;
            int signaturePadding;
            switch (SignatureType)
            {
                case 0x010000: // RSA_4096 SHA1 (Unused for 3DS)
                    signatureLength = 0x200;
                    signaturePadding = 0x3C;
                    break;
                case 0x010001: // RSA_2048 SHA1 (Unused for 3DS)
                    signatureLength = 0x100;
                    signaturePadding = 0x3C;
                    break;
                case 0x010002: // Elliptic Curve with SHA1 (Unused for 3DS)
                    signatureLength = 0x3C;
                    signaturePadding = 0x40;
                    break;
                case 0x010003: // RSA_4096 SHA256
                    signatureLength = 0x200;
                    signaturePadding = 0x3C;
                    break;
                case 0x010004: // RSA_2048 SHA256
                    signatureLength = 0x200;
                    signaturePadding = 0x100;
                    break;
                case 0x010005: // ECDSA with SHA256
                    signatureLength = 0x3C;
                    signaturePadding = 0x40;
                    break;
                default:
                    throw new NotSupportedException("Signature type " + SignatureType + " not supported");
            }

            Signature = await TmdData.ReadAsync(4, signatureLength);

            var totalSignatureSize = signatureLength + signaturePadding;
            SignatureIssuer = await TmdData.ReadAsync(totalSignatureSize + 0, 0x40);
            Version = await TmdData.ReadAsync(totalSignatureSize + 1);
            CaCrlVersion = await TmdData.ReadAsync(totalSignatureSize + 2);
            SignerCrlVersion = await TmdData.ReadAsync(totalSignatureSize + 3);
            Reserved1 = await TmdData.ReadAsync(totalSignatureSize + 4);
            SystemVersion = await TmdData.ReadInt64Async(totalSignatureSize + 0x44);
            TitleId = await TmdData.ReadInt64Async(totalSignatureSize + 0x4C);
            TitleType = await TmdData.ReadInt32Async(totalSignatureSize + 0x54);
            GroupId = await TmdData.ReadInt16Async(totalSignatureSize + 0x58);
            SaveDataSize = await TmdData.ReadInt32Async(totalSignatureSize + 0x5A);


            SrlPrivateSaveDataSize = await TmdData.ReadInt32Async(totalSignatureSize + 0x5E);
            Reserved2 = await TmdData.ReadInt32Async(totalSignatureSize + 0x62);
            SrlFlag = await TmdData.ReadAsync(totalSignatureSize + 0x66);
            Reserved3 = await TmdData.ReadAsync(totalSignatureSize + 0x67, 0x31);
            AccessRights = await TmdData.ReadInt32Async(totalSignatureSize + 0x98);
            TitleVersion = await TmdData.ReadInt16Async(totalSignatureSize + 0x9C);
            ContentCount = await TmdData.ReadInt16Async(totalSignatureSize + 0x9E);
            BootContent = await TmdData.ReadInt16Async(totalSignatureSize + 0xA0);
            Padding = await TmdData.ReadInt16Async(totalSignatureSize + 0xA2);
            ContentInfoRecordsHash = await TmdData.ReadAsync(totalSignatureSize + 0xA4, 0x20);

            var contentInfos = new List<ContentInfo>();
            for (int i = 0; i < 64; i++)
            {
                contentInfos.Add(new ContentInfo
                {
                    ContentIndexOffset = await TmdData.ReadInt16Async(totalSignatureSize + 0xC4 + (i * 0x24) + 0),
                    ContentCommandCount = await TmdData.ReadInt16Async(totalSignatureSize + 0xC4 + (i * 0x24) + 2),
                    NextRecordsHash = await TmdData.ReadAsync(totalSignatureSize + 0xC4 + (i * 0x24) + 4, 0x20)
                });
            }
            ContentInfoRecords = contentInfos.ToArray();

            var contentChunks = new List<ContentChunk>();
            for (int i = 0; i < ContentCount; i++)
            {
                contentChunks.Add(new ContentChunk
                {
                    ContentId = await TmdData.ReadInt32Async(totalSignatureSize + 0x9C4 + (i * 0x30) + 0),
                    ContentIndex = await TmdData.ReadInt16Async(totalSignatureSize + 0x9C4 + (i * 0x30) + 4),
                    ContentType = await TmdData.ReadInt16Async(totalSignatureSize + 0x9C4 + (i * 0x30) + 6),
                    ContentSize = await TmdData.ReadInt64Async(totalSignatureSize + 0x9C4 + (i * 0x30) + 8),
                    Hash = await TmdData.ReadAsync(totalSignatureSize + 0x9C4 + (i * 0x30) + 0x10, 0x20),
                });
            }
            ContentChunkRecords = contentChunks.ToArray();
        }

        private IBinaryDataAccessor TmdData { get; set; }

        public int SignatureType { get; set; } // index: 0, size: 4

        public byte[] Signature { get; set; } // index: 4, size varies (X). Includes padding to align to nearest 0x40

        public byte[] SignatureIssuer { get; set; } // Index: X + 0x00, size: 0x40

        public byte Version { get; set; } // Index: X + 0x40, size: 1

        public byte CaCrlVersion { get; set; } // Index: X + 0x41, size: 1

        public byte SignerCrlVersion { get; set; } // Index: X + 0x42, size: 1

        public byte Reserved1 { get; set; } // Index: X + 0x43, size: 1

        public long SystemVersion { get; set; } // Index: X + 0x44, size: 8

        public long TitleId { get; set; } // Index: X + 0x4C, size: 8

        public int TitleType { get; set; } // Index: X + 0x54, size: 4

        public short GroupId { get; set; } // Index: X + 0x58, size: 2

        /// <summary>
        /// Save data size, in bytes 
        /// </summary>
        public int SaveDataSize { get; set; } // Index: X + 0x5A, size: 4

        /// <summary>
        /// SRL Private save data size, in bytes
        /// </summary>
        public int SrlPrivateSaveDataSize { get; set; } // Index: X + 0x5E, size: 4

        public int Reserved2 { get; set; } // Index: X + 0x62, size: 4

        public byte SrlFlag { get; set; } // Index: X + 0x66, size: 1

        public byte[] Reserved3 { get; set; } // Index: X + 0x67, size: 0x31

        public int AccessRights { get; set; } // Index: X + 0x98, size: 4

        public short TitleVersion { get; set; } // Index: X + 0x9C, size: 2

        public short ContentCount { get; set; } // Index: X + 0x9E, size: 2

        public short BootContent { get; set; } // Index: X + 0xA0, size: 2

        public short Padding { get; set; } // Index: X + 0xA2, size: 2

        /// <summary>
        /// SHA-256 Hash of the Content Info Records
        /// </summary>
        public byte[] ContentInfoRecordsHash { get; set; } // Index: X + 0xA4, size: 0x20

        public ContentInfo[] ContentInfoRecords { get; set; } // index: X + 0xC4, Size: 0x24 * 64

        public ContentChunk[] ContentChunkRecords { get; set; } // index: 0x9C4, size: 0x30 * ContentCount

        public class ContentInfo
        {
            public short ContentIndexOffset { get; set; } // Index 0, size 2

            public short ContentCommandCount { get; set; } // Index 2, size 2

            /// <summary>
            /// SHA-256 hash of the next k content records that have not been hashed yet 
            /// </summary>
            public byte[] NextRecordsHash { get; set; } // Index 4, size 0x20
        }

        public class ContentChunk
        {
            public int ContentId { get; set; } // Index 0, size 4

            public short ContentIndex { get; set; } // Index 4, size 2

            public short ContentType { get; set; } // Index 6, size 2

            public long ContentSize { get; set; } // Index 8, size 8

            public byte[] Hash { get; set; } // Index 0x10, size 0x20
        }
    }
}
