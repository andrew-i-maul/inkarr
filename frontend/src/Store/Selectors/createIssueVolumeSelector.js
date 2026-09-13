import { createSelector } from 'reselect';
import createIssueSelector from './createIssueSelector';

function createIssueVolumeSelector() {
  return createSelector(
    createIssueSelector(),
    (state) => state.volumes.itemMap,
    (state) => state.volumes.items,
    (issue, volumeMap, allVolumes) => {
      return allVolumes[volumeMap[issue.volumeId]];
    }
  );
}

export default createIssueVolumeSelector;
