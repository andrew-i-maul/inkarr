import { createSelector } from 'reselect';
import createVolumeSelector from './createVolumeSelector';

function createVolumeMetadataProfileSelector() {
  return createSelector(
    (state) => state.settings.metadataProfiles.items,
    createVolumeSelector(),
    (metadataProfiles, volume = {}) => {
      return metadataProfiles.find((profile) => {
        return profile.id === volume.metadataProfileId;
      });
    }
  );
}

export default createVolumeMetadataProfileSelector;
