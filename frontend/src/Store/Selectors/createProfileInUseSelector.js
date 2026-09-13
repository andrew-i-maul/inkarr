import _ from 'lodash';
import { createSelector } from 'reselect';
import createAllVolumesSelector from './createAllVolumesSelector';

function createProfileInUseSelector(profileProp) {
  return createSelector(
    (state, { id }) => id,
    createAllVolumesSelector(),
    (state) => state.settings.importLists.items,
    (id, volume, lists) => {
      if (!id) {
        return false;
      }

      if (_.some(volume, { [profileProp]: id }) || _.some(lists, { [profileProp]: id })) {
        return true;
      }

      return false;
    }
  );
}

export default createProfileInUseSelector;
