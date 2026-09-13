import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import * as commandNames from 'Commands/commandNames';
import { executeCommand } from 'Store/Actions/commandActions';
import createAllVolumeSelector from 'Store/Selectors/createAllVolumesSelector';
import RetagVolumeModalContent from './RetagVolumeModalContent';

function createMapStateToProps() {
  return createSelector(
    (state, { volumeIds }) => volumeIds,
    createAllVolumeSelector(),
    (volumeIds, allVolumes) => {
      const volume = _.intersectionWith(allVolumes, volumeIds, (s, id) => {
        return s.id === id;
      });

      const sortedVolume = _.orderBy(volume, 'sortName');
      const volumeNames = _.map(sortedVolume, 'volumeName');

      return {
        volumeNames
      };
    }
  );
}

const mapDispatchToProps = {
  executeCommand
};

class RetagVolumeModalContentConnector extends Component {

  //
  // Listeners

  onRetagVolumePress = (updateCovers, embedMetadata) => {
    this.props.executeCommand({
      name: commandNames.RETAG_VOLUME,
      volumeIds: this.props.volumeIds,
      updateCovers,
      embedMetadata
    });

    this.props.onModalClose(true);
  };

  //
  // Render

  render(props) {
    return (
      <RetagVolumeModalContent
        {...this.props}
        onRetagVolumePress={this.onRetagVolumePress}
      />
    );
  }
}

RetagVolumeModalContentConnector.propTypes = {
  volumeIds: PropTypes.arrayOf(PropTypes.number).isRequired,
  onModalClose: PropTypes.func.isRequired,
  executeCommand: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(RetagVolumeModalContentConnector);
