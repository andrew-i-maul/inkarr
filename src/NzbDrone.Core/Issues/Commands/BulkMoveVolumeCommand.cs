using System;
using System.Collections.Generic;
using NzbDrone.Core.Messaging.Commands;

namespace NzbDrone.Core.Issues.Commands
{
    public class BulkMoveVolumeCommand : Command
    {
        public List<BulkMoveVolume> Volume { get; set; }
        public string DestinationRootFolder { get; set; }

        public override bool SendUpdatesToClient => true;
        public override bool RequiresDiskAccess => true;
    }

    public class BulkMoveVolume : IEquatable<BulkMoveVolume>
    {
        public int VolumeId { get; set; }
        public string SourcePath { get; set; }

        public bool Equals(BulkMoveVolume other)
        {
            if (other == null)
            {
                return false;
            }

            return VolumeId.Equals(other.VolumeId);
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return VolumeId.Equals(((BulkMoveVolume)obj).VolumeId);
        }

        public override int GetHashCode()
        {
            return VolumeId.GetHashCode();
        }
    }
}
