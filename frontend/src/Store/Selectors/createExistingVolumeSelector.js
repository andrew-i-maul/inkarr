import _ from 'lodash';
import { createSelector } from 'reselect';
import createAllVolumesSelector from './createAllVolumesSelector';

function createExistingVolumeSelector() {
  return createSelector(
    (state, { titleSlug }) => titleSlug,
    createAllVolumesSelector(),
    (titleSlug, volume) => {
      return _.some(volume, { titleSlug });
    }
  );
}

export default createExistingVolumeSelector;
