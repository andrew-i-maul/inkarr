using System;
using System.Collections.Generic;
using System.IO.Compression;

namespace VersOne.Epub
{
    public class EpubIssueRef : IDisposable
    {
        private bool _isDisposed;

        public EpubIssueRef(ZipArchive epubArchive)
        {
            EpubArchive = epubArchive;
            _isDisposed = false;
        }

        ~EpubIssueRef()
        {
            Dispose(false);
        }

        public string FilePath { get; set; }
        public string Title { get; set; }
        public string Volume { get; set; }
        public List<string> VolumeList { get; set; }
        public EpubSchema Schema { get; set; }

        protected ZipArchive EpubArchive { get; private set; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    EpubArchive?.Dispose();
                }

                _isDisposed = true;
            }
        }
    }
}
