import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { setBookSort as setIssueSort } from 'Store/Actions/bookIndexActions';
import IssueIndexTable from './IssueIndexTable';

function createMapStateToProps() {
  return createSelector(
    (state) => state.app.dimensions,
    (state) => state.bookIndex.tableOptions,
    (state) => state.bookIndex.columns,
    (dimensions, tableOptions, columns) => {
      return {
        isSmallScreen: dimensions.isSmallScreen,
        showBanners: tableOptions.showBanners,
        columns
      };
    }
  );
}

function createMapDispatchToProps(dispatch, props) {
  return {
    onSortPress(sortKey) {
      dispatch(setIssueSort({ sortKey }));
    }
  };
}

export default connect(createMapStateToProps, createMapDispatchToProps)(IssueIndexTable);
