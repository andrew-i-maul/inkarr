/* eslint max-params: 0 */
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import * as commandNames from 'Commands/commandNames';
import withScrollPosition from 'Components/withScrollPosition';
import { saveBookEditor, setBookFilter, setBookSort, setBookTableOption, setBookView } from 'Store/Actions/bookIndexActions';
import { executeCommand } from 'Store/Actions/commandActions';
import scrollPositions from 'Store/scrollPositions';
import createBookClientSideCollectionItemsSelector from 'Store/Selectors/createBookClientSideCollectionItemsSelector';
import createCommandExecutingSelector from 'Store/Selectors/createCommandExecutingSelector';
import createDimensionsSelector from 'Store/Selectors/createDimensionsSelector';
import IssueIndex from './IssueIndex';

function createMapStateToProps() {
  return createSelector(
    createIssueClientSideCollectionItemsSelector('bookIndex'),
    createCommandExecutingSelector(commandNames.BULK_REFRESH_AUTHOR),
    createCommandExecutingSelector(commandNames.BULK_REFRESH_BOOK),
    createCommandExecutingSelector(commandNames.RSS_SYNC),
    createCommandExecutingSelector(commandNames.CUTOFF_UNMET_BOOK_SEARCH),
    createCommandExecutingSelector(commandNames.MISSING_BOOK_SEARCH),
    createDimensionsSelector(),
    (
      book,
      isRefreshingVolumeCommand,
      isRefreshingIssueCommand,
      isRssSyncExecuting,
      isCutoffIssuesSearch,
      isMissingIssuesSearch,
      dimensionsState
    ) => {
      const isRefreshingIssue = isRefreshingIssueCommand || isRefreshingVolumeCommand;
      return {
        ...book,
        isRefreshingIssue,
        isRssSyncExecuting,
        isSearching: isCutoffIssuesSearch || isMissingIssuesSearch,
        isSmallScreen: dimensionsState.isSmallScreen
      };
    }
  );
}

function createMapDispatchToProps(dispatch, props) {
  return {
    onTableOptionChange(payload) {
      dispatch(setIssueTableOption(payload));
    },

    onSortSelect(sortKey) {
      dispatch(setIssueSort({ sortKey }));
    },

    onFilterSelect(selectedFilterKey) {
      dispatch(setIssueFilter({ selectedFilterKey }));
    },

    dispatchSetIssueView(view) {
      dispatch(setIssueView({ view }));
    },

    dispatchSaveIssueEditor(payload) {
      dispatch(saveIssueEditor(payload));
    },

    onRefreshIssuePress(items) {
      dispatch(executeCommand({
        name: commandNames.BULK_REFRESH_BOOK,
        bookIds: items
      }));
    },

    onRssSyncPress() {
      dispatch(executeCommand({
        name: commandNames.RSS_SYNC
      }));
    },

    onSearchPress(items) {
      dispatch(executeCommand({
        name: commandNames.BOOK_SEARCH,
        bookIds: items
      }));
    }
  };
}

class IssueIndexConnector extends Component {

  //
  // Listeners

  onViewSelect = (view) => {
    this.props.dispatchSetIssueView(view);
  };

  onSaveSelected = (payload) => {
    this.props.dispatchSaveIssueEditor(payload);
  };

  onScroll = ({ scrollTop }) => {
    scrollPositions.bookIndex = scrollTop;
  };

  //
  // Render

  render() {
    return (
      <IssueIndex
        {...this.props}
        onViewSelect={this.onViewSelect}
        onScroll={this.onScroll}
        onSaveSelected={this.onSaveSelected}
      />
    );
  }
}

IssueIndexConnector.propTypes = {
  isSmallScreen: PropTypes.bool.isRequired,
  view: PropTypes.string.isRequired,
  dispatchSetIssueView: PropTypes.func.isRequired,
  dispatchSaveIssueEditor: PropTypes.func.isRequired
};

export default withScrollPosition(
  connect(createMapStateToProps, createMapDispatchToProps)(IssueIndexConnector),
  'bookIndex'
);

