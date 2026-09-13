using System.Collections.Generic;

namespace VersOne.Epub
{
    public class EpubIssue
    {
        public string FilePath { get; set; }
        public string Title { get; set; }
        public string Volume { get; set; }
        public List<string> VolumeList { get; set; }
        public EpubSchema Schema { get; set; }
    }
}
