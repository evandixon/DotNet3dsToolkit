using SkyEditor.Core.IO;
using SkyEditor.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace DotNet3dsToolkit.Core
{
    /// <summary>
    /// A ROM for the Nintendo DS
    /// </summary>
    public class NdsRom : GenericFile, IReportProgress, IIOProvider
    {
        public event EventHandler<ProgressReportedEventArgs> ProgressChanged;
        public event EventHandler Completed;

        #region Properties
        #region Header Properties

        public string GameTitle
        {
            get
            {
                return ReadString(0, 12, Encoding.ASCII);
            }
            set
            {
                WriteString(0, Encoding.ASCII, value.PadRight(12, '\0').Substring(0, 12));
            }
        }

        public string GameCode
        {
            get
            {
                return ReadString(12, 4, Encoding.ASCII);
            }
            set
            {
                WriteString(12, Encoding.ASCII, value.PadRight(4, '\0').Substring(0, 4));
            }
        }

        public string MakerCode
        {
            get
            {
                return ReadString(16, 2, Encoding.ASCII);
            }
            set
            {
                WriteString(16, Encoding.ASCII, value.PadRight(2, '\0').Substring(0, 2));
            }
        }

        public byte UnitCode
        {
            get
            {
                return Read(0x12);
            }
            set
            {
                Write(0x12, value);
            }
        }

        public byte EncryptionSeedSelect
        {
            get
            {
                return Read(0x13);
            }
            set
            {
                Write(0x13, value);
            }
        }

        /// <summary>
        /// The capacity of the cartridge.  Cartridge size = 128KB * (2 ^ DeviceCapacity)
        /// </summary>
        public byte DeviceCapacity
        {
            get
            {
                return Read(0x14);
            }
            set
            {
                Write(0x14, value);
            }
        }

        /// <summary>
        /// Region of the ROM.
        /// (00h=Normal, 80h=China, 40h=Korea)
        /// </summary>
        public byte NdsRegion
        {
            get
            {
                return Read(0x1D);
            }
            set
            {
                Write(0x1D, value);
            }
        }

        public byte RomVersion
        {
            get
            {
                return Read(0x1E);
            }
            set
            {
                Write(0x1E, value);
            }
        }

        //01Fh    1     Autostart (Bit2: Skip "Press Button" after Health and Safety)
        //(Also skips bootmenu, even in Manual mode & even Start pressed)

        public int Arm9RomOffset
        {
            get
            {
                return ReadInt32(0x20);
            }
            set
            {
                WriteInt32(0x20, value);
            }
        }

        public int Arm9EntryAddress
        {
            get
            {
                return ReadInt32(0x24);
            }
            set
            {
                WriteInt32(0x24, value);
            }
        }
        public int Arm9RamAddress
        {
            get
            {
                return ReadInt32(0x28);
            }
            set
            {
                WriteInt32(0x28, value);
            }
        }

        public int Arm9Size
        {
            get
            {
                return ReadInt32(0x2C);
            }
            set
            {
                WriteInt32(0x2C, value);
            }
        }

        public int Arm7RomOffset
        {
            get
            {
                return ReadInt32(0x30);
            }
            set
            {
                WriteInt32(0x30, value);
            }
        }

        public int Arm7EntryAddress
        {
            get
            {
                return ReadInt32(0x34);
            }
            set
            {
                WriteInt32(0x34, value);
            }
        }
        public int Arm7RamAddress
        {
            get
            {
                return ReadInt32(0x38);
            }
            set
            {
                WriteInt32(0x38, value);
            }
        }

        public int Arm7Size
        {
            get
            {
                return ReadInt32(0x3C);
            }
            set
            {
                WriteInt32(0x3C, value);
            }
        }

        public int FilenameTableOffset
        {
            get
            {
                return ReadInt32(0x40);
            }
            set
            {
                WriteInt32(0x40, value);
            }
        }

        #endregion
        #endregion

        #region IReportProgress Implementation

        /// <summary>
        /// Raises <see cref="ProgressChanged"/> using the value of relevant properties
        /// </summary>
        private void ReportProgressChanged()
        {
            ProgressChanged?.Invoke(this, new ProgressReportedEventArgs { Message = Message, IsIndeterminate = false, Progress = Progress });
        }

        /// <summary>
        /// The number of files that have been extracted in the current extraction operation
        /// </summary>
        public int ExtractedFileCount
        {
            get
            {
                return _extractedFileCount;
            }
            set
            {
                if (_extractedFileCount != value)
                {
                    _extractedFileCount = value;
                    ReportProgressChanged();
                }
            }
        }
        private int _extractedFileCount;

        /// <summary>
        /// The total number of files in the ROM
        /// </summary>
        public int TotalFileCount
        {
            get
            {
                return _totalFileCount;
            }
            set
            {
                if (_totalFileCount != value)
                {
                    _totalFileCount = value;
                    ReportProgressChanged();
                }
            }
        }
        private int _totalFileCount;

        /// <summary>
        /// A percentage representing the progress of the current extraction operation
        /// </summary>
        public float Progress => ExtractedFileCount / TotalFileCount;

        /// <summary>
        /// A string representing what is being done in the current extraction operation
        /// </summary>
        public string Message
        {
            get
            {
                if (IsCompleted)
                {
                    return Properties.Resources.Complete;
                }
                else
                {
                    return Properties.Resources.LoadingUnpacking;
                }
            }
        }

        /// <summary>
        /// Whether or not the progress of the current extraction operation can be determined
        /// </summary>
        bool IReportProgress.IsIndeterminate => false;

        /// <summary>
        /// Whether or not the current extraction operation is complete
        /// </summary>
        public bool IsCompleted
        {
            get
            {
                return _isCompleted;
            }
            set
            {
                _isCompleted = value;
                if (_isCompleted)
                {
                    Completed?.Invoke(this, new EventArgs());
                }
            }
        }
        private bool _isCompleted;

        #endregion

        #region IIOProvider Implementation
        public string WorkingDirectory { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public void ResetWorkingDirectory()
        {
            throw new NotImplementedException();
        }

        public long GetFileLength(string filename)
        {
            throw new NotImplementedException();
        }

        public bool FileExists(string filename)
        {
            throw new NotImplementedException();
        }

        public bool DirectoryExists(string path)
        {
            throw new NotImplementedException();
        }

        public void CreateDirectory(string path)
        {
            throw new NotImplementedException();
        }

        public string[] GetFiles(string path, string searchPattern, bool topDirectoryOnly)
        {
            throw new NotImplementedException();
        }

        public string[] GetDirectories(string path, bool topDirectoryOnly)
        {
            throw new NotImplementedException();
        }

        public byte[] ReadAllBytes(string filename)
        {
            throw new NotImplementedException();
        }

        public string ReadAllText(string filename)
        {
            throw new NotImplementedException();
        }

        public void WriteAllBytes(string filename, byte[] data)
        {
            throw new NotImplementedException();
        }

        public void WriteAllText(string filename, string data)
        {
            throw new NotImplementedException();
        }

        public void CopyFile(string sourceFilename, string destinationFilename)
        {
            throw new NotImplementedException();
        }

        public void DeleteFile(string filename)
        {
            throw new NotImplementedException();
        }

        public void DeleteDirectory(string path)
        {
            throw new NotImplementedException();
        }

        public string GetTempFilename()
        {
            throw new NotImplementedException();
        }

        public string GetTempDirectory()
        {
            throw new NotImplementedException();
        }

        public Stream OpenFile(string filename)
        {
            throw new NotImplementedException();
        }

        public Stream OpenFileReadOnly(string filename)
        {
            throw new NotImplementedException();
        }

        public Stream OpenFileWriteOnly(string filename)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
