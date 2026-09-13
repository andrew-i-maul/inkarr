using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using NzbDrone.Common.Reflection;
using NzbDrone.Core.Authentication;
using NzbDrone.Core.Blocklisting;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.CustomFilters;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.Datastore.Converters;
using NzbDrone.Core.Download;
using NzbDrone.Core.Download.History;
using NzbDrone.Core.Download.Pending;
using NzbDrone.Core.Extras.Metadata;
using NzbDrone.Core.Extras.Metadata.Files;
using NzbDrone.Core.Extras.Others;
using NzbDrone.Core.History;
using NzbDrone.Core.Http;
using NzbDrone.Core.ImportLists;
using NzbDrone.Core.ImportLists.Exclusions;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Instrumentation;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Jobs;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Notifications;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles;
using NzbDrone.Core.Profiles.Delay;
using NzbDrone.Core.Profiles.Metadata;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Profiles.Releases;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.RemotePathMappings;
using NzbDrone.Core.RootFolders;
using NzbDrone.Core.Tags;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Update.History;
using static Dapper.SqlMapper;

namespace NzbDrone.Core.Datastore
{
    public static class TableMapping
    {
        static TableMapping()
        {
            Mapper = new TableMapper();
        }

        public static TableMapper Mapper { get; private set; }

        public static void Map()
        {
            RegisterMappers();

            Mapper.Entity<Config>("Config").RegisterModel();

            Mapper.Entity<RootFolder>("RootFolders").RegisterModel()
                  .Ignore(r => r.Accessible)
                  .Ignore(r => r.FreeSpace)
                  .Ignore(r => r.TotalSpace);

            Mapper.Entity<ScheduledTask>("ScheduledTasks").RegisterModel()
                  .Ignore(i => i.Priority);

            Mapper.Entity<IndexerDefinition>("Indexers").RegisterModel()
                  .Ignore(x => x.ImplementationName)
                  .Ignore(i => i.Enable)
                  .Ignore(i => i.Protocol)
                  .Ignore(i => i.SupportsRss)
                  .Ignore(i => i.SupportsSearch);

            Mapper.Entity<ImportListDefinition>("ImportLists").RegisterModel()
                  .Ignore(x => x.ImplementationName)
                  .Ignore(i => i.ListType)
                  .Ignore(i => i.MinRefreshInterval)
                  .Ignore(i => i.Enable);

            Mapper.Entity<NotificationDefinition>("Notifications").RegisterModel()
                  .Ignore(x => x.ImplementationName)
                  .Ignore(i => i.SupportsOnGrab)
                  .Ignore(i => i.SupportsOnReleaseImport)
                  .Ignore(i => i.SupportsOnUpgrade)
                  .Ignore(i => i.SupportsOnRename)
                  .Ignore(i => i.SupportsOnVolumeAdded)
                  .Ignore(i => i.SupportsOnVolumeDelete)
                  .Ignore(i => i.SupportsOnIssueDelete)
                  .Ignore(i => i.SupportsOnIssueFileDelete)
                  .Ignore(i => i.SupportsOnIssueFileDeleteForUpgrade)
                  .Ignore(i => i.SupportsOnHealthIssue)
                  .Ignore(i => i.SupportsOnDownloadFailure)
                  .Ignore(i => i.SupportsOnImportFailure)
                  .Ignore(i => i.SupportsOnIssueRetag)
                  .Ignore(i => i.SupportsOnApplicationUpdate);

            Mapper.Entity<MetadataDefinition>("Metadata").RegisterModel()
                  .Ignore(x => x.ImplementationName)
                  .Ignore(d => d.Tags);

            Mapper.Entity<DownloadClientDefinition>("DownloadClients").RegisterModel()
                  .Ignore(x => x.ImplementationName)
                  .Ignore(d => d.Protocol);

            Mapper.Entity<EntityHistory>("History").RegisterModel();

            Mapper.Entity<Volume>("Volumes")
                  .Ignore(s => s.RootFolderPath)
                  .Ignore(s => s.Name)
                  .Ignore(s => s.ForeignVolumeId)
                  .HasOne(a => a.Metadata, a => a.VolumeMetadataId)
                  .HasOne(a => a.QualityProfile, a => a.QualityProfileId)
                  .HasOne(s => s.MetadataProfile, s => s.MetadataProfileId)
                  .LazyLoad(a => a.Issues, (db, a) => db.Query<Issue>(new SqlBuilder(db.DatabaseType).Where<Issue>(b => b.VolumeMetadataId == a.VolumeMetadataId)).ToList(), a => a.VolumeMetadataId > 0);

            Mapper.Entity<Series>("Series").RegisterModel()
                .Ignore(s => s.ForeignVolumeId)
                .LazyLoad(s => s.LinkItems,
                          (db, series) => db.Query<SeriesIssueLink>(new SqlBuilder(db.DatabaseType).Where<SeriesIssueLink>(s => s.SeriesId == series.Id)).ToList(),
                          s => s.Id > 0)
                .LazyLoad(s => s.Issues,
                          (db, series) => db.Query<Issue>(new SqlBuilder(db.DatabaseType)
                                                         .Join<Issue, SeriesIssueLink>((l, r) => l.Id == r.IssueId)
                                                         .Join<SeriesIssueLink, Series>((l, r) => l.SeriesId == r.Id)
                                                         .Where<Series>(s => s.Id == series.Id)).ToList(),
                          s => s.Id > 0);

            Mapper.Entity<SeriesIssueLink>("SeriesIssueLink").RegisterModel()
                  .HasOne(l => l.Issue, l => l.IssueId)
                  .HasOne(l => l.Series, l => l.SeriesId);

            Mapper.Entity<VolumeMetadata>("VolumeMetadata").RegisterModel();

            Mapper.Entity<Issue>("Issues").RegisterModel()
                .Ignore(x => x.VolumeId)
                .Ignore(x => x.ForeignEditionId)
                .HasOne(r => r.VolumeMetadata, r => r.VolumeMetadataId)
                .LazyLoad(x => x.IssueFiles,
                          (db, issue) => db.Query<IssueFile>(new SqlBuilder(db.DatabaseType)
                                                           .Join<IssueFile, Edition>((l, r) => l.EditionId == r.Id)
                                                           .Where<Edition>(b => b.IssueId == issue.Id)).ToList(),
                          b => b.Id > 0)
                .LazyLoad(x => x.Editions,
                          (db, issue) => db.Query<Edition>(new SqlBuilder(db.DatabaseType).Where<Edition>(e => e.IssueId == issue.Id)).ToList(),
                          b => b.Id > 0)
                .LazyLoad(a => a.Volume,
                          (db, issue) => VolumeRepository.Query(db,
                                                                new SqlBuilder(db.DatabaseType)
                                                                .Join<Volume, VolumeMetadata>((a, m) => a.VolumeMetadataId == m.Id)
                                                                .Where<Volume>(a => a.VolumeMetadataId == issue.VolumeMetadataId)).SingleOrDefault(),
                          a => a.VolumeMetadataId > 0)
                .LazyLoad(b => b.SeriesLinks,
                          (db, issue) => db.Query<SeriesIssueLink>(new SqlBuilder(db.DatabaseType).Where<SeriesIssueLink>(s => s.IssueId == issue.Id)).ToList(),
                          b => b.Id > 0);

            Mapper.Entity<Edition>("Editions").RegisterModel()
                .HasOne(r => r.Issue, r => r.IssueId)
                .LazyLoad(x => x.IssueFiles,
                          (db, edition) => db.Query<IssueFile>(new SqlBuilder(db.DatabaseType).Where<IssueFile>(f => f.EditionId == edition.Id)).ToList(),
                          b => b.Id > 0);

            Mapper.Entity<IssueFile>("IssueFiles").RegisterModel()
                .Ignore(x => x.PartCount)
                .HasOne(f => f.Edition, f => f.EditionId)
                .LazyLoad(x => x.Volume,
                          (db, f) => VolumeRepository.Query(db,
                                                            new SqlBuilder(db.DatabaseType)
                                                            .Join<Volume, VolumeMetadata>((a, m) => a.VolumeMetadataId == m.Id)
                                                            .Join<Volume, Issue>((l, r) => l.VolumeMetadataId == r.VolumeMetadataId)
                                                            .Join<Issue, Edition>((l, r) => l.Id == r.IssueId)
                                                            .Where<Edition>(a => a.Id == f.EditionId)).SingleOrDefault(),
                          t => t.Id > 0);

            Mapper.Entity<QualityDefinition>("QualityDefinitions").RegisterModel()
                  .Ignore(d => d.GroupName)
                  .Ignore(d => d.GroupWeight)
                  .Ignore(d => d.Weight);

            Mapper.Entity<CustomFormat>("CustomFormats").RegisterModel();

            Mapper.Entity<QualityProfile>("QualityProfiles").RegisterModel();
            Mapper.Entity<MetadataProfile>("MetadataProfiles").RegisterModel();
            Mapper.Entity<Log>("Logs").RegisterModel();
            Mapper.Entity<NamingConfig>("NamingConfig").RegisterModel();

            Mapper.Entity<Blocklist>("Blocklist").RegisterModel();
            Mapper.Entity<MetadataFile>("MetadataFiles").RegisterModel();
            Mapper.Entity<OtherExtraFile>("ExtraFiles").RegisterModel();

            Mapper.Entity<PendingRelease>("PendingReleases").RegisterModel()
                  .Ignore(e => e.RemoteIssue);

            Mapper.Entity<RemotePathMapping>("RemotePathMappings").RegisterModel();
            Mapper.Entity<Tag>("Tags").RegisterModel();
            Mapper.Entity<ReleaseProfile>("ReleaseProfiles").RegisterModel();

            Mapper.Entity<DelayProfile>("DelayProfiles").RegisterModel();
            Mapper.Entity<User>("Users").RegisterModel();
            Mapper.Entity<CommandModel>("Commands").RegisterModel()
                  .Ignore(c => c.Message);

            Mapper.Entity<IndexerStatus>("IndexerStatus").RegisterModel();
            Mapper.Entity<DownloadClientStatus>("DownloadClientStatus").RegisterModel();
            Mapper.Entity<ImportListStatus>("ImportListStatus").RegisterModel();
            Mapper.Entity<NotificationStatus>("NotificationStatus").RegisterModel();

            Mapper.Entity<CustomFilter>("CustomFilters").RegisterModel();
            Mapper.Entity<ImportListExclusion>("ImportListExclusions").RegisterModel();

            Mapper.Entity<CachedHttpResponse>("HttpResponse").RegisterModel();

            Mapper.Entity<DownloadHistory>("DownloadHistory").RegisterModel();

            Mapper.Entity<UpdateHistory>("UpdateHistory").RegisterModel();
        }

