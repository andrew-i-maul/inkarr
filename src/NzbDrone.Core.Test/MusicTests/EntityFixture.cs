using System.Collections;
using System.Linq;
using System.Reflection;
using AutoFixture;
using Equ;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Issues;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MusicTests
{
    [TestFixture]
    public class EntityFixture : LoggingTest
    {
        private Fixture _fixture = new Fixture();

        private static bool IsNotMarkedAsIgnore(PropertyInfo propertyInfo)
        {
            return !propertyInfo.GetCustomAttributes(typeof(MemberwiseEqualityIgnoreAttribute), true).Any();
        }

        public class EqualityPropertySource<T>
        {
            public static IEnumerable TestCases
            {
                get
                {
                    foreach (var property in typeof(T).GetProperties().Where(x => x.CanRead && x.CanWrite && IsNotMarkedAsIgnore(x)))
                    {
                        yield return new TestCaseData(property).SetName($"{{m}}_{property.Name}");
                    }
                }
            }
        }

        public class IgnoredPropertySource<T>
        {
            public static IEnumerable TestCases
            {
                get
                {
                    foreach (var property in typeof(T).GetProperties().Where(x => x.CanRead && x.CanWrite && !IsNotMarkedAsIgnore(x)))
                    {
                        yield return new TestCaseData(property).SetName($"{{m}}_{property.Name}");
                    }
                }
            }
        }

        [Test]
        public void two_equivalent_volume_metadata_should_be_equal()
        {
            var item1 = _fixture.Create<VolumeMetadata>();
            var item2 = item1.JsonClone();

            item1.Should().NotBeSameAs(item2);
            item1.Should().Be(item2);
        }

        [Test]
        [TestCaseSource(typeof(EqualityPropertySource<VolumeMetadata>), "TestCases")]
        public void two_different_volume_metadata_should_not_be_equal(PropertyInfo prop)
        {
            var item1 = _fixture.Create<VolumeMetadata>();
            var item2 = item1.JsonClone();
            var different = _fixture.Create<VolumeMetadata>();

            // make item2 different in the property under consideration
            var differentEntry = prop.GetValue(different);
            prop.SetValue(item2, differentEntry);

            item1.Should().NotBeSameAs(item2);
            item1.Should().NotBe(item2);
        }

        [Test]
        public void metadata_and_db_fields_should_replicate_volume_metadata()
        {
            var item1 = _fixture.Create<VolumeMetadata>();
            var item2 = _fixture.Create<VolumeMetadata>();

            item1.Should().NotBe(item2);

            item1.UseMetadataFrom(item2);
            item1.UseDbFieldsFrom(item2);
            item1.Should().Be(item2);
        }

        private Issue GivenIssue()
        {
            return _fixture.Build<Issue>()
                .Without(x => x.VolumeMetadata)
                .Without(x => x.Volume)
                .Without(x => x.VolumeId)
                .Create();
        }

        [Test]
        public void two_equivalent_issues_should_be_equal()
        {
            var item1 = GivenIssue();
            var item2 = item1.JsonClone();

            item1.Should().NotBeSameAs(item2);
            item1.Should().Be(item2);
        }

        [Test]
        [TestCaseSource(typeof(EqualityPropertySource<Issue>), "TestCases")]
        public void two_different_issues_should_not_be_equal(PropertyInfo prop)
        {
            var item1 = GivenIssue();
            var item2 = item1.JsonClone();
            var different = GivenIssue();

            // make item2 different in the property under consideration
            if (prop.PropertyType == typeof(bool))
            {
                prop.SetValue(item2, !(bool)prop.GetValue(item1));
            }
            else
            {
                prop.SetValue(item2, prop.GetValue(different));
            }

            item1.Should().NotBeSameAs(item2);
            item1.Should().NotBe(item2);
        }

        [Test]
        public void metadata_and_db_fields_should_replicate_issue()
        {
            var item1 = GivenIssue();
            var item2 = GivenIssue();

            item1.Should().NotBe(item2);

            item1.UseMetadataFrom(item2);
            item1.UseDbFieldsFrom(item2);
            item1.Should().Be(item2);
        }

        private Edition GivenEdition()
        {
            return _fixture.Build<Edition>()
                .Without(x => x.Issue)
                .Without(x => x.IssueFiles)
                .Create();
        }

        [Test]
        public void two_equivalent_editions_should_be_equal()
        {
            var item1 = GivenEdition();
            var item2 = item1.JsonClone();

            item1.Should().NotBeSameAs(item2);
            item1.Should().Be(item2);
        }

        [Test]
        [TestCaseSource(typeof(EqualityPropertySource<Edition>), "TestCases")]
        public void two_different_editions_should_not_be_equal(PropertyInfo prop)
        {
            var item1 = GivenEdition();
            var item2 = item1.JsonClone();
            var different = GivenEdition();

            // make item2 different in the property under consideration
            if (prop.PropertyType == typeof(bool))
            {
                prop.SetValue(item2, !(bool)prop.GetValue(item1));
            }
            else
            {
                prop.SetValue(item2, prop.GetValue(different));
            }

            item1.Should().NotBeSameAs(item2);
            item1.Should().NotBe(item2);
        }

        [Test]
        public void metadata_and_db_fields_should_replicate_edition()
        {
            var item1 = GivenEdition();
            var item2 = GivenEdition();

            item1.Should().NotBe(item2);

            item1.UseMetadataFrom(item2);
            item1.UseDbFieldsFrom(item2);
            item1.Should().Be(item2);
        }

        private Volume GivenVolume()
        {
            return _fixture.Build<Volume>()
                .With(x => x.Metadata, new LazyLoaded<VolumeMetadata>(_fixture.Create<VolumeMetadata>()))
                .Without(x => x.QualityProfile)
                .Without(x => x.MetadataProfile)
                .Without(x => x.Issues)
                .Without(x => x.Name)
                .Without(x => x.ForeignVolumeId)
                .Create();
        }

        [Test]
        public void two_equivalent_volumes_should_be_equal()
        {
            var item1 = GivenVolume();
            var item2 = item1.JsonClone();

            item1.Should().NotBeSameAs(item2);
            item1.Should().Be(item2);
        }

        [Test]
        [TestCaseSource(typeof(EqualityPropertySource<Volume>), "TestCases")]
        public void two_different_volumes_should_not_be_equal(PropertyInfo prop)
        {
            var item1 = GivenVolume();
            var item2 = item1.JsonClone();
            var different = GivenVolume();

            // make item2 different in the property under consideration
            if (prop.PropertyType == typeof(bool))
            {
                prop.SetValue(item2, !(bool)prop.GetValue(item1));
            }
            else
            {
                prop.SetValue(item2, prop.GetValue(different));
            }

            item1.Should().NotBeSameAs(item2);
            item1.Should().NotBe(item2);
        }

        [Test]
        public void metadata_and_db_fields_should_replicate_volume()
        {
            var item1 = GivenVolume();
            var item2 = GivenVolume();

            item1.Should().NotBe(item2);

            item1.UseMetadataFrom(item2);
            item1.UseDbFieldsFrom(item2);
            item1.Should().Be(item2);
        }
    }
}
