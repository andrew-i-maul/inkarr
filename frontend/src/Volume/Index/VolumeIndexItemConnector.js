/* eslint max-params: 0 */
import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import * as commandNames from 'Commands/commandNames';
import { executeCommand } from 'Store/Actions/commandActions';
import createVolumeMetadataProfileSelector from 'Store/Selectors/createVolumeMetadataProfileSelector';
import createVolumeQualityProfileSelector from 'Store/Selectors/createVolumeQualityProfileSelector';
import createVolumeSelector from 'Store/Selectors/createVolumeSelector';
import createExecutingCommandsSelector from 'Store/Selectors/createExecutingCommandsSelector';

function selectShowSearchAction() {
  return createSelector(
    (state) => state.volumeIndex,
    (volumeIndex) => {
      const view = volumeIndex.view;

      switch (view) {
        case 'posters':
          return volumeIndex.posterOptions.showSearchAction;
        case 'banners':
          return volumeIndex.bannerOptions.showSearchAction;
        case 'overview':
          return volumeIndex.overviewOptions.showSearchAction;
        default:
          return volumeIndex.tableOptions.showSearchAction;
      }
    }
  );
}

function createMapStateToProps() {
  return createSelector(
    createVolumeSelector(),
    createVolumeQualityProfileSelector(),
    createVolumeMetadataProfileSelector(),
    selectShowSearchAction(),
    createExecutingCommandsSelector(),
    (
      volume,
      qualityProfile,
      metadataProfile,
      showSearchAction,
      executingCommands
    ) => {

      // If an volume is deleted this selector may fire before the parent
      // selectors, which will result in an undefined volume, if that happens
      // we want to return early here and again in the render function to avoid
      // trying to show an volume that has no information available.

      if (!volume) {
        return {};
      }

      const isRefreshingVolume = executingCommands.some((command) => {
        return (
          command.name === commandNames.REFRESH_VOLUME &&
          command.body.volumeId === volume.id
        );
      });

      const isSearchingVolume = executingCommands.some((command) => {
        return (
          command.name === commandNames.VOLUME_SEARCH &&
          command.body.volumeId === volume.id
        );
      });

      const latestIssue = _.maxBy(volume.issues, (issue) => issue.releaseDate);

      return {
        ...volume,
        qualityProfile,
        metadataProfile,
        latestIssue,
        showSearchAction,
        isRefreshingVolume,
        isSearchingVolume
      };
    }
  );
}

const mapDispatchToProps = {
  dispatchExecuteCommand: executeCommand
};

class VolumeIndexItemConnector extends Component {

  //
  // Listeners

  onRefreshVolumePress = () => {
    this.props.dispatchExecuteCommand({
      name: commandNames.REFRESH_VOLUME,
      volumeId: this.props.id
    });
  };

  onSearchPress = () => {
    this.props.dispatchExecuteCommand({
      name: commandNames.VOLUME_SEARCH,
      volumeId: this.props.id
    });
  };

  //
  // Render

  render() {
    const {
      id,
      component: ItemComponent,
      ...otherProps
    } = this.props;

    if (!id) {
      return null;
    }

    return (
      <ItemComponent
        {...otherProps}
        id={id}
        onRefreshVolumePress={this.onRefreshVolumePress}
        onSearchPress={this.onSearchPress}
      />
    );
  }
}

VolumeIndexItemConnector.propTypes = {
  id: PropTypes.number,
  component: PropTypes.elementType.isRequired,
  dispatchExecuteCommand: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(VolumeIndexItemConnector);
