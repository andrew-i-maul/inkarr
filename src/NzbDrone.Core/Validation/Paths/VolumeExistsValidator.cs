using FluentValidation.Validators;
using NzbDrone.Core.Issues;

namespace NzbDrone.Core.Validation.Paths
{
    public class VolumeExistsValidator : PropertyValidator
    {
        private readonly IVolumeService _volumeService;

        public VolumeExistsValidator(IVolumeService volumeService)
        {
            _volumeService = volumeService;
        }

        protected override string GetDefaultMessageTemplate() => "This volume has already been added";

        protected override bool IsValid(PropertyValidatorContext context)
        {
            if (context.PropertyValue == null)
            {
                return true;
            }

            var foreignVolumeId = context.PropertyValue.ToString();

            return _volumeService.FindById(foreignVolumeId) == null;
        }
    }
}
