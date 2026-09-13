import { createSelector } from 'reselect';

function createVolumeSelector() {
  return createSelector(
    (state, { volumeId }) => volumeId,
    (state) => state.volumes.itemMap,
    (state) => state.volumes.items,
    (volumeId, itemMap, allVolumes) => {
      return allVolumes[itemMap[volumeId]];
    }
  );
}

export default createVolumeSelector;
