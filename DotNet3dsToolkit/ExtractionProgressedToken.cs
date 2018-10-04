using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace DotNet3dsToolkit
{
    public class ExtractionProgressedToken
    {
        /// <summary>
        /// Raised when either <see cref="TotalFileCount"/> or <see cref="ExtractedFileCount"/> changed
        /// </summary>
        public event EventHandler FileCountChanged;

        /// <summary>
        /// Number of files that have been extracted
        /// </summary>
        public int ExtractedFileCount
        {
            get
            {
                return _extractedFileCount;
            }
            set
            {
                _extractedFileCount = value;
                FileCountChanged?.Invoke(this, new EventArgs());
            }
        }
        private int _extractedFileCount;

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

        public void IncrementExtractedFileCount()
        {
            Interlocked.Increment(ref _extractedFileCount);
            FileCountChanged?.Invoke(this, new EventArgs());
        }
    }
}
