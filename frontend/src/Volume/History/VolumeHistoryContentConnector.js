import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { authorHistoryMarkAsFailed, clearAuthorHistory as clearVolumeHistory, fetchAuthorHistory as fetchVolumeHistory } from 'Store/Actions/authorHistoryActions';

function createMapStateToProps() {
  return createSelector(
    (state) => state.authorHistory,
    (authorHistory) => {
      return authorHistory;
    }
  );
}

const mapDispatchToProps = {
  fetchVolumeHistory,
  clearVolumeHistory,
  authorHistoryMarkAsFailed
};

class VolumeHistoryContentConnector extends Component {

  //
  // Lifecycle

  componentDidMount() {
    const {
      authorId,
      bookId
    } = this.props;

    this.props.fetchVolumeHistory({
      authorId,
      bookId
    });
  }

  componentWillUnmount() {
    this.props.clearVolumeHistory();
  }

  //
  // Listeners

  onMarkAsFailedPress = (historyId) => {
    const {
      authorId,
      bookId
    } = this.props;

    this.props.authorHistoryMarkAsFailed({
      historyId,
      authorId,
      bookId
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
  authorId: PropTypes.number.isRequired,
  bookId: PropTypes.number,
  fetchVolumeHistory: PropTypes.func.isRequired,
  clearVolumeHistory: PropTypes.func.isRequired,
  authorHistoryMarkAsFailed: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(VolumeHistoryContentConnector);
