/* eslint max-params: 0 */
import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { toggleAuthorMonitored } from 'Store/Actions/authorActions';
import createAuthorSelector from 'Store/Selectors/createAuthorSelector';
import createDimensionsSelector from 'Store/Selectors/createDimensionsSelector';
import VolumeDetailsHeader from './VolumeDetailsHeader';

function createMapStateToProps() {
  return createSelector(
    (state) => state.authors,
    createAuthorSelector(),
    createDimensionsSelector(),
    (authors, author, dimensions) => {
      const alternateTitles = _.reduce(author.alternateTitles, (acc, alternateTitle) => {
        if ((alternateTitle.seasonNumber === -1 || alternateTitle.seasonNumber === undefined) &&
            (alternateTitle.sceneSeasonNumber === -1 || alternateTitle.sceneSeasonNumber === undefined)) {
          acc.push(alternateTitle.title);
        }

        return acc;
      }, []);

      return {
        ...author,
        isSaving: authors.isSaving,
        alternateTitles,
        isSmallScreen: dimensions.isSmallScreen
      };
    }
  );
}

const mapDispatchToProps = {
  toggleVolumeMonitored: toggleAuthorMonitored
};

class VolumeDetailsHeaderConnector extends Component {

  //
  // Listeners

  onMonitorTogglePress = (monitored) => {
    this.props.toggleVolumeMonitored({
      authorId: this.props.authorId,
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
  authorId: PropTypes.number.isRequired,
  toggleVolumeMonitored: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(VolumeDetailsHeaderConnector);