        private static void RegisterMappers()
        {
            RegisterEmbeddedConverter();
            RegisterProviderSettingConverter();

            SqlMapper.RemoveTypeMap(typeof(DateTime));
            SqlMapper.AddTypeHandler(new DapperUtcConverter());
            SqlMapper.AddTypeHandler(new DapperTimeSpanConverter());
            SqlMapper.AddTypeHandler(new DapperQualityIntConverter());
            SqlMapper.AddTypeHandler(new EmbeddedDocumentConverter<List<QualityProfileQualityItem>>(new QualityIntConverter()));
            SqlMapper.AddTypeHandler(new EmbeddedDocumentConverter<List<ProfileFormatItem>>(new CustomFormatIntConverter()));
            SqlMapper.AddTypeHandler(new EmbeddedDocumentConverter<List<ICustomFormatSpecification>>(new CustomFormatSpecificationListConverter()));
            SqlMapper.AddTypeHandler(new EmbeddedDocumentConverter<QualityModel>(new QualityIntConverter()));
            SqlMapper.AddTypeHandler(new EmbeddedDocumentConverter<Dictionary<string, string>>());
            SqlMapper.AddTypeHandler(new EmbeddedDocumentConverter<IDictionary<string, string>>());
            SqlMapper.AddTypeHandler(new EmbeddedDocumentConverter<List<int>>());
            SqlMapper.AddTypeHandler(new EmbeddedDocumentConverter<List<KeyValuePair<string, int>>>());
            SqlMapper.AddTypeHandler(new EmbeddedDocumentConverter<KeyValuePair<string, int>>());
            SqlMapper.AddTypeHandler(new EmbeddedDocumentConverter<List<string>>());
            SqlMapper.AddTypeHandler(new EmbeddedDocumentConverter<ParsedIssueInfo>());
            SqlMapper.AddTypeHandler(new EmbeddedDocumentConverter<ParsedTrackInfo>());
            SqlMapper.AddTypeHandler(new EmbeddedDocumentConverter<ReleaseInfo>());
            SqlMapper.AddTypeHandler(new EmbeddedDocumentConverter<HashSet<int>>());
            SqlMapper.AddTypeHandler(new OsPathConverter());
            SqlMapper.RemoveTypeMap(typeof(Guid));
            SqlMapper.RemoveTypeMap(typeof(Guid?));
            SqlMapper.AddTypeHandler(new GuidConverter());
            SqlMapper.AddTypeHandler(new CommandConverter());
            SqlMapper.AddTypeHandler(new SystemVersionConverter());
        }

