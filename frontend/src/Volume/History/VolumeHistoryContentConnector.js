import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { volumeHistoryMarkAsFailed, clearVolumeHistory as clearVolumeHistory, fetchVolumeHistory as fetchVolumeHistory } from 'Store/Actions/volumeHistoryActions';

function createMapStateToProps() {
  return createSelector(
    (state) => state.volumeHistory,
    (volumeHistory) => {
      return volumeHistory;
    }
  );
}

const mapDispatchToProps = {
  fetchVolumeHistory,
  clearVolumeHistory,
  volumeHistoryMarkAsFailed
};

class VolumeHistoryContentConnector extends Component {

  //
  // Lifecycle

  componentDidMount() {
    const {
      volumeId,
      issueId
    } = this.props;

    this.props.fetchVolumeHistory({
      volumeId,
      issueId
    });
  }

  componentWillUnmount() {
    this.props.clearVolumeHistory();
  }

  //
  // Listeners

  onMarkAsFailedPress = (historyId) => {
    const {
      volumeId,
      issueId
    } = this.props;

    this.props.volumeHistoryMarkAsFailed({
      historyId,
      volumeId,
      issueId
    });
  };

  //
  // Render

  render() {
    const {
      component: ViewComponent,
      ...otherProps
    } = this.props;

    return (
      <ViewComponent
        {...otherProps}
        onMarkAsFailedPress={this.onMarkAsFailedPress}
      />
    );
  }
}

VolumeHistoryContentConnector.propTypes = {
  component: PropTypes.elementType.isRequired,
  volumeId: PropTypes.number.isRequired,
  issueId: PropTypes.number,
  fetchVolumeHistory: PropTypes.func.isRequired,
  clearVolumeHistory: PropTypes.func.isRequired,
  volumeHistoryMarkAsFailed: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(VolumeHistoryContentConnector);
