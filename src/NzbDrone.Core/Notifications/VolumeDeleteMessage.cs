using NzbDrone.Core.Issues;

namespace NzbDrone.Core.Notifications
{
    public class VolumeDeleteMessage
    {
        public string Message { get; set; }
        public Volume Volume { get; set; }
        public bool DeletedFiles { get; set; }
        public string DeletedFilesMessage { get; set; }

        public override string ToString()
        {
            return Message;
        }

        public VolumeDeleteMessage(Volume volume, bool deleteFiles)
        {
            Volume = volume;
            DeletedFiles = deleteFiles;
            DeletedFilesMessage = DeletedFiles ?
                "Volume removed and all files were deleted" :
                "Volume removed, files were not deleted";
            Message = volume.Name + " - " + DeletedFilesMessage;
        }
    }
}
