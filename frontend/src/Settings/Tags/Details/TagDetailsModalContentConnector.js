import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import createAllVolumeSelector from 'Store/Selectors/createAllVolumesSelector';
import TagDetailsModalContent from './TagDetailsModalContent';

function findMatchingItems(ids, items) {
  return items.filter((s) => {
    return ids.includes(s.id);
  });
}

function createUnorderedMatchingVolumeSelector() {
  return createSelector(
    (state, { volumeIds }) => volumeIds,
    createAllVolumeSelector(),
    findMatchingItems
  );
}

function createMatchingVolumeSelector() {
  return createSelector(
    createUnorderedMatchingVolumeSelector(),
    (volumes) => {
      return volumes.sort((volumeA, volumeB) => {
        const sortNameA = volumeA.sortName;
        const sortNameB = volumeB.sortName;

        if (sortNameA > sortNameB) {
          return 1;
        } else if (sortNameA < sortNameB) {
          return -1;
        }

        return 0;
      });
    }
  );
}

function createMatchingDelayProfilesSelector() {
  return createSelector(
    (state, { delayProfileIds }) => delayProfileIds,
    (state) => state.settings.delayProfiles.items,
    findMatchingItems
  );
}

function createMatchingImportListsSelector() {
  return createSelector(
    (state, { importListIds }) => importListIds,
    (state) => state.settings.importLists.items,
    findMatchingItems
  );
}

function createMatchingNotificationsSelector() {
  return createSelector(
    (state, { notificationIds }) => notificationIds,
    (state) => state.settings.notifications.items,
    findMatchingItems
  );
}

function createMatchingReleaseProfilesSelector() {
  return createSelector(
    (state, { restrictionIds }) => restrictionIds,
    (state) => state.settings.releaseProfiles.items,
    findMatchingItems
  );
}

function createMatchingIndexersSelector() {
  return createSelector(
    (state, { indexerIds }) => indexerIds,
    (state) => state.settings.indexers.items,
    findMatchingItems
  );
}

function createMatchingDownloadClientsSelector() {
  return createSelector(
    (state, { downloadClientIds }) => downloadClientIds,
    (state) => state.settings.downloadClients.items,
    findMatchingItems
  );
}

function createMapStateToProps() {
  return createSelector(
    createMatchingVolumeSelector(),
    createMatchingDelayProfilesSelector(),
    createMatchingImportListsSelector(),
    createMatchingNotificationsSelector(),
    createMatchingReleaseProfilesSelector(),
    createMatchingIndexersSelector(),
    createMatchingDownloadClientsSelector(),
    (volume, delayProfiles, importLists, notifications, releaseProfiles, indexers, downloadClients) => {
      return {
        volume,
        delayProfiles,
        importLists,
        notifications,
        releaseProfiles,
        indexers,
        downloadClients
      };
    }
  );
}

export default connect(createMapStateToProps)(TagDetailsModalContent);
