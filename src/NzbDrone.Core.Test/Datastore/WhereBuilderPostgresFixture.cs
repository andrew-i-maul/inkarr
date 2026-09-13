using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Datastore
{
    [TestFixture]
    public class WhereBuilderPostgresFixture : CoreTest
    {
        private WhereBuilderPostgres _subject;

        [OneTimeSetUp]
        public void MapTables()
        {
            // Generate table mapping
            Mocker.Resolve<DbFactory>();
        }

        private WhereBuilderPostgres Where(Expression<Func<Volume, bool>> filter)
        {
            return new WhereBuilderPostgres(filter, true, 0);
        }

        private WhereBuilderPostgres WhereMetadata(Expression<Func<VolumeMetadata, bool>> filter)
        {
            return new WhereBuilderPostgres(filter, true, 0);
        }

        [Test]
        public void postgres_where_equal_const()
        {
            _subject = Where(x => x.Id == 10);

            _subject.ToString().Should().Be($"(\"Volumes\".\"Id\" = @Clause1_P1)");
            _subject.Parameters.Get<int>("Clause1_P1").Should().Be(10);
        }

        [Test]
        public void postgres_where_equal_variable()
        {
            var id = 10;
            _subject = Where(x => x.Id == id);

            _subject.ToString().Should().Be($"(\"Volumes\".\"Id\" = @Clause1_P1)");
            _subject.Parameters.Get<int>("Clause1_P1").Should().Be(id);
        }

        [Test]
        public void postgres_where_equal_property()
        {
            var volume = new Volume { Id = 10 };
            _subject = Where(x => x.Id == volume.Id);

            _subject.Parameters.ParameterNames.Should().HaveCount(1);
            _subject.ToString().Should().Be($"(\"Volumes\".\"Id\" = @Clause1_P1)");
            _subject.Parameters.Get<int>("Clause1_P1").Should().Be(volume.Id);
        }

        [Test]
        public void postgres_where_equal_joined_property()
        {
            _subject = Where(x => x.QualityProfile.Value.Id == 1);

            _subject.Parameters.ParameterNames.Should().HaveCount(1);
            _subject.ToString().Should().Be($"(\"QualityProfiles\".\"Id\" = @Clause1_P1)");
            _subject.Parameters.Get<int>("Clause1_P1").Should().Be(1);
        }

        [Test]
        public void postgres_where_throws_without_concrete_condition_if_requiresConcreteCondition()
        {
            Expression<Func<Volume, Volume, bool>> filter = (x, y) => x.Id == y.Id;
            _subject = new WhereBuilderPostgres(filter, true, 0);
            Assert.Throws<InvalidOperationException>(() => _subject.ToString());
        }

        [Test]
        public void postgres_where_allows_abstract_condition_if_not_requiresConcreteCondition()
        {
            Expression<Func<Volume, Volume, bool>> filter = (x, y) => x.Id == y.Id;
            _subject = new WhereBuilderPostgres(filter, false, 0);
            _subject.ToString().Should().Be($"(\"Volumes\".\"Id\" = \"Volumes\".\"Id\")");
        }

        [Test]
        public void postgres_where_string_is_null()
        {
            _subject = Where(x => x.CleanName == null);

            _subject.ToString().Should().Be($"(\"Volumes\".\"CleanName\" IS NULL)");
        }

        [Test]
        public void postgres_where_string_is_null_value()
        {
            string cleanName = null;
            _subject = Where(x => x.CleanName == cleanName);

            _subject.ToString().Should().Be($"(\"Volumes\".\"CleanName\" IS NULL)");
        }

        [Test]
        public void postgres_where_equal_null_property()
        {
            var volume = new Volume { CleanName = null };
            _subject = Where(x => x.CleanName == volume.CleanName);

            _subject.ToString().Should().Be($"(\"Volumes\".\"CleanName\" IS NULL)");
        }

        [Test]
        public void postgres_where_column_contains_string()
        {
            var test = "small";
            _subject = Where(x => x.CleanName.Contains(test));

            _subject.ToString().Should().Be($"(\"Volumes\".\"CleanName\" ILIKE '%' || @Clause1_P1 || '%')");
            _subject.Parameters.Get<string>("Clause1_P1").Should().Be(test);
        }

        [Test]
        public void postgres_where_string_contains_column()
        {
            var test = "small";
            _subject = Where(x => test.Contains(x.CleanName));

            _subject.ToString().Should().Be($"(@Clause1_P1 ILIKE '%' || \"Volumes\".\"CleanName\" || '%')");
            _subject.Parameters.Get<string>("Clause1_P1").Should().Be(test);
        }

        [Test]
        public void postgres_where_column_starts_with_string()
        {
            var test = "small";
            _subject = Where(x => x.CleanName.StartsWith(test));

            _subject.ToString().Should().Be($"(\"Volumes\".\"CleanName\" ILIKE @Clause1_P1 || '%')");
            _subject.Parameters.Get<string>("Clause1_P1").Should().Be(test);
        }

        [Test]
        public void postgres_where_column_ends_with_string()
        {
            var test = "small";
            _subject = Where(x => x.CleanName.EndsWith(test));

            _subject.ToString().Should().Be($"(\"Volumes\".\"CleanName\" ILIKE '%' || @Clause1_P1)");
            _subject.Parameters.Get<string>("Clause1_P1").Should().Be(test);
        }

        [Test]
        public void postgres_where_in_list()
        {
            var list = new List<int> { 1, 2, 3 };
            _subject = Where(x => list.Contains(x.Id));

            _subject.ToString().Should().Be($"(\"Volumes\".\"Id\" = ANY (('{{1, 2, 3}}')))");
        }

        [Test]
        public void postgres_where_in_list_2()
        {
            var list = new List<int> { 1, 2, 3 };
            _subject = Where(x => x.CleanName == "test" && list.Contains(x.Id));

            _subject.ToString().Should().Be($"((\"Volumes\".\"CleanName\" = @Clause1_P1) AND (\"Volumes\".\"Id\" = ANY (('{{1, 2, 3}}'))))");
        }

        [Test]
        public void postgres_where_in_string_list()
        {
            var list = new List<string> { "first", "second", "third" };

            _subject = Where(x => list.Contains(x.CleanName));

            _subject.ToString().Should().Be($"(\"Volumes\".\"CleanName\" = ANY (@Clause1_P1))");
        }

        [Test]
        public void enum_as_int()
        {
            _subject = WhereMetadata(x => x.Status == VolumeStatusType.Continuing);

            _subject.ToString().Should().Be($"(\"VolumeMetadata\".\"Status\" = @Clause1_P1)");
        }

        [Test]
        public void enum_in_list()
        {
            var allowed = new List<VolumeStatusType> { VolumeStatusType.Continuing, VolumeStatusType.Ended };
            _subject = WhereMetadata(x => allowed.Contains(x.Status));

            _subject.ToString().Should().Be($"(\"VolumeMetadata\".\"Status\" = ANY (@Clause1_P1))");
        }

        [Test]
        public void enum_in_array()
        {
            var allowed = new VolumeStatusType[] { VolumeStatusType.Continuing, VolumeStatusType.Ended };
            _subject = WhereMetadata(x => allowed.Contains(x.Status));

            _subject.ToString().Should().Be($"(\"VolumeMetadata\".\"Status\" = ANY (@Clause1_P1))");
        }
    }
}
