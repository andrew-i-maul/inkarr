using System.IO;
using System.IO.Compression;
using System.Text;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.MediaFiles.Comics;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MediaFiles.Comics
{
    // Representative ComicInfo.xml content, per the community schema documented at
    // https://github.com/anansi-project/comicinfo (DOCUMENTATION.md, fetched live while building this) —
    // not a real downloaded file, but a realistic example built from that spec's own element reference.
    [TestFixture]
    public class ComicInfoReaderFixture
    {
        private const string ValidComicInfo =
            "<?xml version=\"1.0\"?>\n" +
            "<ComicInfo>\n" +
            "  <Series>Batman</Series>\n" +
            "  <Number>1</Number>\n" +
            "  <Title>I Am Gotham, Part One</Title>\n" +
            "  <Publisher>DC Comics</Publisher>\n" +
            "  <Writer>Tom King</Writer>\n" +
            "  <Penciller>David Finch</Penciller>\n" +
            "  <Year>2016</Year>\n" +
            "  <Month>8</Month>\n" +
            "  <Summary>Batman faces a new threat.</Summary>\n" +
            "  <PageCount>32</PageCount>\n" +
            "  <LanguageISO>en</LanguageISO>\n" +
            "</ComicInfo>";

        [Test]
        public void should_parse_all_mapped_fields()
        {
            var result = ComicInfoReader.Parse(ValidComicInfo);

            result.Should().NotBeNull();
            result.Series.Should().Be("Batman");
            result.Number.Should().Be("1");
            result.Title.Should().Be("I Am Gotham, Part One");
            result.Publisher.Should().Be("DC Comics");
            result.Writer.Should().Be("Tom King");
            result.Year.Should().Be(2016);
            result.Month.Should().Be(8);
            result.PageCount.Should().Be(32);
            result.LanguageISO.Should().Be("en");
        }

        [Test]
        public void should_tolerate_reordered_elements()
        {
            // XmlSerializer's default sequential matching would choke on this; XDocument-based
            // parsing (what ComicInfoReader actually uses) should not.
            var reordered =
                "<ComicInfo>\n" +
                "  <PageCount>22</PageCount>\n" +
                "  <Series>Saga</Series>\n" +
                "  <Writer>Brian K. Vaughan</Writer>\n" +
                "  <Number>1</Number>\n" +
                "</ComicInfo>";

            var result = ComicInfoReader.Parse(reordered);

            result.Should().NotBeNull();
            result.Series.Should().Be("Saga");
            result.PageCount.Should().Be(22);
        }

        [Test]
        public void should_tolerate_missing_optional_elements()
        {
            var minimal = "<ComicInfo><Series>Saga</Series></ComicInfo>";

            var result = ComicInfoReader.Parse(minimal);

            result.Should().NotBeNull();
            result.Series.Should().Be("Saga");
            result.Writer.Should().BeNull();
            result.PageCount.Should().BeNull();
        }

        [Test]
        public void should_return_null_for_non_comicinfo_document()
        {
            var result = ComicInfoReader.Parse("<SomethingElse><Foo>bar</Foo></SomethingElse>");

            result.Should().BeNull();
        }

        [Test]
        public void should_return_null_for_null_or_blank_input()
        {
            ComicInfoReader.Parse(null).Should().BeNull();
            ComicInfoReader.Parse(string.Empty).Should().BeNull();
            ComicInfoReader.Parse("   ").Should().BeNull();
        }

        [Test]
        public void should_ignore_unparseable_numeric_values_rather_than_throw()
        {
            var badNumber = "<ComicInfo><Series>Test</Series><Year>not-a-year</Year></ComicInfo>";

            var result = ComicInfoReader.Parse(badNumber);

            result.Should().NotBeNull();
            result.Year.Should().BeNull();
        }
    }

    // Exercises the real cbz path end to end against an actual zip archive built on disk at test time.
    // The cbr path is NOT covered here: synthesizing a real RAR archive needs proprietary tooling not
    // available in this environment, so cbr support is verified only by successful compilation against
    // SharpCompress's RarArchive API, not by an executable test. Flagging this rather than claiming
    // coverage that doesn't exist.
    [TestFixture]
    public class ComicArchiveReaderCbzFixture : CoreTest<ComicArchiveReader>
    {
        private string _archivePath;

        [TearDown]
        public void TearDown()
        {
            if (_archivePath != null && File.Exists(_archivePath))
            {
                File.Delete(_archivePath);
            }
        }

        private string BuildCbz(string comicInfoXml, params string[] imageEntryNames)
        {
            _archivePath = Path.Combine(Path.GetTempPath(), $"inkarr-test-{Path.GetRandomFileName()}.cbz");

            using (var stream = new FileStream(_archivePath, FileMode.Create))
            using (var zip = new ZipArchive(stream, ZipArchiveMode.Create))
            {
                if (comicInfoXml != null)
                {
                    var entry = zip.CreateEntry("ComicInfo.xml");
                    using (var writer = new StreamWriter(entry.Open()))
                    {
                        writer.Write(comicInfoXml);
                    }
                }

                foreach (var name in imageEntryNames)
                {
                    var entry = zip.CreateEntry(name);
                    using (var entryStream = entry.Open())
                    {
                        var bytes = Encoding.UTF8.GetBytes("fake-image-bytes-" + name);
                        entryStream.Write(bytes, 0, bytes.Length);
                    }
                }
            }

            return _archivePath;
        }

        [Test]
        public void should_read_comicinfo_and_page_count_from_cbz()
        {
            var path = BuildCbz(
                "<ComicInfo><Series>Batman</Series><Number>1</Number><PageCount>3</PageCount></ComicInfo>",
                "002.jpg",
                "001.jpg",
                "003.jpg");

            var result = Subject.Read(path);

            result.ComicInfo.Should().NotBeNull();
            result.ComicInfo.Series.Should().Be("Batman");
            result.PageCount.Should().Be(3);
        }

        [Test]
        public void should_pick_first_image_by_name_order_as_cover()
        {
            var path = BuildCbz(null, "003.jpg", "001.jpg", "002.jpg");

            var result = Subject.Read(path, readCoverImage: true);

            result.CoverImage.Should().NotBeNull();
            Encoding.UTF8.GetString(result.CoverImage).Should().Be("fake-image-bytes-001.jpg");
        }

        [Test]
        public void should_fall_back_to_image_count_when_no_comicinfo_page_count()
        {
            var path = BuildCbz(null, "001.jpg", "002.jpg");

            var result = Subject.Read(path, readCoverImage: false);

            result.ComicInfo.Should().BeNull();
            result.PageCount.Should().Be(2);
            result.CoverImage.Should().BeNull();
        }
    }
}
