using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Common.EnsureThat;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.Organizer
{
    public interface IBuildFileNames
    {
        string BuildIssueFileName(Volume volume, Edition edition, IssueFile issueFile, NamingConfig namingConfig = null, List<CustomFormat> customFormats = null);
        string BuildIssueFilePath(Volume volume, Edition edition, string fileName, string extension);
        string BuildIssuePath(Volume volume);
        BasicNamingConfig GetBasicNamingConfig(NamingConfig nameSpec);
        string GetVolumeFolder(Volume volume, NamingConfig namingConfig = null);
    }

    public class FileNameBuilder : IBuildFileNames
    {
        private readonly INamingConfigService _namingConfigService;
        private readonly IQualityDefinitionService _qualityDefinitionService;
        private readonly ICustomFormatCalculationService _formatCalculator;
        private readonly ICached<IssueFormat[]> _trackFormatCache;
        private readonly Logger _logger;

        private static readonly Regex TitleRegex = new Regex(@"\{(?<prefix>[- ._\[(]*)(?<token>(?:[a-z0-9]+)(?:(?<separator>[- ._]+)(?:[a-z0-9]+))?)(?::(?<customFormat>[a-z0-9]+))?(?<suffix>[- ._)\]]*)\}",
                                                             RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static readonly Regex PartRegex = new Regex(@"\{(?<prefix>[^{]*?)(?<token1>PartNumber|PartCount)(?::(?<customFormat1>[a-z0-9]+))?(?<separator>.*(?=PartNumber|PartCount))?((?<token2>PartNumber|PartCount)(?::(?<customFormat2>[a-z0-9]+))?)?(?<suffix>[^}]*)\}",
                                                            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static readonly Regex SeasonEpisodePatternRegex = new Regex(@"(?<separator>(?<=})[- ._]+?)?(?<seasonEpisode>s?{season(?:\:0+)?}(?<episodeSeparator>[- ._]?[ex])(?<episode>{episode(?:\:0+)?}))(?<separator>[- ._]+?(?={))?",
                                                                            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static readonly Regex VolumeNameRegex = new Regex(@"(?<token>\{(?:Volume)(?<separator>[- ._])(Clean)?(Sort)?Name(The)?\})",
                                                                            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static readonly Regex IssueTitleRegex = new Regex(@"(?<token>\{(?:Issue)(?<separator>[- ._])(Clean)?Title(The)?(NoSub)?\})",
                                                                            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex FileNameCleanupRegex = new Regex(@"([- ._])(\1)+", RegexOptions.Compiled);
        private static readonly Regex TrimSeparatorsRegex = new Regex(@"[- ._]$", RegexOptions.Compiled);

        private static readonly Regex ScenifyRemoveChars = new Regex(@"(?<=\s)(,|<|>|\/|\\|;|:|'|""|\||`|~|!|\?|@|$|%|^|\*|-|_|=){1}(?=\s)|('|:|\?|,)(?=(?:(?:s|m)\s)|\s|$)|(\(|\)|\[|\]|\{|\})", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex ScenifyReplaceChars = new Regex(@"[\/]", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex TitlePrefixRegex = new Regex(@"^(The|An|A) (.*?)((?: *\([^)]+\))*)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public FileNameBuilder(INamingConfigService namingConfigService,
                               IQualityDefinitionService qualityDefinitionService,
                               ICacheManager cacheManager,
                               ICustomFormatCalculationService formatCalculator,
                               Logger logger)
        {
            _namingConfigService = namingConfigService;
            _qualityDefinitionService = qualityDefinitionService;
            _formatCalculator = formatCalculator;
            _trackFormatCache = cacheManager.GetCache<IssueFormat[]>(GetType(), "issueFormat");
            _logger = logger;
        }

        public string BuildIssueFileName(Volume volume, Edition edition, IssueFile issueFile, NamingConfig namingConfig = null, List<CustomFormat> customFormats = null)
        {
            if (namingConfig == null)
            {
                namingConfig = _namingConfigService.GetConfig();
            }

            if (!namingConfig.RenameIssues)
            {
                return GetOriginalFileName(issueFile);
            }

            if (namingConfig.StandardIssueFormat.IsNullOrWhiteSpace())
            {
                throw new NamingFormatException("File name format cannot be empty");
            }

            var pattern = namingConfig.StandardIssueFormat;

            var tokenHandlers = new Dictionary<string, Func<TokenMatch, string>>(FileNameBuilderTokenEqualityComparer.Instance);

            AddVolumeTokens(tokenHandlers, volume);
            AddIssueTokens(tokenHandlers, edition);
            AddIssueFileTokens(tokenHandlers, issueFile);
            AddQualityTokens(tokenHandlers, volume, issueFile);
            AddMediaInfoTokens(tokenHandlers, issueFile);
            AddCustomFormats(tokenHandlers, volume, issueFile, customFormats);

            var splitPatterns = pattern.Split(new char[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
            var components = new List<string>();

            foreach (var s in splitPatterns)
            {
                var splitPattern = s;

                var component = ReplacePartTokens(splitPattern, tokenHandlers, namingConfig).Trim();
                component = ReplaceTokens(component, tokenHandlers, namingConfig).Trim();

                component = FileNameCleanupRegex.Replace(component, match => match.Captures[0].Value[0].ToString());
                component = TrimSeparatorsRegex.Replace(component, string.Empty);

                if (component.IsNotNullOrWhiteSpace())
                {
                    components.Add(component);
                }
            }

            return Path.Combine(components.ToArray());
        }

        public string BuildIssueFilePath(Volume volume, Edition edition, string fileName, string extension)
        {
            Ensure.That(extension, () => extension).IsNotNullOrWhiteSpace();

            var path = BuildIssuePath(volume);

            return Path.Combine(path, fileName + extension);
        }

        public string BuildIssuePath(Volume volume)
        {
            return volume.Path;
        }

        public BasicNamingConfig GetBasicNamingConfig(NamingConfig nameSpec)
        {
            var trackFormat = GetTrackFormat(nameSpec.StandardIssueFormat).LastOrDefault();

            if (trackFormat == null)
            {
                return new BasicNamingConfig();
            }

            var basicNamingConfig = new BasicNamingConfig
            {
                Separator = trackFormat.Separator
            };

            var titleTokens = TitleRegex.Matches(nameSpec.StandardIssueFormat);

            foreach (Match match in titleTokens)
            {
                var separator = match.Groups["separator"].Value;
                var token = match.Groups["token"].Value;

                if (!separator.Equals(" "))
                {
                    basicNamingConfig.ReplaceSpaces = true;
                }

                if (token.StartsWith("{Volume", StringComparison.InvariantCultureIgnoreCase))
                {
                    basicNamingConfig.IncludeVolumeName = true;
                }

                if (token.StartsWith("{Issue", StringComparison.InvariantCultureIgnoreCase))
                {
                    basicNamingConfig.IncludeIssueTitle = true;
                }

                if (token.StartsWith("{Quality", StringComparison.InvariantCultureIgnoreCase))
                {
                    basicNamingConfig.IncludeQuality = true;
                }
            }

            return basicNamingConfig;
        }

        public string GetVolumeFolder(Volume volume, NamingConfig namingConfig = null)
        {
            if (namingConfig == null)
            {
                namingConfig = _namingConfigService.GetConfig();
            }

            var pattern = namingConfig.VolumeFolderFormat;
            var tokenHandlers = new Dictionary<string, Func<TokenMatch, string>>(FileNameBuilderTokenEqualityComparer.Instance);

            AddVolumeTokens(tokenHandlers, volume);

            var splitPatterns = pattern.Split(new char[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
            var components = new List<string>();

            foreach (var s in splitPatterns)
            {
                var splitPattern = s;

                var component = ReplaceTokens(splitPattern, tokenHandlers, namingConfig);
                component = CleanFolderName(component);

                if (component.IsNotNullOrWhiteSpace())
                {
                    components.Add(component);
                }
            }

            return Path.Combine(components.ToArray());
        }

        public static string CleanTitle(string title)
        {
            title = title.Replace("&", "and");
            title = ScenifyReplaceChars.Replace(title, " ");
            title = ScenifyRemoveChars.Replace(title, string.Empty);

            return title;
        }

        public static string TitleThe(string title)
        {
            return TitlePrefixRegex.Replace(title, "$2, $1$3");
        }

        public static string CleanFileName(string name)
        {
            return CleanFileName(name, NamingConfig.Default);
        }

        public static string CleanFolderName(string name)
        {
            name = FileNameCleanupRegex.Replace(name, match => match.Captures[0].Value[0].ToString());

            return name.Trim(' ', '.');
        }

        private void AddVolumeTokens(Dictionary<string, Func<TokenMatch, string>> tokenHandlers, Volume volume)
        {
            tokenHandlers["{Volume Name}"] = m => volume.Name;
            tokenHandlers["{Volume CleanName}"] = m => CleanTitle(volume.Name);
            tokenHandlers["{Volume NameThe}"] = m => TitleThe(volume.Name);
            tokenHandlers["{Volume SortName}"] = m => volume?.Metadata?.Value?.NameLastFirst ?? string.Empty;
            tokenHandlers["{Volume NameFirstCharacter}"] = m => TitleThe(volume.Name).Substring(0, 1).FirstCharToUpper();

            if (volume.Metadata.Value.Disambiguation != null)
            {
                tokenHandlers["{Volume Disambiguation}"] = m => volume.Metadata.Value.Disambiguation;
            }
        }

        private void AddIssueTokens(Dictionary<string, Func<TokenMatch, string>> tokenHandlers, Edition edition)
        {
            tokenHandlers["{Issue Title}"] = m => edition.Title;
            tokenHandlers["{Issue CleanTitle}"] = m => CleanTitle(edition.Title);
            tokenHandlers["{Issue TitleThe}"] = m => TitleThe(edition.Title);

            var (titleNoSub, subtitle) = edition.Title.SplitIssueTitle(edition.Issue.Value.VolumeMetadata.Value.Name);

            tokenHandlers["{Issue TitleNoSub}"] = m => titleNoSub;
            tokenHandlers["{Issue CleanTitleNoSub}"] = m => CleanTitle(titleNoSub);
            tokenHandlers["{Issue TitleTheNoSub}"] = m => TitleThe(titleNoSub);

            tokenHandlers["{Issue Subtitle}"] = m => subtitle;
            tokenHandlers["{Issue CleanSubtitle}"] = m => CleanTitle(subtitle);
            tokenHandlers["{Issue SubtitleThe}"] = m => TitleThe(subtitle);

            var seriesLinks = edition.Issue.Value.SeriesLinks.Value;
            if (seriesLinks.Any())
            {
                var primarySeries = seriesLinks.OrderBy(x => x.SeriesPosition).First();
                var seriesTitle = primarySeries.Series?.Value?.Title + (primarySeries.Position.IsNotNullOrWhiteSpace() ? $" #{primarySeries.Position}" : string.Empty);

                tokenHandlers["{Issue Series}"] = m => primarySeries.Series.Value.Title;
                tokenHandlers["{Issue SeriesPosition}"] = m => primarySeries.Position;
                tokenHandlers["{Issue SeriesTitle}"] = m => seriesTitle;
            }

            if (edition.Disambiguation != null)
            {
                tokenHandlers["{Issue Disambiguation}"] = m => edition.Disambiguation;
            }

            if (edition.ReleaseDate.HasValue)
            {
                tokenHandlers["{Release Year}"] = m => edition.ReleaseDate.Value.Year.ToString();
            }
            else if (edition.Issue.Value.ReleaseDate.HasValue)
            {
                tokenHandlers["{Release Year}"] = m => edition.Issue.Value.ReleaseDate.Value.Year.ToString();
            }
            else
            {
                tokenHandlers["{Release Year}"] = m => "Unknown";
            }

            if (edition.ReleaseDate.HasValue)
            {
                tokenHandlers["{Edition Year}"] = m => edition.ReleaseDate.Value.Year.ToString();
            }
            else
            {
                tokenHandlers["{Edition Year}"] = m => "Unknown";
            }

            if (edition.Issue.Value.ReleaseDate.HasValue)
            {
                tokenHandlers["{Release YearFirst}"] = m => edition.Issue.Value.ReleaseDate.Value.Year.ToString();
            }
            else
            {
                tokenHandlers["{Release YearFirst}"] = m => "Unknown";
            }
        }

        private void AddIssueFileTokens(Dictionary<string, Func<TokenMatch, string>> tokenHandlers, IssueFile issueFile)
        {
            tokenHandlers["{Original Title}"] = m => GetOriginalTitle(issueFile);
            tokenHandlers["{Original Filename}"] = m => GetOriginalFileName(issueFile);
            tokenHandlers["{Release Group}"] = m => issueFile.ReleaseGroup ?? m.DefaultValue("Inkarr");

            if (issueFile.PartCount > 1)
            {
                tokenHandlers["{PartNumber}"] = m => issueFile.Part.ToString(m.CustomFormat);
                tokenHandlers["{PartCount}"] = m => issueFile.PartCount.ToString(m.CustomFormat);
            }
        }

        private void AddQualityTokens(Dictionary<string, Func<TokenMatch, string>> tokenHandlers, Volume volume, IssueFile issueFile)
        {
            var qualityTitle = _qualityDefinitionService.Get(issueFile.Quality.Quality).Title;
            var qualityProper = GetQualityProper(issueFile.Quality);

            //var qualityReal = GetQualityReal(volume, issueFile.Quality);
            tokenHandlers["{Quality Full}"] = m => string.Format("{0}", qualityTitle);
            tokenHandlers["{Quality Title}"] = m => qualityTitle;
            tokenHandlers["{Quality Proper}"] = m => qualityProper;

            //tokenHandlers["{Quality Real}"] = m => qualityReal;
        }

        private void AddMediaInfoTokens(Dictionary<string, Func<TokenMatch, string>> tokenHandlers, IssueFile issueFile)
        {
            if (issueFile.MediaInfo == null)
            {
                _logger.Trace("Media info is unavailable for {0}", issueFile);

                return;
            }

            var audioCodec = MediaInfoFormatter.FormatAudioCodec(issueFile.MediaInfo);
            var audioChannels = MediaInfoFormatter.FormatAudioChannels(issueFile.MediaInfo);
            var audioChannelsFormatted = audioChannels > 0 ?
                                audioChannels.ToString("F1", CultureInfo.InvariantCulture) :
                                string.Empty;

            tokenHandlers["{MediaInfo AudioCodec}"] = m => audioCodec;
            tokenHandlers["{MediaInfo AudioChannels}"] = m => audioChannelsFormatted;
            tokenHandlers["{MediaInfo AudioBitRate}"] = m => MediaInfoFormatter.FormatAudioBitrate(issueFile.MediaInfo);
            tokenHandlers["{MediaInfo AudioBitsPerSample}"] = m => MediaInfoFormatter.FormatAudioBitsPerSample(issueFile.MediaInfo);
            tokenHandlers["{MediaInfo AudioSampleRate}"] = m => MediaInfoFormatter.FormatAudioSampleRate(issueFile.MediaInfo);
        }

        private void AddCustomFormats(Dictionary<string, Func<TokenMatch, string>> tokenHandlers, Volume volume, IssueFile issueFile, List<CustomFormat> customFormats = null)
        {
            if (customFormats == null)
            {
                issueFile.Volume = volume;
                customFormats = _formatCalculator.ParseCustomFormat(issueFile, volume);
            }

            tokenHandlers["{Custom Formats}"] = m => string.Join(" ", customFormats.Where(x => x.IncludeCustomFormatWhenRenaming));
        }

        private string ReplaceTokens(string pattern, Dictionary<string, Func<TokenMatch, string>> tokenHandlers, NamingConfig namingConfig)
        {
            return TitleRegex.Replace(pattern, match => ReplaceToken(match, tokenHandlers, namingConfig));
        }

        private string ReplaceToken(Match match, Dictionary<string, Func<TokenMatch, string>> tokenHandlers, NamingConfig namingConfig)
        {
            var tokenMatch = new TokenMatch
            {
                RegexMatch = match,
                Prefix = match.Groups["prefix"].Value,
                Separator = match.Groups["separator"].Value,
                Suffix = match.Groups["suffix"].Value,
                Token = match.Groups["token"].Value,
                CustomFormat = match.Groups["customFormat"].Value
            };

            if (tokenMatch.CustomFormat.IsNullOrWhiteSpace())
            {
                tokenMatch.CustomFormat = null;
            }

            var tokenHandler = tokenHandlers.GetValueOrDefault(tokenMatch.Token, m => string.Empty);

            var replacementText = tokenHandler(tokenMatch).Trim();

            if (tokenMatch.Token.All(t => !char.IsLetter(t) || char.IsLower(t)))
            {
                replacementText = replacementText.ToLower();
            }
            else if (tokenMatch.Token.All(t => !char.IsLetter(t) || char.IsUpper(t)))
            {
                replacementText = replacementText.ToUpper();
            }

            if (!tokenMatch.Separator.IsNullOrWhiteSpace())
            {
                replacementText = replacementText.Replace(" ", tokenMatch.Separator);
            }

            replacementText = CleanFileName(replacementText, namingConfig);

            if (!replacementText.IsNullOrWhiteSpace())
            {
                replacementText = tokenMatch.Prefix + replacementText + tokenMatch.Suffix;
            }

            return replacementText;
        }

        private string ReplacePartTokens(string pattern, Dictionary<string, Func<TokenMatch, string>> tokenHandlers, NamingConfig namingConfig)
        {
            return PartRegex.Replace(pattern, match => ReplacePartToken(match, tokenHandlers, namingConfig));
        }

        private string ReplacePartToken(Match match, Dictionary<string, Func<TokenMatch, string>> tokenHandlers, NamingConfig namingConfig)
        {
            var tokenHandler = tokenHandlers.GetValueOrDefault($"{{{match.Groups["token1"].Value}}}", m => string.Empty);

            var tokenText1 = tokenHandler(new TokenMatch { CustomFormat = match.Groups["customFormat1"].Success ? match.Groups["customFormat1"].Value : "0" });

            if (tokenText1 == string.Empty)
            {
                return string.Empty;
            }

            var prefix = match.Groups["prefix"].Value;

            var tokenText2 = string.Empty;

            var separator = match.Groups["separator"].Success ? match.Groups["separator"].Value : string.Empty;

            var suffix = match.Groups["suffix"].Value;

            if (match.Groups["token2"].Success)
            {
                tokenHandler = tokenHandlers.GetValueOrDefault($"{{{match.Groups["token2"].Value}}}", m => string.Empty);

                tokenText2 = tokenHandler(new TokenMatch { CustomFormat = match.Groups["customFormat2"].Success ? match.Groups["customFormat2"].Value : "0" });
            }

            return $"{prefix}{tokenText1}{separator}{tokenText2}{suffix}";
        }

        private IssueFormat[] GetTrackFormat(string pattern)
        {
            return _trackFormatCache.Get(pattern, () => SeasonEpisodePatternRegex.Matches(pattern).OfType<Match>()
                .Select(match => new IssueFormat
                {
                    IssueSeparator = match.Groups["episodeSeparator"].Value,
                    Separator = match.Groups["separator"].Value,
                    IssuePattern = match.Groups["episode"].Value,
                }).ToArray());
        }

        private string GetQualityProper(QualityModel quality)
        {
            if (quality.Revision.Version > 1)
            {
                if (quality.Revision.IsRepack)
                {
                    return "Repack";
                }

                return "Proper";
            }

            return string.Empty;
        }

        private string GetOriginalTitle(IssueFile issueFile)
        {
            if (issueFile.SceneName.IsNullOrWhiteSpace())
            {
                return GetOriginalFileName(issueFile);
            }

            return issueFile.SceneName;
        }

        private string GetOriginalFileName(IssueFile issueFile)
        {
            return Path.GetFileNameWithoutExtension(issueFile.Path);
        }

        private static string CleanFileName(string name, NamingConfig namingConfig)
        {
            var result = name;
            string[] badCharacters = { "\\", "/", "<", ">", "?", "*", "|", "\"" };
            string[] goodCharacters = { "+", "+", "", "", "!", "-", "", "" };

            if (namingConfig.ReplaceIllegalCharacters)
            {
                // Smart replaces a colon followed by a space with space dash space for a better appearance
                if (namingConfig.ColonReplacementFormat == ColonReplacementFormat.Smart)
                {
                    result = result.Replace(": ", " - ");
                    result = result.Replace(":", "-");
                }
                else
                {
                    var replacement = string.Empty;

                    switch (namingConfig.ColonReplacementFormat)
                    {
                        case ColonReplacementFormat.Dash:
                            replacement = "-";
                            break;
                        case ColonReplacementFormat.SpaceDash:
                            replacement = " -";
                            break;
                        case ColonReplacementFormat.SpaceDashSpace:
                            replacement = " - ";
                            break;
                    }

                    result = result.Replace(":", replacement);
                }
            }
            else
            {
                result = result.Replace(":", string.Empty);
            }

            for (var i = 0; i < badCharacters.Length; i++)
            {
                result = result.Replace(badCharacters[i], namingConfig.ReplaceIllegalCharacters ? goodCharacters[i] : string.Empty);
            }

            return result.TrimStart(' ', '.').TrimEnd(' ');
        }
    }

    internal sealed class TokenMatch
    {
        public Match RegexMatch { get; set; }
        public string Prefix { get; set; }
        public string Separator { get; set; }
        public string Suffix { get; set; }
        public string Token { get; set; }
        public string CustomFormat { get; set; }

        public string DefaultValue(string defaultValue)
        {
            if (string.IsNullOrEmpty(Prefix) && string.IsNullOrEmpty(Suffix))
            {
                return defaultValue;
            }
            else
            {
                return string.Empty;
            }
        }
    }

    public enum ColonReplacementFormat
    {
        Delete = 0,
        Dash = 1,
        SpaceDash = 2,
        SpaceDashSpace = 3,
        Smart = 4
    }
}
