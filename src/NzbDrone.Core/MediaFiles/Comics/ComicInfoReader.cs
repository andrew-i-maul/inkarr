using System.Xml.Linq;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Core.MediaFiles.Comics
{
    public static class ComicInfoReader
    {
        // Parses via XDocument rather than XmlSerializer: real-world ComicInfo.xml files from various
        // taggers don't reliably order elements the way XmlSerializer's default sequential matching expects,
        // so we read known child elements by name instead, tolerating missing/extra/reordered elements.
        public static ComicInfo Parse(string xml)
        {
            if (xml.IsNullOrWhiteSpace())
            {
                return null;
            }

            var doc = XDocument.Parse(xml);
            var root = doc.Root;

            if (root == null || root.Name.LocalName != "ComicInfo")
            {
                return null;
            }

            return new ComicInfo
            {
                Title = Element(root, "Title"),
                Series = Element(root, "Series"),
                Number = Element(root, "Number"),
                Count = IntElement(root, "Count"),
                Volume = Element(root, "Volume"),
                Summary = Element(root, "Summary"),
                Notes = Element(root, "Notes"),
                Year = IntElement(root, "Year"),
                Month = IntElement(root, "Month"),
                Day = IntElement(root, "Day"),
                Writer = Element(root, "Writer"),
                Penciller = Element(root, "Penciller"),
                Inker = Element(root, "Inker"),
                Colorist = Element(root, "Colorist"),
                Letterer = Element(root, "Letterer"),
                CoverArtist = Element(root, "CoverArtist"),
                Editor = Element(root, "Editor"),
                Publisher = Element(root, "Publisher"),
                Imprint = Element(root, "Imprint"),
                Genre = Element(root, "Genre"),
                Web = Element(root, "Web"),
                PageCount = IntElement(root, "PageCount"),
                LanguageISO = Element(root, "LanguageISO"),
                Format = Element(root, "Format"),
                AgeRating = Element(root, "AgeRating"),
                StoryArc = Element(root, "StoryArc"),
                StoryArcNumber = Element(root, "StoryArcNumber"),
                GTIN = Element(root, "GTIN")
            };
        }

        private static string Element(XElement root, string name)
        {
            var value = root.Element(name)?.Value;
            return value.IsNullOrWhiteSpace() ? null : value.Trim();
        }

        private static int? IntElement(XElement root, string name)
        {
            var value = Element(root, name);
            return value != null && int.TryParse(value, out var result) ? result : (int?)null;
        }
    }
}
