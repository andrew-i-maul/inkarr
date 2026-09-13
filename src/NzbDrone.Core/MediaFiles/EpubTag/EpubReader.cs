using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using VersOne.Epub.Internal;

namespace VersOne.Epub
{
    public static class EpubReader
    {
        /// <summary>
        /// Opens the issue synchronously without reading its whole content. Holds the handle to the EPUB file.
        /// </summary>
        /// <param name="filePath">path to the EPUB file</param>
        /// <returns></returns>
        public static EpubIssueRef OpenIssue(string filePath)
        {
            return OpenIssueAsync(filePath).Result;
        }

        /// <summary>
        /// Opens the issue asynchronously without reading its whole content. Holds the handle to the EPUB file.
        /// </summary>
        /// <param name="filePath">path to the EPUB file</param>
        /// <returns></returns>
        public static Task<EpubIssueRef> OpenIssueAsync(string filePath)
        {
            if (!File.Exists(filePath))
            {
                if (!filePath.StartsWith(@"\\?\"))
                {
                    filePath = @"\\?\" + filePath;
                }

                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException("Specified epub file not found.", filePath);
                }
            }

            return OpenIssueAsync(GetZipArchive(filePath));
        }

        private static async Task<EpubIssueRef> OpenIssueAsync(ZipArchive zipArchive, string filePath = null)
        {
            EpubIssueRef result = null;
            try
            {
                result = new EpubIssueRef(zipArchive);
                result.FilePath = filePath;
                result.Schema = await SchemaReader.ReadSchemaAsync(zipArchive).ConfigureAwait(false);
                result.Title = result.Schema.Package.Metadata.Titles.FirstOrDefault() ?? string.Empty;
                result.VolumeList = result.Schema.Package.Metadata.Creators.Select(creator => creator.Creator).ToList();
                result.Volume = string.Join(", ", result.VolumeList);
                return result;
            }
            catch
            {
                result?.Dispose();
                throw;
            }
        }

        private static ZipArchive GetZipArchive(string filePath)
        {
            return ZipFile.OpenRead(filePath);
        }
    }
}
