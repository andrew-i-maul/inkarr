import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { addVolume, setVolumeAddDefault } from 'Store/Actions/searchActions';
import createDimensionsSelector from 'Store/Selectors/createDimensionsSelector';
import createSystemStatusSelector from 'Store/Selectors/createSystemStatusSelector';
import selectSettings from 'Store/Selectors/selectSettings';
import AddNewVolumeModalContent from './AddNewVolumeModalContent';

function createMapStateToProps() {
  return createSelector(
    (state) => state.search,
    (state) => state.settings.metadataProfiles,
    createDimensionsSelector(),
    createSystemStatusSelector(),
    (searchState, metadataProfiles, dimensions, systemStatus) => {
      const {
        isAdding,
        addError,
        volumeDefaults
      } = searchState;

      const {
        settings,
        validationErrors,
        validationWarnings
      } = selectSettings(volumeDefaults, {}, addError);

      return {
        isAdding,
        addError,
        showMetadataProfile: metadataProfiles.items.length > 2, // NONE (not allowed for volumes) and one other
        isSmallScreen: dimensions.isSmallScreen,
        validationErrors,
        validationWarnings,
        isWindows: systemStatus.isWindows,
        ...settings
      };
    }
  );
}

const mapDispatchToProps = {
  setVolumeAddDefault,
  addVolume
};

class AddNewVolumeModalContentConnector extends Component {

  //
  // Listeners

  onInputChange = ({ name, value }) => {
    this.props.setVolumeAddDefault({ [name]: value });
  };

  onAddVolumePress = (searchForMissingIssues) => {
    const {
      foreignVolumeId,
      rootFolderPath,
      monitor,
      monitorNewItems,
      qualityProfileId,
      metadataProfileId,
      tags
    } = this.props;

    this.props.addVolume({
      foreignVolumeId,
      rootFolderPath: rootFolderPath.value,
      monitor: monitor.value,
      monitorNewItems: monitorNewItems.value,
      qualityProfileId: qualityProfileId.value,
      metadataProfileId: metadataProfileId.value,
      tags: tags.value,
      searchForMissingIssues
    });
  };

  //
  // Render

  render() {
    return (
      <AddNewVolumeModalContent
        {...this.props}
        onInputChange={this.onInputChange}
        onAddVolumePress={this.onAddVolumePress}
      />
    );
  }
}

AddNewVolumeModalContentConnector.propTypes = {
  foreignVolumeId: PropTypes.string.isRequired,
  rootFolderPath: PropTypes.object,
  monitor: PropTypes.object.isRequired,
  monitorNewItems: PropTypes.object.isRequired,
  qualityProfileId: PropTypes.object,
  metadataProfileId: PropTypes.object,
  tags: PropTypes.object.isRequired,
  onModalClose: PropTypes.func.isRequired,
  setVolumeAddDefault: PropTypes.func.isRequired,
  addVolume: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(AddNewVolumeModalContentConnector);
