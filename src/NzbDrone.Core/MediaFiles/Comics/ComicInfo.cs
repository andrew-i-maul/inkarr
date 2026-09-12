namespace NzbDrone.Core.MediaFiles.Comics
{
    // Fields per the community ComicInfo.xml schema (https://github.com/anansi-project/comicinfo),
    // the de facto metadata standard embedded by ComicRack, Comictagger, etc. inside cbz/cbr archives.
    // Only the subset useful for identification/renaming is modelled here; page-level <Pages> data
    // (per-page Image/Type/DoublePage/ImageSize/ImageWidth/ImageHeight) is not parsed.
    public class ComicInfo
    {
        public string Title { get; set; }
        public string Series { get; set; }
        public string Number { get; set; }
        public int? Count { get; set; }
        public string Volume { get; set; }
        public string Summary { get; set; }
        public string Notes { get; set; }
        public int? Year { get; set; }
        public int? Month { get; set; }
        public int? Day { get; set; }
        public string Writer { get; set; }
        public string Penciller { get; set; }
        public string Inker { get; set; }
        public string Colorist { get; set; }
        public string Letterer { get; set; }
        public string CoverArtist { get; set; }
        public string Editor { get; set; }
        public string Publisher { get; set; }
        public string Imprint { get; set; }
        public string Genre { get; set; }
        public string Web { get; set; }
        public int? PageCount { get; set; }
        public string LanguageISO { get; set; }
        public string Format { get; set; }
        public string AgeRating { get; set; }
        public string StoryArc { get; set; }
        public string StoryArcNumber { get; set; }
        public string GTIN { get; set; }
    }
}
