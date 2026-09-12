import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import * as commandNames from 'Commands/commandNames';
import { executeCommand } from 'Store/Actions/commandActions';
import createAllAuthorSelector from 'Store/Selectors/createAllAuthorsSelector';
import RetagVolumeModalContent from './RetagVolumeModalContent';

function createMapStateToProps() {
  return createSelector(
    (state, { authorIds }) => authorIds,
    createAllVolumeSelector(),
    (authorIds, allVolumes) => {
      const author = _.intersectionWith(allVolumes, authorIds, (s, id) => {
        return s.id === id;
      });

      const sortedVolume = _.orderBy(author, 'sortName');
      const authorNames = _.map(sortedVolume, 'authorName');

      return {
        authorNames
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
      name: commandNames.RETAG_AUTHOR,
      authorIds: this.props.authorIds,
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
  authorIds: PropTypes.arrayOf(PropTypes.number).isRequired,
  onModalClose: PropTypes.func.isRequired,
  executeCommand: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(RetagVolumeModalContentConnector);
