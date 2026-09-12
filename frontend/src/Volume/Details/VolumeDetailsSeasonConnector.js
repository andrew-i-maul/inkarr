/* eslint max-params: 0 */
import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { setAuthorDetailsId, setAuthorDetailsSort } from 'Store/Actions/authorDetailsActions';
import { setBooksTableOption, toggleBooksMonitored } from 'Store/Actions/bookActions';
import { executeCommand } from 'Store/Actions/commandActions';
import createAuthorSelector from 'Store/Selectors/createAuthorSelector';
import createClientSideCollectionSelector from 'Store/Selectors/createClientSideCollectionSelector';
import createDimensionsSelector from 'Store/Selectors/createDimensionsSelector';
import createUISettingsSelector from 'Store/Selectors/createUISettingsSelector';
import VolumeDetailsSeason from './VolumeDetailsSeason';

function createMapStateToProps() {
  return createSelector(
    createClientSideCollectionSelector('books', 'authorDetails'),
    createVolumeSelector(),
    createDimensionsSelector(),
    createUISettingsSelector(),
    (books, author, dimensions, uiSettings) => {

      const booksInGroup = books.items;

      let sortDir = 'asc';

      if (books.sortDirection === 'descending') {
        sortDir = 'desc';
      }

      const sortedIssues = _.orderBy(booksInGroup, books.sortKey, sortDir);

      return {
        items: sortedIssues,
        columns: books.columns,
        sortKey: books.sortKey,
        sortDirection: books.sortDirection,
        authorMonitored: author.monitored,
        isSmallScreen: dimensions.isSmallScreen,
        uiSettings
      };
    }
  );
}

const mapDispatchToProps = {
  setVolumeDetailsId,
  setVolumeDetailsSort,
  toggleIssuesMonitored,
  setIssuesTableOption,
  executeCommand
};

class VolumeDetailsSeasonConnector extends Component {

  //
  // Lifecycle

  componentDidMount() {
    this.props.setVolumeDetailsId({ authorId: this.props.authorId });
  }

  //
  // Listeners

  onTableOptionChange = (payload) => {
    this.props.setIssuesTableOption(payload);
  };

  onSortPress = (sortKey) => {
    this.props.setVolumeDetailsSort({ sortKey });
  };

  onMonitorIssuePress = (bookIds, monitored) => {
    this.props.toggleIssuesMonitored({
      bookIds,
      monitored
    });
  };

  //
  // Render

  render() {
    return (
      <VolumeDetailsSeason
        {...this.props}
        onSortPress={this.onSortPress}
        onTableOptionChange={this.onTableOptionChange}
        onMonitorIssuePress={this.onMonitorIssuePress}
      />
    );
  }
}

VolumeDetailsSeasonConnector.propTypes = {
  authorId: PropTypes.number.isRequired,
  toggleIssuesMonitored: PropTypes.func.isRequired,
  setIssuesTableOption: PropTypes.func.isRequired,
  setVolumeDetailsId: PropTypes.func.isRequired,
  setVolumeDetailsSort: PropTypes.func.isRequired,
  executeCommand: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(VolumeDetailsSeasonConnector);
