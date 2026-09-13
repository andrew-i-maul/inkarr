import { createSelector } from 'reselect';

function createAllVolumesSelector() {
  return createSelector(
    (state) => state.volumes,
    (volume) => {
      return volume.items;
    }
  );
}

export default createAllVolumesSelector;
