using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Issues.Calibre;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles.IssueImport.Identification
{
    public static class DistanceCalculator
    {
        private static readonly Logger Logger = NzbDroneLogger.GetLogger(typeof(DistanceCalculator));

        public static readonly List<string> VariousVolumeIds = new List<string> { "89ad4ac3-39f7-470e-963a-56509c546377" };

        private static readonly RegexReplace StripSeriesRegex = new RegexReplace(@"\([^\)].+?\)$", string.Empty, RegexOptions.Compiled);

        private static readonly RegexReplace CleanTitleCruft = new RegexReplace(@"\((?:unabridged)\)", string.Empty, RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly List<string> EissueFormats = new List<string> { "Kindle Edition", "Nook", "eissue" };

        private static readonly List<string> AudioissueFormats = new List<string> { "Audioissue", "Audio CD", "Audio Cassette", "Audible Audio", "CD-ROM", "MP3 CD" };

        public static Distance IssueDistance(List<LocalIssue> localTracks, Edition edition)
        {
            var dist = new Distance();

            // the most common list of volumes reported by a file
            var fileVolumes = localTracks.Select(x => x.FileTrackInfo.Volumes.Where(a => a.IsNotNullOrWhiteSpace()).ToList())
                .GroupBy(x => x.ConcatToString())
                .OrderByDescending(x => x.Count())
                .First()
                .First();

            var volumes = GetVolumeVariants(fileVolumes);

            dist.AddString("volume", volumes, edition.Issue.Value.VolumeMetadata.Value.Name);
            Logger.Trace("volume: '{0}' vs '{1}'; {2}", volumes.ConcatToString("' or '"), edition.Issue.Value.VolumeMetadata.Value.Name, dist.NormalizedDistance());

            var title = localTracks.MostCommon(x => x.FileTrackInfo.IssueTitle) ?? "";
            var titleOptions = new List<string> { edition.Title };
            if (titleOptions[0].Contains("#"))
            {
                titleOptions.Add(StripSeriesRegex.Replace(titleOptions[0]));
            }

            var (maintitle, _) = edition.Title.SplitIssueTitle(edition.Issue.Value.VolumeMetadata.Value.Name);
            if (!titleOptions.Contains(maintitle))
            {
                titleOptions.Add(maintitle);
            }

            if (edition.Issue.Value.SeriesLinks?.Value?.Any() ?? false)
            {
                foreach (var l in edition.Issue.Value.SeriesLinks.Value)
                {
                    if (l.Series?.Value?.Title?.IsNotNullOrWhiteSpace() ?? false)
                    {
                        titleOptions.Add($"{l.Series.Value.Title} {l.Position} {edition.Title}");
                        titleOptions.Add($"{l.Series.Value.Title} Issue {l.Position} {edition.Title}");
                        titleOptions.Add($"{edition.Title} {l.Series.Value.Title} {l.Position}");
                        titleOptions.Add($"{edition.Title} {l.Series.Value.Title} Issue {l.Position}");
                    }
                }
            }

            var fileTitles = new[] { title, CleanTitleCruft.Replace(title) }.Distinct().ToList();

            dist.AddString("issue", fileTitles, titleOptions);
            Logger.Trace("issue: '{0}' vs '{1}'; {2}", fileTitles.ConcatToString("' or '"), titleOptions.ConcatToString("' or '"), dist.NormalizedDistance());

            var isbn = localTracks.MostCommon(x => x.FileTrackInfo.Isbn);
            if (isbn.IsNotNullOrWhiteSpace() && edition.Isbn13.IsNotNullOrWhiteSpace())
            {
                dist.AddBool("isbn", isbn != edition.Isbn13);
                Logger.Trace("isbn: '{0}' vs '{1}'; {2}", isbn, edition.Isbn13, dist.NormalizedDistance());
            }
            else if (isbn.IsNullOrWhiteSpace() != edition.Isbn13.IsNullOrWhiteSpace())
            {
                dist.AddBool("isbn_missing", true);
                Logger.Trace("isbn: '{0}' vs '{1}'; {2}", isbn, edition.Isbn13, dist.NormalizedDistance());
            }

            var asin = localTracks.MostCommon(x => x.FileTrackInfo.Asin);
            if (asin.IsNotNullOrWhiteSpace() && edition.Asin.IsNotNullOrWhiteSpace())
            {
                dist.AddBool("asin", asin != edition.Asin);
                Logger.Trace("asin: '{0}' vs '{1}'; {2}", asin, edition.Asin, dist.NormalizedDistance());
            }
            else if (asin.IsNullOrWhiteSpace() != edition.Asin.IsNullOrWhiteSpace())
            {
                dist.AddBool("asin_missing", true);
                Logger.Trace("asin: '{0}' vs '{1}'; {2}", asin, edition.Asin, dist.NormalizedDistance());
            }

            // Year
            var localYear = localTracks.MostCommon(x => x.FileTrackInfo.Year);
            if (localYear > 0 && edition.ReleaseDate.HasValue)
            {
                var issueYear = edition.ReleaseDate?.Year ?? 0;
                if (localYear == issueYear)
                {
                    dist.Add("year", 0.0);
                }
                else
                {
                    var remoteYear = issueYear;
                    var diff = Math.Abs(localYear - remoteYear);
                    var diff_max = Math.Abs(DateTime.Now.Year - remoteYear);
                    dist.AddRatio("year", diff, diff_max);
                }

                Logger.Trace($"year: {localYear} vs {edition.ReleaseDate?.Year}; {dist.NormalizedDistance()}");
            }

            // Language - only if set for both the local issue and remote edition
            var localLanguage = localTracks.MostCommon(x => x.FileTrackInfo.Language).CanonicalizeLanguage();
            var editionLanguage = edition.Language.CanonicalizeLanguage();
            if (localLanguage.IsNotNullOrWhiteSpace() && editionLanguage.IsNotNullOrWhiteSpace())
            {
                dist.AddBool("language", localLanguage != editionLanguage);
                Logger.Trace($"language: {localLanguage} vs {editionLanguage}; {dist.NormalizedDistance()}");
            }

            // Publisher - only if set for both the local issue and remote edition
            var localPublisher = localTracks.MostCommon(x => x.FileTrackInfo.Publisher);
            var editionPublisher = edition.Publisher;
            if (localPublisher.IsNotNullOrWhiteSpace() && editionPublisher.IsNotNullOrWhiteSpace())
            {
                dist.AddString("publisher", localPublisher, editionPublisher);
                Logger.Trace($"publisher: {localPublisher} vs {editionPublisher}; {dist.NormalizedDistance()}");
            }

            // try to tilt it towards the correct "type" of release
            var isAudio = MediaFileExtensions.AudioExtensions.Contains(localTracks.First().Path.GetPathExtension());

            if (edition.Format.IsNotNullOrWhiteSpace())
            {
                if (!isAudio)
                {
                    // text issues should prefer eissue formats
                    dist.AddBool("eissue_format", !EissueFormats.Contains(edition.Format));

                    // text issues should not match audio entries
                    dist.AddBool("wrong_format", AudioissueFormats.Contains(edition.Format));
                }
                else
                {
                    // audio issues should prefer audio formats
                    dist.AddBool("audio_format", !AudioissueFormats.Contains(edition.Format));
                }
            }

            return dist;
        }

        public static List<string> GetVolumeVariants(List<string> fileVolumes)
        {
            var volumes = new List<string>(fileVolumes);

            if (fileVolumes.Count == 1)
            {
                volumes.AddRange(SplitVolume(fileVolumes[0]));
            }

            foreach (var volume in fileVolumes)
            {
                if (volume.Contains(','))
                {
                    var split = volume.Split(',', 2).Select(x => x.Trim());
                    if (!split.First().Contains(' '))
                    {
                        volumes.Add(split.Reverse().ConcatToString(" "));
                    }
                }
            }

            return volumes;
        }

        private static List<string> SplitVolume(string input)
        {
            var seps = new[] { ';', '/' };
            foreach (var sep in seps)
            {
                if (input.Contains(sep))
                {
                    return input.Split(sep).Select(x => x.Trim()).ToList();
                }
            }

            var andSeps = new List<string> { " and ", " & " };
            foreach (var sep in andSeps)
            {
                if (input.Contains(sep))
                {
                    var result = new List<string>();
                    foreach (var s in input.Split(sep).Select(x => x.Trim()))
                    {
                        var s2 = SplitVolume(s);
                        if (s2.Any())
                        {
                            result.AddRange(s2);
                        }
                        else
                        {
                            result.Add(s);
                        }
                    }

                    return result;
                }
            }

            if (input.Contains(','))
            {
                var split = input.Split(',').Select(x => x.Trim()).ToList();
                if (split[0].Contains(' '))
                {
                    return split;
                }
            }

            return new List<string>();
        }
    }
}
