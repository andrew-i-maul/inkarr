import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { deleteVolume } from 'Store/Actions/volumeActions';
import createVolumeSelector from 'Store/Selectors/createVolumeSelector';
import DeleteVolumeModalContent from './DeleteVolumeModalContent';

function createMapStateToProps() {
  return createSelector(
    createVolumeSelector(),
    (volume) => {
      return volume;
    }
  );
}

const mapDispatchToProps = {
  deleteVolume: deleteVolume
};

class DeleteVolumeModalContentConnector extends Component {

  //
  // Listeners

  onDeletePress = (deleteFiles, addImportListExclusion) => {
    this.props.deleteVolume({
      id: this.props.volumeId,
      deleteFiles,
      addImportListExclusion
    });

    this.props.onModalClose(true);
  };

  //
  // Render

  render() {
    return (
      <DeleteVolumeModalContent
        {...this.props}
        onDeletePress={this.onDeletePress}
      />
    );
  }
}

DeleteVolumeModalContentConnector.propTypes = {
  volumeId: PropTypes.number.isRequired,
  onModalClose: PropTypes.func.isRequired,
  deleteVolume: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(DeleteVolumeModalContentConnector);
