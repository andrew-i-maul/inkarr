import { push } from 'connected-react-router';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { deleteBook } from 'Store/Actions/bookActions';
import createBookSelector from 'Store/Selectors/createBookSelector';
import DeleteIssueModalContent from './DeleteIssueModalContent';

function createMapStateToProps() {
  return createSelector(
    createBookSelector(),
    (book) => {
      return book;
    }
  );
}

const mapDispatchToProps = {
  push,
  deleteIssue: deleteBook
};

class DeleteIssueModalContentConnector extends Component {

  //
  // Listeners

  onDeletePress = (deleteFiles, addImportListExclusion) => {
    this.props.deleteIssue({
      id: this.props.bookId,
      deleteFiles,
      addImportListExclusion
    });

    this.props.onModalClose(true);

    this.props.push(`${window.Inkarr.urlBase}/author/${this.props.authorSlug}`);
  };

  //
  // Render

  render() {
    return (
      <DeleteIssueModalContent
        {...this.props}
        onDeletePress={this.onDeletePress}
      />
    );
  }
}

DeleteIssueModalContentConnector.propTypes = {
  bookId: PropTypes.number.isRequired,
  authorSlug: PropTypes.string.isRequired,
  push: PropTypes.func.isRequired,
  onModalClose: PropTypes.func.isRequired,
  deleteIssue: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(DeleteIssueModalContentConnector);
