import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { setVolumeSort as setVolumeSort } from 'Store/Actions/volumeIndexActions';
import VolumeIndexTable from './VolumeIndexTable';

function createMapStateToProps() {
  return createSelector(
    (state) => state.app.dimensions,
    (state) => state.volumeIndex.tableOptions,
    (state) => state.volumeIndex.columns,
    (dimensions, tableOptions, columns) => {
      return {
        isSmallScreen: dimensions.isSmallScreen,
        showBanners: tableOptions.showBanners,
        showTitle: tableOptions.showTitle,
        columns
      };
    }
  );
}

function createMapDispatchToProps(dispatch, props) {
  return {
    onSortPress(sortKey) {
      dispatch(setVolumeSort({ sortKey }));
    }
  };
}

export default connect(createMapStateToProps, createMapDispatchToProps)(VolumeIndexTable);