        private static void RegisterProviderSettingConverter()
        {
            var settingTypes = typeof(IProviderConfig).Assembly.ImplementationsOf<IProviderConfig>()
                .Where(x => !x.ContainsGenericParameters);

            var providerSettingConverter = new ProviderSettingConverter();
            foreach (var embeddedType in settingTypes)
            {
                SqlMapper.AddTypeHandler(embeddedType, providerSettingConverter);
            }
        }

        private static void RegisterEmbeddedConverter()
        {
            var embeddedTypes = typeof(IEmbeddedDocument).Assembly.ImplementationsOf<IEmbeddedDocument>();

            var embeddedConverterDefinition = typeof(EmbeddedDocumentConverter<>).GetGenericTypeDefinition();
            var genericListDefinition = typeof(List<>).GetGenericTypeDefinition();

            foreach (var embeddedType in embeddedTypes)
            {
                var embeddedListType = genericListDefinition.MakeGenericType(embeddedType);

                RegisterEmbeddedConverter(embeddedType, embeddedConverterDefinition);
                RegisterEmbeddedConverter(embeddedListType, embeddedConverterDefinition);
            }
        }

        private static void RegisterEmbeddedConverter(Type embeddedType, Type embeddedConverterDefinition)
        {
            var embeddedConverterType = embeddedConverterDefinition.MakeGenericType(embeddedType);
            var converter = (ITypeHandler)Activator.CreateInstance(embeddedConverterType);

            SqlMapper.AddTypeHandler(embeddedType, converter);
        }
    }
}
