import { createSelector } from 'reselect';
import createVolumeSelector from './createVolumeSelector';

function createVolumeQualityProfileSelector() {
  return createSelector(
    (state) => state.settings.qualityProfiles.items,
    createVolumeSelector(),
    (qualityProfiles, volume = {}) => {
      return qualityProfiles.find((profile) => {
        return profile.id === volume.qualityProfileId;
      });
    }
  );
}

export default createVolumeQualityProfileSelector;
