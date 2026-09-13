import { createSelector } from 'reselect';
import createIssueVolumeSelector from './createIssueVolumeSelector';

function createIssueQualityProfileSelector() {
  return createSelector(
    (state) => state.settings.qualityProfiles.items,
    createIssueVolumeSelector(),
    (qualityProfiles, volume) => {
      if (!volume) {
        return {};
      }

      return qualityProfiles.find((profile) => profile.id === volume.qualityProfileId);
    }
  );
}

export default createIssueQualityProfileSelector;
