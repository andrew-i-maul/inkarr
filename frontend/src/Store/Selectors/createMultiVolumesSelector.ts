import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';
import Volume from 'Volume/Volume';

function createMultiVolumesSelector(volumeIds: number[]) {
  return createSelector(
    (state: AppState) => state.volumes.itemMap,
    (state: AppState) => state.volumes.items,
    (itemMap, allVolumes) => {
      return volumeIds.reduce((acc: Volume[], volumeId) => {
        const volume = allVolumes[itemMap[volumeId]];

        if (volume) {
          acc.push(volume);
        }

        return acc;
      }, []);
    }
  );
}

export default createMultiVolumesSelector;
