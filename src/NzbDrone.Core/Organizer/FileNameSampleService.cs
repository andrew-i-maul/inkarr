using System.Collections.Generic;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.Organizer
{
    public interface IFilenameSampleService
    {
        SampleResult GetStandardTrackSample(NamingConfig nameSpec);
        SampleResult GetMultiDiscTrackSample(NamingConfig nameSpec);
        string GetVolumeFolderSample(NamingConfig nameSpec);
    }

    public class FileNameSampleService : IFilenameSampleService
    {
        private readonly IBuildFileNames _buildFileNames;

        private static Volume _standardVolume;
        private static Issue _standardIssue;
        private static Edition _standardEdition;
        private static IssueFile _singleTrackFile;
        private static IssueFile _multiTrackFile;
        private static List<CustomFormat> _customFormats;

        public FileNameSampleService(IBuildFileNames buildFileNames)
        {
            _buildFileNames = buildFileNames;

            _standardVolume = new Volume
            {
                Metadata = new VolumeMetadata
                {
                    Name = "The Volume Name",
                    Disambiguation = "US Volume",
                    NameLastFirst = "Last name, First name"
                }
            };

            var series = new Series
            {
                Title = "Series Title"
            };

            var seriesLink = new SeriesIssueLink
            {
                Position = "1",
                Series = series
            };

            _standardIssue = new Issue
            {
                Title = "The Issue Title",
                ReleaseDate = System.DateTime.Today,
                Volume = _standardVolume,
                VolumeMetadata = _standardVolume.Metadata.Value,
                SeriesLinks = new List<SeriesIssueLink> { seriesLink }
            };

            _standardEdition = new Edition
            {
                Title = "The Edition Title",
                Issue = _standardIssue
            };

            _customFormats = new List<CustomFormat>
            {
                new CustomFormat
                {
                    Name = "Surround Sound",
                    IncludeCustomFormatWhenRenaming = true
                },
                new CustomFormat
                {
                    Name = "x264",
                    IncludeCustomFormatWhenRenaming = true
                }
            };

            var mediaInfo = new MediaInfoModel()
            {
                AudioFormat = "Flac Audio",
                AudioChannels = 2,
                AudioBitrate = 875,
                AudioBits = 24,
                AudioSampleRate = 44100
            };

            _singleTrackFile = new IssueFile
            {
                Quality = new QualityModel(Quality.MP3, new Revision(2)),
                Path = "/music/Volume.Name.Issue.Name.TrackNum.Track.Title.MP3256.mp3",
                SceneName = "Volume.Name.Issue.Name.TrackNum.Track.Title.MP3256",
                ReleaseGroup = "RlsGrp",
                MediaInfo = mediaInfo,
                Edition = _standardEdition,
                Part = 1,
                PartCount = 1
            };

            _multiTrackFile = new IssueFile
            {
                Quality = new QualityModel(Quality.MP3, new Revision(2)),
                Path = "/music/Volume.Name.Issue.Name.TrackNum.Track.Title.MP3256.mp3",
                SceneName = "Volume.Name.Issue.Name.TrackNum.Track.Title.MP3256",
                ReleaseGroup = "RlsGrp",
                MediaInfo = mediaInfo,
                Edition = _standardEdition,
                Part = 1,
                PartCount = 2
            };
        }

        public SampleResult GetStandardTrackSample(NamingConfig nameSpec)
        {
            var result = new SampleResult
            {
                FileName = BuildTrackSample(_standardVolume, _singleTrackFile, nameSpec),
                Volume = _standardVolume,
                Issue = _standardIssue,
                IssueFile = _singleTrackFile
            };

            return result;
        }

        public SampleResult GetMultiDiscTrackSample(NamingConfig nameSpec)
        {
            var result = new SampleResult
            {
                FileName = BuildTrackSample(_standardVolume, _multiTrackFile, nameSpec),
                Volume = _standardVolume,
                Issue = _standardIssue,
                IssueFile = _singleTrackFile
            };

            return result;
        }

        public string GetVolumeFolderSample(NamingConfig nameSpec)
        {
            return _buildFileNames.GetVolumeFolder(_standardVolume, nameSpec);
        }

        private string BuildTrackSample(Volume volume, IssueFile issueFile, NamingConfig nameSpec)
        {
            try
            {
                return _buildFileNames.BuildIssueFileName(volume, issueFile.Edition.Value, issueFile, nameSpec, _customFormats);
            }
            catch (NamingFormatException)
            {
                return string.Empty;
            }
        }
    }
}
