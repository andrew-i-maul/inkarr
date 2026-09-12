import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { setAuthorSort as setVolumeSort } from 'Store/Actions/authorIndexActions';
import VolumeIndexTable from './VolumeIndexTable';

function createMapStateToProps() {
  return createSelector(
    (state) => state.app.dimensions,
    (state) => state.authorIndex.tableOptions,
    (state) => state.authorIndex.columns,
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
