using System.Collections.Generic;
using FluentValidation;
using NzbDrone.Core.Annotations;

namespace NzbDrone.Core.ImportLists.Goodreads
{
    public class GoodreadsIssueshelfImportListSettingsValidator : GoodreadsSettingsBaseValidator<GoodreadsIssueshelfImportListSettings>
    {
        public GoodreadsIssueshelfImportListSettingsValidator()
        : base()
        {
            RuleFor(c => c.IssueshelfIds).NotEmpty();
        }
    }

    public class GoodreadsIssueshelfImportListSettings : GoodreadsSettingsBase<GoodreadsIssueshelfImportListSettings>
    {
        public GoodreadsIssueshelfImportListSettings()
        {
            IssueshelfIds = new string[] { };
        }

        [FieldDefinition(1, Label = "Issueshelves", Type = FieldType.Issueshelf)]
        public IEnumerable<string> IssueshelfIds { get; set; }

        protected override AbstractValidator<GoodreadsIssueshelfImportListSettings> Validator => new GoodreadsIssueshelfImportListSettingsValidator();
    }
}
