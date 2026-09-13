import { createSelector, createSelectorCreator, defaultMemoize } from 'reselect';
import hasDifferentItemsOrOrder from 'Utilities/Object/hasDifferentItemsOrOrder';
import createClientSideCollectionSelector from './createClientSideCollectionSelector';

function createUnoptimizedSelector(uiSection) {
  return createSelector(
    createClientSideCollectionSelector('volumes', uiSection),
    (volumes) => {
      const items = volumes.items.map((s) => {
        const {
          id,
          sortName,
          sortNameLastFirst
        } = s;

        return {
          id,
          sortName,
          sortNameLastFirst
        };
      });

      return {
        ...volumes,
        items
      };
    }
  );
}

function volumeListEqual(a, b) {
  return hasDifferentItemsOrOrder(a, b);
}

const createVolumeEqualSelector = createSelectorCreator(
  defaultMemoize,
  volumeListEqual
);

function createVolumeClientSideCollectionItemsSelector(uiSection) {
  return createVolumeEqualSelector(
    createUnoptimizedSelector(uiSection),
    (volume) => volume
  );
}

export default createVolumeClientSideCollectionItemsSelector;
