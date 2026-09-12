using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using NLog;
using PdfSharpCore.Pdf.IO;
using SharpCompress.Archives;
using SharpCompress.Archives.Rar;

namespace NzbDrone.Core.MediaFiles.Comics
{
    public class ComicArchiveContents
    {
        public ComicInfo ComicInfo { get; set; }
        public int PageCount { get; set; }
        public byte[] CoverImage { get; set; }
        public string CoverImageExtension { get; set; }
    }

    public interface IComicArchiveReader
    {
        ComicArchiveContents Read(string path, bool readCoverImage = true);
    }

    public class ComicArchiveReader : IComicArchiveReader
    {
        private static readonly HashSet<string> ImageExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp"
        };

        private readonly Logger _logger;

        public ComicArchiveReader(Logger logger)
        {
            _logger = logger;
        }

        public ComicArchiveContents Read(string path, bool readCoverImage = true)
        {
            var extension = Path.GetExtension(path);

            switch (extension?.ToLowerInvariant())
            {
                case ".cbz":
                    return ReadZip(path, readCoverImage);
                case ".cbr":
                    return ReadRar(path, readCoverImage);
                case ".pdf":
                    return ReadPdf(path);
                default:
                    throw new NotSupportedException($"'{extension}' is not a supported comic archive format");
            }
        }

        private ComicArchiveContents ReadZip(string path, bool readCoverImage)
        {
            using (var zip = ZipFile.OpenRead(path))
            {
                var entries = zip.Entries.Where(e => e.Length > 0).ToList();

                var comicInfoEntry = entries.FirstOrDefault(IsComicInfoEntry);
                var comicInfo = comicInfoEntry == null ? null : ReadComicInfoEntry(() => comicInfoEntry.Open());
                var imageEntries = entries.Where(e => ImageExtensions.Contains(Path.GetExtension(e.FullName)))
                                           .OrderBy(e => e.FullName, StringComparer.OrdinalIgnoreCase)
                                           .ToList();

                var result = new ComicArchiveContents
                {
                    ComicInfo = comicInfo,
                    PageCount = comicInfo?.PageCount ?? imageEntries.Count
                };

                if (readCoverImage && imageEntries.Count > 0)
                {
                    var cover = imageEntries.First();
                    using (var stream = cover.Open())
                    using (var memory = new MemoryStream())
                    {
                        stream.CopyTo(memory);
                        result.CoverImage = memory.ToArray();
                        result.CoverImageExtension = Path.GetExtension(cover.FullName);
                    }
                }

                return result;
            }
        }

        private ComicArchiveContents ReadRar(string path, bool readCoverImage)
        {
            using (var archive = RarArchive.Open(path))
            {
                var entries = archive.Entries.Where(e => !e.IsDirectory).ToList();

                var comicInfoEntry = entries.FirstOrDefault(e => IsComicInfoName(e.Key));
                ComicInfo comicInfo = null;
                if (comicInfoEntry != null)
                {
                    using (var stream = comicInfoEntry.OpenEntryStream())
                    using (var reader = new StreamReader(stream))
                    {
                        comicInfo = ComicInfoReader.Parse(reader.ReadToEnd());
                    }
                }

                var imageEntries = entries.Where(e => ImageExtensions.Contains(Path.GetExtension(e.Key)))
                                           .OrderBy(e => e.Key, StringComparer.OrdinalIgnoreCase)
                                           .ToList();

                var result = new ComicArchiveContents
                {
                    ComicInfo = comicInfo,
                    PageCount = comicInfo?.PageCount ?? imageEntries.Count
                };

                if (readCoverImage && imageEntries.Count > 0)
                {
                    var cover = imageEntries.First();
                    using (var stream = cover.OpenEntryStream())
                    using (var memory = new MemoryStream())
                    {
                        stream.CopyTo(memory);
                        result.CoverImage = memory.ToArray();
                        result.CoverImageExtension = Path.GetExtension(cover.Key);
                    }
                }

                return result;
            }
        }

        private ComicArchiveContents ReadPdf(string path)
        {
            // Deliberate scope decision: PDFs get page-count only, no cover extraction. Rendering a PDF
            // page to a raster cover image needs a real rendering engine (PdfSharpCore only reads/writes
            // PDF structure, it doesn't rasterize pages), and pulling in a rendering dependency isn't
            // justified for what is a minority format in comic distribution compared to cbz/cbr.
            var result = new ComicArchiveContents();

            try
            {
                using (var document = PdfReader.Open(path, PdfDocumentOpenMode.InformationOnly))
                {
                    result.PageCount = document.PageCount;
                }
            }
            catch (Exception ex)
            {
                _logger.Debug(ex, $"Unable to read page count from PDF '{path}'");
            }

            return result;
        }

        private static bool IsComicInfoEntry(ZipArchiveEntry entry)
        {
            return IsComicInfoName(entry.FullName);
        }

        private static bool IsComicInfoName(string entryName)
        {
            var name = entryName.Replace('\\', '/');
            return name.Equals("ComicInfo.xml", StringComparison.OrdinalIgnoreCase) ||
                   name.EndsWith("/ComicInfo.xml", StringComparison.OrdinalIgnoreCase);
        }

        private static ComicInfo ReadComicInfoEntry(Func<Stream> openEntry)
        {
            if (openEntry == null)
            {
                return null;
            }

            using (var stream = openEntry())
            using (var reader = new StreamReader(stream))
            {
                return ComicInfoReader.Parse(reader.ReadToEnd());
            }
        }
    }
}
