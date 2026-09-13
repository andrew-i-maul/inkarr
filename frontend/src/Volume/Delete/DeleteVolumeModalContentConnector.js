import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { deleteAuthor } from 'Store/Actions/authorActions';
import createAuthorSelector from 'Store/Selectors/createAuthorSelector';
import DeleteVolumeModalContent from './DeleteVolumeModalContent';

function createMapStateToProps() {
  return createSelector(
    createAuthorSelector(),
    (author) => {
      return author;
    }
  );
}

const mapDispatchToProps = {
  deleteVolume: deleteAuthor
};

class DeleteVolumeModalContentConnector extends Component {

  //
  // Listeners

  onDeletePress = (deleteFiles, addImportListExclusion) => {
    this.props.deleteVolume({
      id: this.props.authorId,
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
  authorId: PropTypes.number.isRequired,
  onModalClose: PropTypes.func.isRequired,
  deleteVolume: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(DeleteVolumeModalContentConnector);
