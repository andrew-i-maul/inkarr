/* eslint max-params: 0 */
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import * as commandNames from 'Commands/commandNames';
import withScrollPosition from 'Components/withScrollPosition';
import { saveVolumeEditor, setVolumeFilter, setVolumeSort, setVolumeTableOption, setVolumeView } from 'Store/Actions/volumeIndexActions';
import { executeCommand } from 'Store/Actions/commandActions';
import scrollPositions from 'Store/scrollPositions';
import createVolumeClientSideCollectionItemsSelector from 'Store/Selectors/createVolumeClientSideCollectionItemsSelector';
import createCommandExecutingSelector from 'Store/Selectors/createCommandExecutingSelector';
import createDimensionsSelector from 'Store/Selectors/createDimensionsSelector';
import VolumeIndex from './VolumeIndex';

function createMapStateToProps() {
  return createSelector(
    createVolumeClientSideCollectionItemsSelector('volumeIndex'),
    createCommandExecutingSelector(commandNames.BULK_REFRESH_VOLUME),
    createCommandExecutingSelector(commandNames.RSS_SYNC),
    createCommandExecutingSelector(commandNames.RENAME_VOLUME),
    createCommandExecutingSelector(commandNames.RETAG_VOLUME),
    createDimensionsSelector(),
    (
      volume,
      isRefreshingVolume,
      isRssSyncExecuting,
      isOrganizingVolume,
      isRetaggingVolume,
      dimensionsState
    ) => {
      return {
        ...volume,
        isRefreshingVolume,
        isRssSyncExecuting,
        isOrganizingVolume,
        isRetaggingVolume,
        isSmallScreen: dimensionsState.isSmallScreen
      };
    }
  );
}

function createMapDispatchToProps(dispatch, props) {
  return {
    onTableOptionChange(payload) {
      dispatch(setVolumeTableOption(payload));
    },

    onSortSelect(sortKey) {
      dispatch(setVolumeSort({ sortKey }));
    },

    onFilterSelect(selectedFilterKey) {
      dispatch(setVolumeFilter({ selectedFilterKey }));
    },

    dispatchSetVolumeView(view) {
      dispatch(setVolumeView({ view }));
    },

    dispatchSaveVolumeEditor(payload) {
      dispatch(saveVolumeEditor(payload));
    },

    onRefreshVolumePress(items) {
      dispatch(executeCommand({
        name: commandNames.BULK_REFRESH_VOLUME,
        volumeIds: items
      }));
    },

    onRssSyncPress() {
      dispatch(executeCommand({
        name: commandNames.RSS_SYNC
      }));
    }
  };
}

class VolumeIndexConnector extends Component {

  //
  // Listeners

  onViewSelect = (view) => {
    this.props.dispatchSetVolumeView(view);
  };

  onSaveSelected = (payload) => {
    this.props.dispatchSaveVolumeEditor(payload);
  };

  onScroll = ({ scrollTop }) => {
    scrollPositions.volumeIndex = scrollTop;
  };

  //
  // Render

  render() {
    return (
      <VolumeIndex
        {...this.props}
        onViewSelect={this.onViewSelect}
        onScroll={this.onScroll}
        onSaveSelected={this.onSaveSelected}
      />
    );
  }
}

VolumeIndexConnector.propTypes = {
  isSmallScreen: PropTypes.bool.isRequired,
  view: PropTypes.string.isRequired,
  dispatchSetVolumeView: PropTypes.func.isRequired,
  dispatchSaveVolumeEditor: PropTypes.func.isRequired
};

export default withScrollPosition(
  connect(createMapStateToProps, createMapDispatchToProps)(VolumeIndexConnector),
  'volumeIndex'
);
