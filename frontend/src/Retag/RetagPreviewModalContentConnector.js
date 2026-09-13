import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import * as commandNames from 'Commands/commandNames';
import { executeCommand } from 'Store/Actions/commandActions';
import { fetchRetagPreview } from 'Store/Actions/retagPreviewActions';
import createVolumeSelector from 'Store/Selectors/createVolumeSelector';
import RetagPreviewModalContent from './RetagPreviewModalContent';

function createMapStateToProps() {
  return createSelector(
    (state) => state.retagPreview,
    createVolumeSelector(),
    (retagPreview, volume) => {
      const props = { ...retagPreview };
      props.isFetching = retagPreview.isFetching;
      props.isPopulated = retagPreview.isPopulated;
      props.error = retagPreview.error;
      props.path = volume.path;

      return props;
    }
  );
}

const mapDispatchToProps = {
  fetchRetagPreview,
  executeCommand
};

class RetagPreviewModalContentConnector extends Component {

  //
  // Lifecycle

  componentDidMount() {
    const {
      volumeId,
      issueId
    } = this.props;

    this.props.fetchRetagPreview({
      volumeId,
      issueId
    });
  }

  //
  // Listeners

  onRetagPress = (files, updateCovers, embedMetadata) => {
    this.props.executeCommand({
      name: commandNames.RETAG_FILES,
      volumeId: this.props.volumeId,
      updateCovers,
      embedMetadata,
      files
    });

    this.props.onModalClose();
  };

  //
  // Render

  render() {
    return (
      <RetagPreviewModalContent
        {...this.props}
        onRetagPress={this.onRetagPress}
      />
    );
  }
}

RetagPreviewModalContentConnector.propTypes = {
  volumeId: PropTypes.number.isRequired,
  issueId: PropTypes.number,
  isPopulated: PropTypes.bool.isRequired,
  isFetching: PropTypes.bool.isRequired,
  fetchRetagPreview: PropTypes.func.isRequired,
  executeCommand: PropTypes.func.isRequired,
  onModalClose: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(RetagPreviewModalContentConnector);
