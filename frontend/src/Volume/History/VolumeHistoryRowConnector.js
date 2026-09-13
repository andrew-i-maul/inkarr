import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { fetchHistory, markAsFailed } from 'Store/Actions/historyActions';
import createVolumeSelector from 'Store/Selectors/createVolumeSelector';
import createIssueSelector from 'Store/Selectors/createIssueSelector';
import VolumeHistoryRow from './VolumeHistoryRow';

function createMapStateToProps() {
  return createSelector(
    createVolumeSelector(),
    createIssueSelector(),
    (volume, issue) => {
      return {
        volume,
        issue
      };
    }
  );
}

const mapDispatchToProps = {
  fetchHistory,
  markAsFailed
};

export default connect(createMapStateToProps, mapDispatchToProps)(VolumeHistoryRow);
