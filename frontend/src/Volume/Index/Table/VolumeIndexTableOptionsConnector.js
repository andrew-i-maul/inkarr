import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import VolumeIndexTableOptions from './VolumeIndexTableOptions';

function createMapStateToProps() {
  return createSelector(
    (state) => state.authorIndex.tableOptions,
    (tableOptions) => {
      return tableOptions;
    }
  );
}

export default connect(createMapStateToProps)(VolumeIndexTableOptions);
