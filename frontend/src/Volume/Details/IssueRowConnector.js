/* eslint max-params: 0 */
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import createVolumeSelector from 'Store/Selectors/createVolumeSelector';
import IssueRow from './IssueRow';

const selectIssueFiles = createSelector(
  (state) => state.issueFiles,
  (issueFiles) => {
    const { items } = issueFiles;

    return items.reduce((acc, file) => {
      const issueId = file.issueId;
      if (!acc.hasOwnProperty(issueId)) {
        acc[issueId] = [];
      }

      acc[issueId].push(file);

      return acc;
    }, {});
  }
);

function createMapStateToProps() {
  return createSelector(
    createVolumeSelector(),
    selectIssueFiles,
    (state, { id }) => id,
    (volume = {}, issueFiles, issueId) => {
      const files = issueFiles[issueId] ?? [];
      const issueFile = files[0];

      return {
        volumeMonitored: volume.monitored,
        volumeName: volume.volumeName,
        issueFiles: files,
        indexerFlags: issueFile ? issueFile.indexerFlags : 0
      };
    }
  );
}
export default connect(createMapStateToProps)(IssueRow);
