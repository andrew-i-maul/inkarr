/* eslint max-params: 0 */
import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import * as commandNames from 'Commands/commandNames';
import { toggleBooksMonitored } from 'Store/Actions/bookActions';
import { clearBookFiles, fetchBookFiles } from 'Store/Actions/bookFileActions';
import { executeCommand } from 'Store/Actions/commandActions';
import { clearEditions, fetchEditions } from 'Store/Actions/editionActions';
import { cancelFetchReleases, clearReleases } from 'Store/Actions/releaseActions';
import createAllAuthorSelector from 'Store/Selectors/createAllAuthorsSelector';
import createCommandsSelector from 'Store/Selectors/createCommandsSelector';
import createDimensionsSelector from 'Store/Selectors/createDimensionsSelector';
import createUISettingsSelector from 'Store/Selectors/createUISettingsSelector';
import { findCommand, isCommandExecuting } from 'Utilities/Command';
import { registerPagePopulator, unregisterPagePopulator } from 'Utilities/pagePopulator';
import IssueDetails from './IssueDetails';

const selectIssueFiles = createSelector(
  (state) => state.bookFiles,
  (bookFiles) => {
    const {
      items,
      isFetching,
      isPopulated,
      error
    } = bookFiles;

    const hasIssueFiles = !!items.length;

    return {
      isIssueFilesFetching: isFetching,
      isIssueFilesPopulated: isPopulated,
      bookFilesError: error,
      hasIssueFiles
    };
  }
);

function createMapStateToProps() {
  return createSelector(
    (state, { titleSlug }) => titleSlug,
    selectIssueFiles,
    (state) => state.books,
    (state) => state.editions,
    createAllAuthorSelector(),
    createCommandsSelector(),
    createUISettingsSelector(),
    createDimensionsSelector(),
    (titleSlug, bookFiles, books, editions, authors, commands, uiSettings, dimensions) => {
      const book = books.items.find((b) => b.titleSlug === titleSlug);
      const author = authors.find((a) => a.id === book.authorId);
      const sortedIssues = books.items.filter((b) => b.authorId === book.authorId);
      sortedIssues.sort((a, b) => ((a.releaseDate > b.releaseDate) ? 1 : -1));
      const bookIndex = sortedIssues.findIndex((b) => b.id === book.id);

      if (!book) {
        return {};
      }

      const {
        isIssueFilesFetching,
        isIssueFilesPopulated,
        bookFilesError,
        hasIssueFiles
      } = bookFiles;

      const previousIssue = sortedIssues[bookIndex - 1] || _.last(sortedIssues);
      const nextIssue = sortedIssues[bookIndex + 1] || _.first(sortedIssues);
      const isRefreshingCommand = findCommand(commands, { name: commandNames.REFRESH_BOOK });
      const isRefreshing = (
        isCommandExecuting(isRefreshingCommand) &&
        isRefreshingCommand.body.bookId === book.id
      );
      const isSearchingCommand = findCommand(commands, { name: commandNames.BOOK_SEARCH });
      const isSearching = (
        isCommandExecuting(isSearchingCommand) &&
        isSearchingCommand.body.bookIds.indexOf(book.id) > -1
      );
      const isRenamingFiles = isCommandExecuting(findCommand(commands, { name: commandNames.RENAME_FILES, authorId: author.id }));
      const isRenamingVolumeCommand = findCommand(commands, { name: commandNames.RENAME_AUTHOR });
      const isRenamingVolume = (
        isCommandExecuting(isRenamingVolumeCommand) &&
        isRenamingVolumeCommand.body.authorIds.indexOf(author.id) > -1
      );

      const isFetching = isIssueFilesFetching || editions.isFetching;
      const isPopulated = isIssueFilesPopulated && editions.isPopulated;

      return {
        ...book,
        shortDateFormat: uiSettings.shortDateFormat,
        author,
        isRefreshing,
        isSearching,
        isRenamingFiles,
        isRenamingVolume,
        isFetching,
        isPopulated,
        bookFilesError,
        hasIssueFiles,
        previousIssue,
        nextIssue,
        isSmallScreen: dimensions.isSmallScreen
      };
    }
  );
}

const mapDispatchToProps = {
  executeCommand,
  fetchIssueFiles: fetchBookFiles,
  clearIssueFiles: clearBookFiles,
  fetchEditions,
  clearEditions,
  clearReleases,
  cancelFetchReleases,
  toggleIssuesMonitored: toggleBooksMonitored
};

function getMonitoredEditions(props) {
  return _.map(_.filter(props.editions, { monitored: true }), 'id').sort();
}

class IssueDetailsConnector extends Component {

  componentDidMount() {
    registerPagePopulator(this.populate);
    this.populate();
  }

  componentDidUpdate(prevProps) {
    const {
      id,
      anyReleaseOk,
      isRenamingFiles,
      isRenamingVolume
    } = this.props;

    if (
      (prevProps.isRenamingFiles && !isRenamingFiles) ||
      (prevProps.isRenamingVolume && !isRenamingVolume) ||
      !_.isEqual(getMonitoredEditions(prevProps), getMonitoredEditions(this.props)) ||
      (prevProps.anyReleaseOk === false && anyReleaseOk === true)
    ) {
      this.unpopulate();
      this.populate();
    }

    // If the id has changed we need to clear the book
    // files and fetch from the server.

    if (prevProps.id !== id) {
      this.unpopulate();
      this.populate();
    }
  }

  componentWillUnmount() {
    unregisterPagePopulator(this.populate);
    this.unpopulate();
  }

  //
  // Control

  populate = () => {
    const bookId = this.props.id;

    this.props.fetchIssueFiles({ bookId });
    this.props.fetchEditions({ bookId });
  };

  unpopulate = () => {
    this.props.cancelFetchReleases();
    this.props.clearReleases();
    this.props.clearIssueFiles();
    this.props.clearEditions();
  };

  //
  // Listeners

  onMonitorTogglePress = (monitored) => {
    this.props.toggleIssuesMonitored({
      bookIds: [this.props.id],
      monitored
    });
  };

  onRefreshPress = () => {
    this.props.executeCommand({
      name: commandNames.REFRESH_BOOK,
      bookId: this.props.id
    });
  };

  onSearchPress = () => {
    this.props.executeCommand({
      name: commandNames.BOOK_SEARCH,
      bookIds: [this.props.id]
    });
  };

  //
  // Render

  render() {
    return (
      <IssueDetails
        {...this.props}
        onMonitorTogglePress={this.onMonitorTogglePress}
        onRefreshPress={this.onRefreshPress}
        onSearchPress={this.onSearchPress}
      />
    );
  }
}

IssueDetailsConnector.propTypes = {
  id: PropTypes.number,
  anyReleaseOk: PropTypes.bool,
  isRenamingFiles: PropTypes.bool.isRequired,
  isRenamingVolume: PropTypes.bool.isRequired,
  isIssueFetching: PropTypes.bool,
  isIssuePopulated: PropTypes.bool,
  titleSlug: PropTypes.string.isRequired,
  fetchIssueFiles: PropTypes.func.isRequired,
  clearIssueFiles: PropTypes.func.isRequired,
  fetchEditions: PropTypes.func.isRequired,
  clearEditions: PropTypes.func.isRequired,
  clearReleases: PropTypes.func.isRequired,
  cancelFetchReleases: PropTypes.func.isRequired,
  toggleIssuesMonitored: PropTypes.func.isRequired,
  executeCommand: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(IssueDetailsConnector);
