using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace DotNet3dsToolkit
{
    public class ProcessingProgressedToken
    {
        /// <summary>
        /// Raised when either <see cref="TotalFileCount"/> or <see cref="ProcessedFileCount"/> changed
        /// </summary>
        public event EventHandler FileCountChanged;

        /// <summary>
        /// Number of files that have been extracted
        /// </summary>
        public int ProcessedFileCount
        {
            get
            {
                return _processedFileCount;
            }
            set
            {
                _processedFileCount = value;
                FileCountChanged?.Invoke(this, new EventArgs());
            }
        }
        private int _processedFileCount;

        /// <summary>
        /// Total number of files
        /// </summary>
        public int TotalFileCount
        {
            get
            {
                return _totalFileCount;
            }
            set
            {
                _totalFileCount = value;
                FileCountChanged?.Invoke(this, new EventArgs());
            }
        }
        private int _totalFileCount;

        public void IncrementProcessedFileCount()
        {
            Interlocked.Increment(ref _processedFileCount);
            FileCountChanged?.Invoke(this, new EventArgs());
        }
    }
}
