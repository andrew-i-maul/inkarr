/* eslint max-params: 0 */
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import * as commandNames from 'Commands/commandNames';
import withScrollPosition from 'Components/withScrollPosition';
import { saveAuthorEditor, setAuthorFilter, setAuthorSort, setAuthorTableOption, setAuthorView } from 'Store/Actions/authorIndexActions';
import { executeCommand } from 'Store/Actions/commandActions';
import scrollPositions from 'Store/scrollPositions';
import createAuthorClientSideCollectionItemsSelector from 'Store/Selectors/createAuthorClientSideCollectionItemsSelector';
import createCommandExecutingSelector from 'Store/Selectors/createCommandExecutingSelector';
import createDimensionsSelector from 'Store/Selectors/createDimensionsSelector';
import VolumeIndex from './VolumeIndex';

function createMapStateToProps() {
  return createSelector(
    createAuthorClientSideCollectionItemsSelector('authorIndex'),
    createCommandExecutingSelector(commandNames.BULK_REFRESH_AUTHOR),
    createCommandExecutingSelector(commandNames.RSS_SYNC),
    createCommandExecutingSelector(commandNames.RENAME_AUTHOR),
    createCommandExecutingSelector(commandNames.RETAG_AUTHOR),
    createDimensionsSelector(),
    (
      author,
      isRefreshingVolume,
      isRssSyncExecuting,
      isOrganizingVolume,
      isRetaggingVolume,
      dimensionsState
    ) => {
      return {
        ...author,
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
        name: commandNames.BULK_REFRESH_AUTHOR,
        authorIds: items
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
    scrollPositions.authorIndex = scrollTop;
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
  'authorIndex'
);
