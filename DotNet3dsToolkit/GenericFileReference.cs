using SkyEditor.Core.IO;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DotNet3dsToolkit
{
    /// <summary>
    /// Provides a read-only view on top of a generic file or other generic file reference
    /// </summary>
    public class GenericFileReference
    {
        public GenericFileReference(GenericFile file, long offset, long length)
        {
            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            File = file ?? throw new ArgumentNullException(nameof(file));
            Offset = offset;
            Length = length;
        }

        public GenericFileReference(GenericFileReference reference, long offset, long length)
        {
            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            Reference = reference ?? throw new ArgumentNullException(nameof(reference));
            Offset = offset;
            Length = length;
        }

        private GenericFile File { get; }
        private GenericFileReference Reference { get; }
        private long Offset { get; set; }

        public long Length { get; private set; }

        public bool IsThreadSafe
        {
            get
            {
                if (File != null)
                {
                    return File.IsThreadSafe;
                }
                else
                {
                    return Reference.IsThreadSafe;
                }
            }
        }


        public async Task<byte[]> ReadAsync()
        {
            if (Length > int.MaxValue)
            {
                throw new ArgumentException(Properties.Resources.BufferOverflow_BecauseIntMaxValue);
            }

            if (File != null)
            {
                return await File.ReadAsync(Offset, (int)Length);
            }
            else
            {
                return await Reference.ReadAsync(Offset, (int)Length);
            }
        }

        public async Task<byte> ReadAsync(long index)
        {
            if (File != null)
            {
                return await File.ReadAsync(Offset + index);
            }
            else
            {
                return await Reference.ReadAsync(Offset + index);
            }
        }

        public async Task<byte[]> ReadAsync(long index, int length)
        {
            if (Length > int.MaxValue)
            {
                throw new ArgumentException(Properties.Resources.BufferOverflow_BecauseIntMaxValue);
            }

            if (File != null)
            {
                return await File.ReadAsync(Offset + index, (int)Math.Min(Length, length));
            }
            else
            {
                return await Reference.ReadAsync(Offset + index, (int)Math.Min(Length, length));
            }
        }

        public async Task<int> ReadInt32Async(long index)
        {
            return BitConverter.ToInt32(await ReadAsync(index, 4), 0);
        }

        public async Task<long> ReadInt64Async(long index)
        {
            return BitConverter.ToInt64(await ReadAsync(index, 8), 0);
        }
    }
}
