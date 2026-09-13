/* eslint max-params: 0 */
import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { toggleIssuesMonitored } from 'Store/Actions/issueActions';
import { executeCommand } from 'Store/Actions/commandActions';
import { setSeriesSort, setSeriesTableOption } from 'Store/Actions/seriesActions';
import createVolumeSelector from 'Store/Selectors/createVolumeSelector';
import createCommandsSelector from 'Store/Selectors/createCommandsSelector';
import createDimensionsSelector from 'Store/Selectors/createDimensionsSelector';
import createUISettingsSelector from 'Store/Selectors/createUISettingsSelector';
import VolumeDetailsSeries from './VolumeDetailsSeries';

function createMapStateToProps() {
  return createSelector(
    (state, { seriesId }) => seriesId,
    (state) => state.issues,
    createVolumeSelector(),
    (state) => state.series,
    createCommandsSelector(),
    createDimensionsSelector(),
    createUISettingsSelector(),
    (seriesId, issues, volume, series, commands, dimensions, uiSettings) => {

      const currentSeries = _.find(series.items, { id: seriesId });

      const issueIds = currentSeries.links.map((x) => x.issueId);
      const positionMap = currentSeries.links.reduce((acc, curr) => {
        acc[curr.issueId] = curr.position;
        return acc;
      }, {});

      const issuesInSeries = _.filter(issues.items, (issue) => issueIds.includes(issue.id));

      let sortDir = 'asc';

      if (series.sortDirection === 'descending') {
        sortDir = 'desc';
      }

      let sortedIssues = [];
      if (series.sortKey === 'position') {
        sortedIssues = issuesInSeries.sort((a, b) => {
          const apos = positionMap[a.id] || '';
          const bpos = positionMap[b.id] || '';
          return apos.localeCompare(bpos, undefined, { numeric: true, sensivity: 'base' });
        });
      } else {
        sortedIssues = _.orderBy(issuesInSeries, series.sortKey, sortDir);
      }

      return {
        id: currentSeries.id,
        label: currentSeries.title,
        items: sortedIssues,
        positionMap,
        columns: series.columns,
        sortKey: series.sortKey,
        sortDirection: series.sortDirection,
        volumeMonitored: volume.monitored,
        isSmallScreen: dimensions.isSmallScreen,
        uiSettings
      };
    }
  );
}

const mapDispatchToProps = {
  toggleIssuesMonitored: toggleIssuesMonitored,
  setSeriesTableOption,
  dispatchSetSeriesSort: setSeriesSort,
  executeCommand
};

class VolumeDetailsSeasonConnector extends Component {

  //
  // Listeners

  onTableOptionChange = (payload) => {
    this.props.setSeriesTableOption(payload);
  };

  onSortPress = (sortKey) => {
    this.props.dispatchSetSeriesSort({ sortKey });
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
      <VolumeDetailsSeries
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
  setSeriesTableOption: PropTypes.func.isRequired,
  dispatchSetSeriesSort: PropTypes.func.isRequired,
  executeCommand: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(VolumeDetailsSeasonConnector);
