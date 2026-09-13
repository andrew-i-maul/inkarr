using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Notifications.Goodreads
{
    public class GoodreadsIssueshelfNotificationSettingsValidator : GoodreadsSettingsBaseValidator<GoodreadsIssueshelfNotificationSettings>
    {
        public GoodreadsIssueshelfNotificationSettingsValidator()
        : base()
        {
            RuleFor(c => c.RemoveIds).NotEmpty().When(c => !c.AddIds.Any());
            RuleFor(c => c.AddIds).NotEmpty().When(c => !c.RemoveIds.Any());
        }
    }

    public class GoodreadsIssueshelfNotificationSettings : GoodreadsSettingsBase<GoodreadsIssueshelfNotificationSettings>
    {
        private static readonly GoodreadsIssueshelfNotificationSettingsValidator Validator = new GoodreadsIssueshelfNotificationSettingsValidator();

        public GoodreadsIssueshelfNotificationSettings()
        {
            RemoveIds = new string[] { };
            AddIds = new string[] { };
        }

        [FieldDefinition(1, Label = "Remove from Issueshelves", Type = FieldType.Issueshelf)]
        public IEnumerable<string> RemoveIds { get; set; }

        [FieldDefinition(1, Label = "Add to Issueshelves", Type = FieldType.Issueshelf)]
        public IEnumerable<string> AddIds { get; set; }

        public override NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
