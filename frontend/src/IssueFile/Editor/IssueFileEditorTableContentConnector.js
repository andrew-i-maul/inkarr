/* eslint max-params: 0 */
import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { deleteBookFile, deleteBookFiles, setBookFilesSort, updateBookFiles } from 'Store/Actions/bookFileActions';
import { fetchQualityProfileSchema } from 'Store/Actions/settingsActions';
import createAuthorSelector from 'Store/Selectors/createAuthorSelector';
import createClientSideCollectionSelector from 'Store/Selectors/createClientSideCollectionSelector';
import getQualities from 'Utilities/Quality/getQualities';
import IssueFileEditorTableContent from './IssueFileEditorTableContent';

function createSchemaSelector() {
  return createSelector(
    (state) => state.settings.qualityProfiles,
    (qualityProfiles) => {
      const qualities = getQualities(qualityProfiles.schema.items);

      let error = null;

      if (qualityProfiles.schemaError) {
        error = 'Unable to load qualities';
      }

      return {
        isFetching: qualityProfiles.isSchemaFetching,
        isPopulated: qualityProfiles.isSchemaPopulated,
        error,
        qualities
      };
    }
  );
}

function createMapStateToProps() {
  return createSelector(
    (state, { bookId }) => bookId,
    createClientSideCollectionSelector('bookFiles'),
    createSchemaSelector(),
    createVolumeSelector(),
    (
      bookId,
      bookFiles,
      schema,
      author
    ) => {
      const {
        items,
        ...otherProps
      } = bookFiles;
      return {
        ...schema,
        items,
        ...otherProps,
        isDeleting: bookFiles.isDeleting,
        isSaving: bookFiles.isSaving
      };
    }
  );
}

function createMapDispatchToProps(dispatch, props) {
  return {
    onSortPress(sortKey) {
      dispatch(setIssueFilesSort({ sortKey }));
    },

    dispatchFetchQualityProfileSchema(name, path) {
      dispatch(fetchQualityProfileSchema());
    },

    dispatchUpdateIssueFiles(updateProps) {
      dispatch(updateIssueFiles(updateProps));
    },

    onDeletePress(bookFileIds) {
      dispatch(deleteIssueFiles({ bookFileIds }));
    },

    dispatchDeleteIssueFile(id) {
      dispatch(deleteIssueFile(id));
    }
  };
}

class IssueFileEditorTableContentConnector extends Component {

  //
  // Lifecycle

  componentDidMount() {
    this.props.dispatchFetchQualityProfileSchema();
  }

  //
  // Listeners

  onQualityChange = (bookFileIds, qualityId) => {
    const quality = {
      quality: _.find(this.props.qualities, { id: qualityId }),
      revision: {
        version: 1,
        real: 0
      }
    };

    this.props.dispatchUpdateIssueFiles({ bookFileIds, quality });
  };

  //
  // Render

  render() {
    const {
      dispatchFetchQualityProfileSchema,
      dispatchUpdateIssueFiles,
      ...otherProps
    } = this.props;

    return (
      <IssueFileEditorTableContent
        {...otherProps}
        onQualityChange={this.onQualityChange}
      />
    );
  }
}

IssueFileEditorTableContentConnector.propTypes = {
  authorId: PropTypes.number.isRequired,
  bookId: PropTypes.number,
  qualities: PropTypes.arrayOf(PropTypes.object).isRequired,
  dispatchFetchQualityProfileSchema: PropTypes.func.isRequired,
  dispatchUpdateIssueFiles: PropTypes.func.isRequired,
  dispatchDeleteIssueFile: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, createMapDispatchToProps)(IssueFileEditorTableContentConnector);
