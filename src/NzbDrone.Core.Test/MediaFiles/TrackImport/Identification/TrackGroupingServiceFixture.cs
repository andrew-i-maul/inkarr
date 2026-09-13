using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using FizzWare.NBuilder;
using FizzWare.NBuilder.PropertyNaming;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.MediaFiles.IssueImport.Identification;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MediaFiles.IssueImport.Identification
{
    // we need to use random strings to test the va (so we don't just get volume1, volume2 etc which are too similar)
    // but the standard random value namer would give paths that are too long on windows
    public class RandomValueNamerShortStrings : RandomValuePropertyNamer
    {
        private static readonly List<char> AllowedChars;
        private readonly IRandomGenerator _generator;

        public RandomValueNamerShortStrings(BuilderSettings settings)
            : base(settings)
        {
            _generator = new RandomGenerator();
        }

        static RandomValueNamerShortStrings()
        {
            AllowedChars = new List<char>();
            for (var c = 'a'; c < 'z'; c++)
            {
                AllowedChars.Add(c);
            }

            for (var c = 'A'; c < 'Z'; c++)
            {
                AllowedChars.Add(c);
            }

            for (var c = '0'; c < '9'; c++)
            {
                AllowedChars.Add(c);
            }
        }

        protected override string GetString(MemberInfo memberInfo)
        {
            var length = _generator.Next(1, 100);

            var chars = new char[length];

            for (var i = 0; i < length; i++)
            {
                var index = _generator.Next(0, AllowedChars.Count - 1);
                chars[i] = AllowedChars[index];
            }

            var bytes = Encoding.UTF8.GetBytes(chars);
            return Encoding.UTF8.GetString(bytes, 0, bytes.Length);
        }
    }

    [TestFixture]
    public class TrackGroupingServiceFixture : CoreTest<TrackGroupingService>
    {
        private List<LocalIssue> GivenTracks(string root, string volume, string issue, int count)
        {
            var fileInfos = Builder<ParsedTrackInfo>
                .CreateListOfSize(count)
                .All()
                .With(f => f.Volumes = new List<string> { volume })
                .With(f => f.IssueTitle = issue)
                .With(f => f.IssueMBId = null)
                .With(f => f.ReleaseMBId = null)
                .Build();

            var tracks = fileInfos.Select(x => Builder<LocalIssue>
                                          .CreateNew()
                                          .With(y => y.FileTrackInfo = x)
                                          .With(y => y.Path = Path.Combine(root, x.Title))
                                          .Build()).ToList();

            return tracks;
        }

        private List<LocalIssue> GivenTracksWithNoTags(string root, int count)
        {
            var outp = new List<LocalIssue>();

            for (var i = 0; i < count; i++)
            {
                var track = Builder<LocalIssue>
                    .CreateNew()
                    .With(y => y.FileTrackInfo = new ParsedTrackInfo())
                    .With(y => y.Path = Path.Combine(root, $"{i}.mp3"))
                    .Build();
                outp.Add(track);
            }

            return outp;
        }

        [Repeat(100)]
        private List<LocalIssue> GivenVaTracks(string root, string issue, int count)
        {
            var settings = new BuilderSettings();
            settings.SetPropertyNamerFor<ParsedTrackInfo>(new RandomValueNamerShortStrings(settings));

            var builder = new Builder(settings);

            var fileInfos = builder
                .CreateListOfSize<ParsedTrackInfo>(count)
                .All()
                .With(f => f.IssueTitle = "issue")
                .With(f => f.IssueMBId = null)
                .With(f => f.ReleaseMBId = null)
                .Build();

            var tracks = fileInfos.Select(x => Builder<LocalIssue>
                                          .CreateNew()
                                          .With(y => y.FileTrackInfo = x)
                                          .With(y => y.Path = Path.Combine(@"C:\music\incoming".AsOsAgnostic(), x.Title))
                                          .Build()).ToList();

            return tracks;
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(10)]
        public void single_volume_is_not_various_volumes(int count)
        {
            var tracks = GivenTracks(@"C:\music\incoming".AsOsAgnostic(), "volume", "issue", count);
            TrackGroupingService.IsVariousVolumes(tracks).Should().Be(false);
        }

        // GivenVaTracks uses random names so repeat multiple times to try to prompt any intermittent failures
        [Ignore("TODO: fix")]
        [Test]
        [Repeat(100)]
        public void all_different_volumes_is_various_volumes()
        {
            var tracks = GivenVaTracks(@"C:\music\incoming".AsOsAgnostic(), "issue", 10);
            TrackGroupingService.IsVariousVolumes(tracks).Should().Be(true);
        }

        [Test]
        public void two_volumes_is_not_various_volumes()
        {
            var dir = @"C:\music\incoming".AsOsAgnostic();
            var tracks = GivenTracks(dir, "volume1", "issue", 10);
            tracks.AddRange(GivenTracks(dir, "volume2", "issue", 10));

            TrackGroupingService.IsVariousVolumes(tracks).Should().Be(false);
        }

        [Ignore("TODO: fix")]
        [Test]
        [Repeat(100)]
        public void mostly_different_volumes_is_various_volumes()
        {
            var dir = @"C:\music\incoming".AsOsAgnostic();
            var tracks = GivenVaTracks(dir, "issue", 10);
            tracks.AddRange(GivenTracks(dir, "single_volume", "issue", 2));
            TrackGroupingService.IsVariousVolumes(tracks).Should().Be(true);
        }

        [TestCase("")]
        [TestCase("Various Volumes")]
        [TestCase("Various")]
        [TestCase("VA")]
        [TestCase("Unknown")]
        public void va_volume_title_is_various_volumes(string volume)
        {
            var tracks = GivenTracks(@"C:\music\incoming".AsOsAgnostic(), volume, "issue", 10);
            TrackGroupingService.IsVariousVolumes(tracks).Should().Be(true);
        }

        [TestCase("Va?!")]
        [TestCase("Va Va Voom")]
        [TestCase("V.A. Jr.")]
        [TestCase("Ca Va")]
        public void va_in_volume_name_is_not_various_volumes(string volume)
        {
            var tracks = GivenTracks(@"C:\music\incoming".AsOsAgnostic(), volume, "issue", 10);
            TrackGroupingService.IsVariousVolumes(tracks).Should().Be(false);
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(10)]
        public void should_group_single_volume_issue(int count)
        {
            var tracks = GivenTracks(@"C:\music\incoming".AsOsAgnostic(), "volume", "issue", count);
            var output = Subject.GroupTracks(tracks);

            TrackGroupingService.IsVariousVolumes(tracks).Should().Be(false);
            TrackGroupingService.LooksLikeSingleRelease(tracks).Should().Be(true);

            output.Count.Should().Be(1);
            output[0].LocalIssues.Count.Should().Be(count);
        }

        [TestCase("cd")]
        [TestCase("disc")]
        [TestCase("disk")]
        public void should_group_multi_disc_release(string mediaName)
        {
            var tracks = GivenTracks($"C:\\music\\incoming\\volume - issue\\{mediaName} 1".AsOsAgnostic(), "volume", "issue", 10);
            tracks.AddRange(GivenTracks($"C:\\music\\incoming\\volume - issue\\{mediaName} 2".AsOsAgnostic(), "volume", "issue", 5));

            TrackGroupingService.IsVariousVolumes(tracks).Should().Be(false);
            TrackGroupingService.LooksLikeSingleRelease(tracks).Should().Be(true);

            var output = Subject.GroupTracks(tracks);
            output.Count.Should().Be(1);
            output[0].LocalIssues.Count.Should().Be(15);
        }

        [Test]
        public void should_not_group_two_different_issues_by_same_volume()
        {
            var tracks = GivenTracks($"C:\\music\\incoming\\volume - issue1".AsOsAgnostic(), "volume", "issue1", 10);
            tracks.AddRange(GivenTracks($"C:\\music\\incoming\\volume - issue2".AsOsAgnostic(), "volume", "issue2", 5));

            TrackGroupingService.IsVariousVolumes(tracks).Should().Be(false);
            TrackGroupingService.LooksLikeSingleRelease(tracks).Should().Be(false);

            var output = Subject.GroupTracks(tracks);
            output.Count.Should().Be(2);
            output[0].LocalIssues.Count.Should().Be(10);
            output[1].LocalIssues.Count.Should().Be(5);
        }

        [Test]
        public void should_group_issues_with_typos()
        {
            var tracks = GivenTracks($"C:\\music\\incoming\\volume - issue".AsOsAgnostic(), "volume", "Rastaman Vibration (Remastered)", 10);
            tracks.AddRange(GivenTracks($"C:\\music\\incoming\\volume - issue".AsOsAgnostic(), "volume", "Rastaman Vibration (Remastered", 5));

            TrackGroupingService.IsVariousVolumes(tracks).Should().Be(false);
            TrackGroupingService.LooksLikeSingleRelease(tracks).Should().Be(true);

            var output = Subject.GroupTracks(tracks);
            output.Count.Should().Be(1);
            output[0].LocalIssues.Count.Should().Be(15);
        }

        [Test]
        public void should_not_group_two_different_tracks_in_same_directory()
        {
            var tracks = GivenTracks($"C:\\music\\incoming".AsOsAgnostic(), "volume", "issue1", 1);
            tracks.AddRange(GivenTracks($"C:\\music\\incoming".AsOsAgnostic(), "volume", "issue2", 1));

            TrackGroupingService.IsVariousVolumes(tracks).Should().Be(false);
            TrackGroupingService.LooksLikeSingleRelease(tracks).Should().Be(false);

            var output = Subject.GroupTracks(tracks);
            output.Count.Should().Be(2);
            output[0].LocalIssues.Count.Should().Be(1);
            output[1].LocalIssues.Count.Should().Be(1);
        }

        [Test]
        public void should_separate_two_issues_in_same_directory()
        {
            var tracks = GivenTracks($"C:\\music\\incoming\\volume discog".AsOsAgnostic(), "volume", "issue1", 10);
            tracks.AddRange(GivenTracks($"C:\\music\\incoming\\volume disog".AsOsAgnostic(), "volume", "issue2", 5));

            TrackGroupingService.IsVariousVolumes(tracks).Should().Be(false);
            TrackGroupingService.LooksLikeSingleRelease(tracks).Should().Be(false);

            var output = Subject.GroupTracks(tracks);
            output.Count.Should().Be(2);
            output[0].LocalIssues.Count.Should().Be(10);
            output[1].LocalIssues.Count.Should().Be(5);
        }

        [Test]
        public void should_separate_many_issues_in_same_directory()
        {
            var tracks = new List<LocalIssue>();
            for (var i = 0; i < 100; i++)
            {
                tracks.AddRange(GivenTracks($"C:\\music".AsOsAgnostic(), "volume" + i, "issue" + i, 10));
            }

            // don't test various volumes here because it's designed to only work if there's a common issue
            TrackGroupingService.LooksLikeSingleRelease(tracks).Should().Be(false);

            var output = Subject.GroupTracks(tracks);
            output.Count.Should().Be(100);
            output.Select(x => x.LocalIssues.Count).Distinct().Should().BeEquivalentTo(new List<int> { 10 });
        }

        [Test]
        public void should_separate_two_issues_by_different_volumes_in_same_directory()
        {
            var tracks = GivenTracks($"C:\\music\\incoming".AsOsAgnostic(), "volume1", "issue1", 10);
            tracks.AddRange(GivenTracks($"C:\\music\\incoming".AsOsAgnostic(), "volume2", "issue2", 5));

            TrackGroupingService.IsVariousVolumes(tracks).Should().Be(false);
            TrackGroupingService.LooksLikeSingleRelease(tracks).Should().Be(false);

            var output = Subject.GroupTracks(tracks);
            output.Count.Should().Be(2);
            output[0].LocalIssues.Count.Should().Be(10);
            output[1].LocalIssues.Count.Should().Be(5);
        }

        [Ignore("TODO: fix")]
        [Test]
        [Repeat(100)]
        public void should_group_va_release()
        {
            var tracks = GivenVaTracks(@"C:\music\incoming".AsOsAgnostic(), "issue", 10);

            TrackGroupingService.IsVariousVolumes(tracks).Should().Be(true);
            TrackGroupingService.LooksLikeSingleRelease(tracks).Should().Be(true);

            var output = Subject.GroupTracks(tracks);
            output.Count.Should().Be(1);
            output[0].LocalIssues.Count.Should().Be(10);
        }

        [Test]
        public void should_not_group_two_issues_by_different_volumes_with_same_title()
        {
            var tracks = GivenTracks($"C:\\music\\incoming\\issue".AsOsAgnostic(), "volume1", "issue", 10);
            tracks.AddRange(GivenTracks($"C:\\music\\incoming\\issue".AsOsAgnostic(), "volume2", "issue", 5));

            TrackGroupingService.IsVariousVolumes(tracks).Should().Be(false);
            TrackGroupingService.LooksLikeSingleRelease(tracks).Should().Be(false);

            var output = Subject.GroupTracks(tracks);

            output.Count.Should().Be(2);
            output[0].LocalIssues.Count.Should().Be(10);
            output[1].LocalIssues.Count.Should().Be(5);
        }

        [Test]
        public void should_not_fail_if_all_tags_null()
        {
            var tracks = GivenTracksWithNoTags($"C:\\music\\incoming\\issue".AsOsAgnostic(), 10);

            TrackGroupingService.IsVariousVolumes(tracks).Should().Be(false);
            TrackGroupingService.LooksLikeSingleRelease(tracks).Should().Be(true);

            var output = Subject.GroupTracks(tracks);
            output.Count.Should().Be(1);
            output[0].LocalIssues.Count.Should().Be(10);
        }

        [Test]
        public void should_not_fail_if_some_tags_null()
        {
            var tracks = GivenTracks($"C:\\music\\incoming\\issue".AsOsAgnostic(), "volume1", "issue", 10);
            tracks.AddRange(GivenTracksWithNoTags($"C:\\music\\incoming\\issue".AsOsAgnostic(), 2));

            TrackGroupingService.IsVariousVolumes(tracks).Should().Be(false);
            TrackGroupingService.LooksLikeSingleRelease(tracks).Should().Be(true);

            var output = Subject.GroupTracks(tracks);
            output.Count.Should().Be(1);
            output[0].LocalIssues.Count.Should().Be(12);
        }

        [Test]
        public void should_cope_with_one_issue_in_subfolder_of_another()
        {
            var tracks = GivenTracks($"C:\\music\\incoming\\issue".AsOsAgnostic(), "volume1", "issue", 10);
            tracks.AddRange(GivenTracks($"C:\\music\\incoming\\issue\\anotherissue".AsOsAgnostic(), "volume2", "issue2", 10));

            TrackGroupingService.IsVariousVolumes(tracks).Should().Be(false);
            TrackGroupingService.LooksLikeSingleRelease(tracks).Should().Be(false);

            var output = Subject.GroupTracks(tracks);

            foreach (var group in output)
            {
                TestLogger.Debug($"*** group {group} ***");
                TestLogger.Debug(string.Join("\n", group.LocalIssues.Select(x => x.Path)));
            }

            output.Count.Should().Be(2);
            output[0].LocalIssues.Count.Should().Be(10);
            output[1].LocalIssues.Count.Should().Be(10);
        }
    }
}
