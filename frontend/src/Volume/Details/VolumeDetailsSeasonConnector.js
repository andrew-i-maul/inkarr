/* eslint max-params: 0 */
import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { setVolumeDetailsId, setVolumeDetailsSort } from 'Store/Actions/volumeDetailsActions';
import { setIssuesTableOption, toggleIssuesMonitored } from 'Store/Actions/issueActions';
import { executeCommand } from 'Store/Actions/commandActions';
import createVolumeSelector from 'Store/Selectors/createVolumeSelector';
import createClientSideCollectionSelector from 'Store/Selectors/createClientSideCollectionSelector';
import createDimensionsSelector from 'Store/Selectors/createDimensionsSelector';
import createUISettingsSelector from 'Store/Selectors/createUISettingsSelector';
import VolumeDetailsSeason from './VolumeDetailsSeason';

function createMapStateToProps() {
  return createSelector(
    createClientSideCollectionSelector('issues', 'volumeDetails'),
    createVolumeSelector(),
    createDimensionsSelector(),
    createUISettingsSelector(),
    (issues, volume, dimensions, uiSettings) => {

      const issuesInGroup = issues.items;

      let sortDir = 'asc';

      if (issues.sortDirection === 'descending') {
        sortDir = 'desc';
      }

      const sortedIssues = _.orderBy(issuesInGroup, issues.sortKey, sortDir);

      return {
        items: sortedIssues,
        columns: issues.columns,
        sortKey: issues.sortKey,
        sortDirection: issues.sortDirection,
        volumeMonitored: volume.monitored,
        isSmallScreen: dimensions.isSmallScreen,
        uiSettings
      };
    }
  );
}

const mapDispatchToProps = {
  setVolumeDetailsId: setVolumeDetailsId,
  setVolumeDetailsSort: setVolumeDetailsSort,
  toggleIssuesMonitored: toggleIssuesMonitored,
  setIssuesTableOption: setIssuesTableOption,
  executeCommand
};

class VolumeDetailsSeasonConnector extends Component {

  //
  // Lifecycle

  componentDidMount() {
    this.props.setVolumeDetailsId({ volumeId: this.props.volumeId });
  }

  //
  // Listeners

  onTableOptionChange = (payload) => {
    this.props.setIssuesTableOption(payload);
  };

  onSortPress = (sortKey) => {
    this.props.setVolumeDetailsSort({ sortKey });
  };

  onMonitorIssuePress = (issueIds, monitored) => {
    this.props.toggleIssuesMonitored({
      issueIds,
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
  volumeId: PropTypes.number.isRequired,
  toggleIssuesMonitored: PropTypes.func.isRequired,
  setIssuesTableOption: PropTypes.func.isRequired,
  setVolumeDetailsId: PropTypes.func.isRequired,
  setVolumeDetailsSort: PropTypes.func.isRequired,
  executeCommand: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(VolumeDetailsSeasonConnector);
