import { createSelector } from 'reselect';
import createAllVolumesSelector from './createAllVolumesSelector';

function createVolumeCountSelector() {
  return createSelector(
    createAllVolumesSelector(),
    (state) => state.volumes.error,
    (state) => state.volumes.isFetching,
    (state) => state.volumes.isPopulated,
    (volumes, error, isFetching, isPopulated) => {
      return {
        count: volumes.length,
        error,
        isFetching,
        isPopulated
      };
    }
  );
}

export default createVolumeCountSelector;
