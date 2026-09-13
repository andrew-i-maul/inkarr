namespace NzbDrone.Core.IndexerSearch.Definitions
{
    public class VolumeSearchCriteria : SearchCriteriaBase
    {
        public override string ToString()
        {
            return $"[{Volume.Name}]";
        }
    }
}
