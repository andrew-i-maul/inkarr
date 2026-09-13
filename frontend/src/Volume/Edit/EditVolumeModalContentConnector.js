import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { saveAuthor, setAuthorValue } from 'Store/Actions/authorActions';
import createAuthorSelector from 'Store/Selectors/createAuthorSelector';
import selectSettings from 'Store/Selectors/selectSettings';
import EditVolumeModalContent from './EditVolumeModalContent';

function createIsPathChangingSelector() {
  return createSelector(
    (state) => state.authors.pendingChanges,
    createAuthorSelector(),
    (pendingChanges, author) => {
      const path = pendingChanges.path;

      if (path == null) {
        return false;
      }

      return author.path !== path;
    }
  );
}

function createMapStateToProps() {
  return createSelector(
    (state) => state.authors,
    (state) => state.settings.metadataProfiles,
    createAuthorSelector(),
    createIsPathChangingSelector(),
    (authorsState, metadataProfiles, author, isPathChanging) => {
      const {
        isSaving,
        saveError,
        pendingChanges
      } = authorsState;

      const authorSettings = _.pick(author, [
        'monitored',
        'monitorNewItems',
        'qualityProfileId',
        'metadataProfileId',
        'path',
        'tags'
      ]);

      const settings = selectSettings(authorSettings, pendingChanges, saveError);

      return {
        authorName: author.authorName,
        isSaving,
        saveError,
        isPathChanging,
        originalPath: author.path,
        item: settings.settings,
        showMetadataProfile: metadataProfiles.items.length > 1,
        ...settings
      };
    }
  );
}

const mapDispatchToProps = {
  dispatchSetVolumeValue: setAuthorValue,
  dispatchSaveVolume: saveAuthor
};

class EditVolumeModalContentConnector extends Component {

  //
  // Lifecycle

  componentDidUpdate(prevProps, prevState) {
    if (prevProps.isSaving && !this.props.isSaving && !this.props.saveError) {
      this.props.onModalClose();
    }
  }

  //
  // Listeners

  onInputChange = ({ name, value }) => {
    this.props.dispatchSetVolumeValue({ name, value });
  };

  onSavePress = (moveFiles) => {
    this.props.dispatchSaveVolume({
      id: this.props.authorId,
      moveFiles
    });
  };

  //
  // Render

  render() {
    return (
      <EditVolumeModalContent
        {...this.props}
        onInputChange={this.onInputChange}
        onSavePress={this.onSavePress}
        onMoveVolumePress={this.onMoveVolumePress}
      />
    );
  }
}

EditVolumeModalContentConnector.propTypes = {
  authorId: PropTypes.number,
  isSaving: PropTypes.bool.isRequired,
  saveError: PropTypes.object,
  dispatchSetVolumeValue: PropTypes.func.isRequired,
  dispatchSaveVolume: PropTypes.func.isRequired,
  onModalClose: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(EditVolumeModalContentConnector);
