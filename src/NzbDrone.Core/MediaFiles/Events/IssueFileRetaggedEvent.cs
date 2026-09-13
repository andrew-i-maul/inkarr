using System;
using System.Collections.Generic;
using NzbDrone.Common.Messaging;
using NzbDrone.Core.Issues;

namespace NzbDrone.Core.MediaFiles.Events
{
    public class IssueFileRetaggedEvent : IEvent
    {
        public Volume Volume { get; private set; }
        public IssueFile IssueFile { get; private set; }
        public Dictionary<string, Tuple<string, string>> Diff { get; private set; }
        public bool Scrubbed { get; private set; }

        public IssueFileRetaggedEvent(Volume volume,
                                      IssueFile issueFile,
                                      Dictionary<string, Tuple<string, string>> diff,
                                      bool scrubbed)
        {
            Volume = volume;
            IssueFile = issueFile;
            Diff = diff;
            Scrubbed = scrubbed;
        }
    }
}
