using NzbDrone.Common.Exceptions;

namespace NzbDrone.Core.Exceptions
{
    public class VolumeNotFoundException : NzbDroneException
    {
        public string ForeignVolumeId { get; set; }

        public VolumeNotFoundException(string foreignVolumeId)
            : base($"Volume with id {foreignVolumeId} was not found, it may have been removed from the metadata server.")
        {
            ForeignVolumeId = foreignVolumeId;
        }

        public VolumeNotFoundException(string foreignVolumeId, string message, params object[] args)
            : base(message, args)
        {
            ForeignVolumeId = foreignVolumeId;
        }

        public VolumeNotFoundException(string foreignVolumeId, string message)
            : base(message)
        {
            ForeignVolumeId = foreignVolumeId;
        }
    }
}
