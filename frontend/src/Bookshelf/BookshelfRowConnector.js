import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { toggleVolumeMonitored } from 'Store/Actions/volumeActions';
import { toggleIssuesMonitored } from 'Store/Actions/issueActions';
import createVolumeSelector from 'Store/Selectors/createVolumeSelector';
import BookshelfRow from './BookshelfRow';

// Use a const to share the reselect cache between instances
const getIssueMap = createSelector(
  (state) => state.issues.items,
  (issues) => {
    return issues.reduce((acc, curr) => {
      (acc[curr.volumeId] = acc[curr.volumeId] || []).push(curr);
      return acc;
    }, {});
  }
);

function createMapStateToProps() {
  return createSelector(
    createVolumeSelector(),
    getIssueMap,
    (volume, issueMap) => {
      const issuesInVolume = issueMap.hasOwnProperty(volume.id) ? issueMap[volume.id] : [];
      const sortedIssues = _.orderBy(issuesInVolume, 'releaseDate', 'desc');

      return {
        ...volume,
        volumeId: volume.id,
        volumeName: volume.volumeName,
        monitored: volume.monitored,
        status: volume.status,
        isSaving: volume.isSaving,
        issues: sortedIssues
      };
    }
  );
}

const mapDispatchToProps = {
  toggleVolumeMonitored,
  toggleIssuesMonitored
};

class BookshelfRowConnector extends Component {

  //
  // Listeners

  onVolumeMonitoredPress = () => {
    const {
      volumeId,
      monitored
    } = this.props;

    this.props.toggleVolumeMonitored({
      volumeId,
      monitored: !monitored
    });
  };

  onIssueMonitoredPress = (issueId, monitored) => {
    const issueIds = [issueId];
    this.props.toggleIssuesMonitored({
      issueIds,
      monitored
    });
  };

  //
  // Render

  render() {
    return (
      <BookshelfRow
        {...this.props}
        onVolumeMonitoredPress={this.onVolumeMonitoredPress}
        onIssueMonitoredPress={this.onIssueMonitoredPress}
      />
    );
  }
}

BookshelfRowConnector.propTypes = {
  volumeId: PropTypes.number.isRequired,
  monitored: PropTypes.bool.isRequired,
  toggleVolumeMonitored: PropTypes.func.isRequired,
  toggleIssuesMonitored: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(BookshelfRowConnector);
