import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { saveVolume, setVolumeValue } from 'Store/Actions/volumeActions';
import createVolumeSelector from 'Store/Selectors/createVolumeSelector';
import selectSettings from 'Store/Selectors/selectSettings';
import EditVolumeModalContent from './EditVolumeModalContent';

function createIsPathChangingSelector() {
  return createSelector(
    (state) => state.volumes.pendingChanges,
    createVolumeSelector(),
    (pendingChanges, volume) => {
      const path = pendingChanges.path;

      if (path == null) {
        return false;
      }

      return volume.path !== path;
    }
  );
}

function createMapStateToProps() {
  return createSelector(
    (state) => state.volumes,
    (state) => state.settings.metadataProfiles,
    createVolumeSelector(),
    createIsPathChangingSelector(),
    (volumesState, metadataProfiles, volume, isPathChanging) => {
      const {
        isSaving,
        saveError,
        pendingChanges
      } = volumesState;

      const volumeSettings = _.pick(volume, [
        'monitored',
        'monitorNewItems',
        'qualityProfileId',
        'metadataProfileId',
        'path',
        'tags'
      ]);

      const settings = selectSettings(volumeSettings, pendingChanges, saveError);

      return {
        volumeName: volume.volumeName,
        isSaving,
        saveError,
        isPathChanging,
        originalPath: volume.path,
        item: settings.settings,
        showMetadataProfile: metadataProfiles.items.length > 1,
        ...settings
      };
    }
  );
}

const mapDispatchToProps = {
  dispatchSetVolumeValue: setVolumeValue,
  dispatchSaveVolume: saveVolume
};

class EditVolumeModalContentConnector extends Component {

  //
  // Lifecycle

  componentDidUpdate(prevProps, prevState) {
    if (prevProps.isSaving && !this.props.isSaving && !this.props.saveError) {
      this.props.onModalClose();
    }
  }

  //
  // Listeners

  onInputChange = ({ name, value }) => {
    this.props.dispatchSetVolumeValue({ name, value });
  };

  onSavePress = (moveFiles) => {
    this.props.dispatchSaveVolume({
      id: this.props.volumeId,
      moveFiles
    });
  };

  //
  // Render

  render() {
    return (
      <EditVolumeModalContent
        {...this.props}
        onInputChange={this.onInputChange}
        onSavePress={this.onSavePress}
        onMoveVolumePress={this.onMoveVolumePress}
      />
    );
  }
}

EditVolumeModalContentConnector.propTypes = {
  volumeId: PropTypes.number,
  isSaving: PropTypes.bool.isRequired,
  saveError: PropTypes.object,
  dispatchSetVolumeValue: PropTypes.func.isRequired,
  dispatchSaveVolume: PropTypes.func.isRequired,
  onModalClose: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(EditVolumeModalContentConnector);
