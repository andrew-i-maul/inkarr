using FluentValidation;
using FluentValidation.Results;
using NzbDrone.Core.Validation;
using NzbDrone.Core.Validation.Paths;

namespace NzbDrone.Core.Issues
{
    public interface IAddVolumeValidator
    {
        ValidationResult Validate(Volume instance);
    }

    public class AddVolumeValidator : AbstractValidator<Volume>, IAddVolumeValidator
    {
        public AddVolumeValidator(RootFolderValidator rootFolderValidator,
                                  RecycleBinValidator recycleBinValidator,
                                  VolumePathValidator volumePathValidator,
                                  VolumeAncestorValidator volumeAncestorValidator,
                                  QualityProfileExistsValidator qualityProfileExistsValidator,
                                  MetadataProfileExistsValidator metadataProfileExistsValidator)
        {
            RuleFor(c => c.Path).Cascade(CascadeMode.Stop)
                                .IsValidPath()
                                .SetValidator(rootFolderValidator)
                                .SetValidator(recycleBinValidator)
                                .SetValidator(volumePathValidator)
                                .SetValidator(volumeAncestorValidator);

            RuleFor(c => c.QualityProfileId).SetValidator(qualityProfileExistsValidator);

            RuleFor(c => c.MetadataProfileId).SetValidator(metadataProfileExistsValidator);
        }
    }
}
