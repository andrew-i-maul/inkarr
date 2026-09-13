/* eslint max-params: 0 */
import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { toggleVolumeMonitored } from 'Store/Actions/volumeActions';
import createVolumeSelector from 'Store/Selectors/createVolumeSelector';
import createDimensionsSelector from 'Store/Selectors/createDimensionsSelector';
import VolumeDetailsHeader from './VolumeDetailsHeader';

function createMapStateToProps() {
  return createSelector(
    (state) => state.volumes,
    createVolumeSelector(),
    createDimensionsSelector(),
    (volumes, volume, dimensions) => {
      const alternateTitles = _.reduce(volume.alternateTitles, (acc, alternateTitle) => {
        if ((alternateTitle.seasonNumber === -1 || alternateTitle.seasonNumber === undefined) &&
            (alternateTitle.sceneSeasonNumber === -1 || alternateTitle.sceneSeasonNumber === undefined)) {
          acc.push(alternateTitle.title);
        }

        return acc;
      }, []);

      return {
        ...volume,
        isSaving: volumes.isSaving,
        alternateTitles,
        isSmallScreen: dimensions.isSmallScreen
      };
    }
  );
}

const mapDispatchToProps = {
  toggleVolumeMonitored: toggleVolumeMonitored
};

class VolumeDetailsHeaderConnector extends Component {

  //
  // Listeners

  onMonitorTogglePress = (monitored) => {
    this.props.toggleVolumeMonitored({
      volumeId: this.props.volumeId,
      monitored
    });
  };

  //
  // Render

  render() {
    return (
      <VolumeDetailsHeader
        {...this.props}
        onMonitorTogglePress={this.onMonitorTogglePress}
      />
    );
  }
}

VolumeDetailsHeaderConnector.propTypes = {
  volumeId: PropTypes.number.isRequired,
  toggleVolumeMonitored: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(VolumeDetailsHeaderConnector);
