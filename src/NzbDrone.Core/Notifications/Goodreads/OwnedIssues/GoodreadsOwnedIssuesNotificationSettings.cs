using NzbDrone.Core.Annotations;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Notifications.Goodreads
{
    public enum OwnedIssueCondition
    {
        BrandNew = 10,
        LikeNew = 20,
        VeryGood = 30,
        Good = 40,
        Acceptable = 50,
        Poor = 60
    }

    public class GoodreadsOwnedIssuesNotificationSettings : GoodreadsSettingsBase<GoodreadsOwnedIssuesNotificationSettings>
    {
        private static readonly GoodreadsSettingsBaseValidator<GoodreadsOwnedIssuesNotificationSettings> Validator = new GoodreadsSettingsBaseValidator<GoodreadsOwnedIssuesNotificationSettings>();

        [FieldDefinition(1, Label = "Condition", Type = FieldType.Select, SelectOptions = typeof(OwnedIssueCondition))]
        public int Condition { get; set; } = (int)OwnedIssueCondition.BrandNew;

        [FieldDefinition(1, Label = "Condition Description", Type = FieldType.Textbox)]
        public string Description { get; set; }

        [FieldDefinition(1, Label = "Purchase Location", HelpText = "Will be displayed on Goodreads website", Type = FieldType.Textbox)]
        public string Location { get; set; }

        public override NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
