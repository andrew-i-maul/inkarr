import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { saveBookshelf, setBookshelfFilter, setBookshelfSort } from 'Store/Actions/bookshelfActions';
import createVolumeClientSideCollectionItemsSelector from 'Store/Selectors/createVolumeClientSideCollectionItemsSelector';
import createDimensionsSelector from 'Store/Selectors/createDimensionsSelector';
import Bookshelf from './Bookshelf';

function createIssueFetchStateSelector() {
  return createSelector(
    (state) => state.issues.items.length,
    (state) => state.issues.isFetching,
    (state) => state.issues.isPopulated,
    (length, isFetching, isPopulated) => {
      const issueCount = (!isFetching && isPopulated) ? length : 0;
      return {
        issueCount,
        isFetching,
        isPopulated
      };
    }
  );
}

function createMapStateToProps() {
  return createSelector(
    createIssueFetchStateSelector(),
    createVolumeClientSideCollectionItemsSelector('bookshelf'),
    createDimensionsSelector(),
    (issues, volume, dimensionsState) => {
      const isPopulated = issues.isPopulated && volume.isPopulated;
      const isFetching = volume.isFetching || issues.isFetching;
      return {
        ...volume,
        isPopulated,
        isFetching,
        issueCount: issues.issueCount,
        isSmallScreen: dimensionsState.isSmallScreen
      };
    }
  );
}

const mapDispatchToProps = {
  setBookshelfSort,
  setBookshelfFilter,
  saveBookshelf
};

class BookshelfConnector extends Component {

  //
  // Listeners

  onSortPress = (sortKey) => {
    this.props.setBookshelfSort({ sortKey });
  };

  onFilterSelect = (selectedFilterKey) => {
    this.props.setBookshelfFilter({ selectedFilterKey });
  };

  onUpdateSelectedPress = (payload) => {
    this.props.saveBookshelf(payload);
  };

  //
  // Render

  render() {
    return (
      <Bookshelf
        {...this.props}
        onSortPress={this.onSortPress}
        onFilterSelect={this.onFilterSelect}
        onUpdateSelectedPress={this.onUpdateSelectedPress}
      />
    );
  }
}

BookshelfConnector.propTypes = {
  setBookshelfSort: PropTypes.func.isRequired,
  setBookshelfFilter: PropTypes.func.isRequired,
  saveBookshelf: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(BookshelfConnector);
