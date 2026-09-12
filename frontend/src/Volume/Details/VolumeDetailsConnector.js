/* eslint max-params: 0 */
import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import * as commandNames from 'Commands/commandNames';
import { toggleAuthorMonitored } from 'Store/Actions/authorActions';
import { clearBookFiles, fetchBookFiles } from 'Store/Actions/bookFileActions';
import { saveBookEditor } from 'Store/Actions/bookIndexActions';
import { executeCommand } from 'Store/Actions/commandActions';
import { clearQueueDetails, fetchQueueDetails } from 'Store/Actions/queueActions';
import { cancelFetchReleases, clearReleases } from 'Store/Actions/releaseActions';
import { clearSeries, fetchSeries } from 'Store/Actions/seriesActions';
import createAllAuthorSelector from 'Store/Selectors/createAllAuthorsSelector';
import createCommandsSelector from 'Store/Selectors/createCommandsSelector';
import createDimensionsSelector from 'Store/Selectors/createDimensionsSelector';
import createSortedSectionSelector from 'Store/Selectors/createSortedSectionSelector';
import { findCommand, isCommandExecuting } from 'Utilities/Command';
import { registerPagePopulator, unregisterPagePopulator } from 'Utilities/pagePopulator';
import VolumeDetails from './VolumeDetails';

const selectIssues = createSelector(
  (state) => state.books,
  (state) => state.bookIndex,
  (books, index) => {
    const {
      items,
      isFetching,
      isPopulated,
      error
    } = books;

    const {
      isSaving,
      saveError,
      isDeleting,
      deleteError
    } = index;

    const hasIssues = !!items.length;
    const hasMonitoredIssues = items.some((e) => e.monitored);

    return {
      isIssuesFetching: isFetching,
      isIssuesPopulated: isPopulated,
      booksError: error,
      hasIssues,
      hasMonitoredIssues,
      isSaving,
      saveError,
      isDeleting,
      deleteError
    };
  }
);

const selectSeries = createSelector(
  createSortedSectionSelector('series', (a, b) => a.title.localeCompare(b.title)),
  (state) => state.series,
  (series) => {
    const {
      items,
      isFetching,
      isPopulated,
      error
    } = series;

    const hasSeries = !!items.length;

    return {
      isSeriesFetching: isFetching,
      isSeriesPopulated: isPopulated,
      seriesError: error,
      hasSeries,
      series: series.items
    };
  }
);

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
    selectIssues,
    selectSeries,
    selectIssueFiles,
    createAllVolumeSelector(),
    createCommandsSelector(),
    createDimensionsSelector(),
    (titleSlug, books, series, bookFiles, allVolumes, commands, dimensions) => {
      const sortedVolume = _.orderBy(allVolumes, 'sortNameLastFirst');
      const authorIndex = _.findIndex(sortedVolume, { titleSlug });
      const author = sortedVolume[authorIndex];

      if (!author) {
        return {};
      }

      const {
        isIssuesFetching,
        isIssuesPopulated,
        booksError,
        hasIssues,
        hasMonitoredIssues,
        isSaving,
        saveError,
        isDeleting,
        deleteError
      } = books;

      const {
        isSeriesFetching,
        isSeriesPopulated,
        seriesError,
        hasSeries,
        series: seriesItems
      } = series;

      const {
        isIssueFilesFetching,
        isIssueFilesPopulated,
        bookFilesError,
        hasIssueFiles
      } = bookFiles;

      const previousVolume = sortedVolume[authorIndex - 1] || _.last(sortedVolume);
      const nextVolume = sortedVolume[authorIndex + 1] || _.first(sortedVolume);
      const isVolumeRefreshing = isCommandExecuting(findCommand(commands, { name: commandNames.REFRESH_AUTHOR, authorId: author.id }));
      const authorRefreshingCommand = findCommand(commands, { name: commandNames.REFRESH_AUTHOR });
      const allVolumeRefreshing = (
        isCommandExecuting(authorRefreshingCommand) &&
        !authorRefreshingCommand.body.authorId
      );
      const isRefreshing = isVolumeRefreshing || allVolumeRefreshing;
      const isSearching = isCommandExecuting(findCommand(commands, { name: commandNames.AUTHOR_SEARCH, authorId: author.id }));
      const isRenamingFiles = isCommandExecuting(findCommand(commands, { name: commandNames.RENAME_FILES, authorId: author.id }));
      const isRenamingVolumeCommand = findCommand(commands, { name: commandNames.RENAME_AUTHOR });
      const isRenamingVolume = (
        isCommandExecuting(isRenamingVolumeCommand) &&
        isRenamingVolumeCommand.body.authorIds.indexOf(author.id) > -1
      );

      const isFetching = isIssuesFetching || isSeriesFetching || isIssueFilesFetching;
      const isPopulated = isIssuesPopulated && isSeriesPopulated && isIssueFilesPopulated;

      const alternateTitles = _.reduce(author.alternateTitles, (acc, alternateTitle) => {
        if ((alternateTitle.seasonNumber === -1 || alternateTitle.seasonNumber === undefined) &&
            (alternateTitle.sceneSeasonNumber === -1 || alternateTitle.sceneSeasonNumber === undefined)) {
          acc.push(alternateTitle.title);
        }

        return acc;
      }, []);

      return {
        ...author,
        alternateTitles,
        isVolumeRefreshing,
        allVolumeRefreshing,
        isRefreshing,
        isSearching,
        isRenamingFiles,
        isRenamingVolume,
        isFetching,
        isPopulated,
        booksError,
        isSaving,
        saveError,
        isDeleting,
        deleteError,
        seriesError,
        bookFilesError,
        hasIssues,
        hasMonitoredIssues,
        hasSeries,
        series: seriesItems,
        hasIssueFiles,
        previousVolume,
        nextVolume,
        isSmallScreen: dimensions.isSmallScreen
      };
    }
  );
}

const mapDispatchToProps = {
  fetchSeries,
  clearSeries,
  saveIssueEditor,
  fetchIssueFiles,
  clearIssueFiles,
  toggleVolumeMonitored,
  fetchQueueDetails,
  clearQueueDetails,
  clearReleases,
  cancelFetchReleases,
  executeCommand
};

class VolumeDetailsConnector extends Component {

  //
  // Lifecycle

  componentDidMount() {
    registerPagePopulator(this.populate);
    this.populate();
  }

  componentDidUpdate(prevProps) {
    const {
      id,
      isVolumeRefreshing,
      allVolumeRefreshing,
      isRenamingFiles,
      isRenamingVolume
    } = this.props;

    if (
      (prevProps.isVolumeRefreshing && !isVolumeRefreshing) ||
      (prevProps.allVolumeRefreshing && !allVolumeRefreshing) ||
      (prevProps.isRenamingFiles && !isRenamingFiles) ||
      (prevProps.isRenamingVolume && !isRenamingVolume)
    ) {
      this.populate();
    }

    // If the id has changed we need to clear the books
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
    const authorId = this.props.id;

    this.props.fetchSeries({ authorId });
    this.props.fetchIssueFiles({ authorId });
    this.props.fetchQueueDetails({ authorId });
  };

  unpopulate = () => {
    this.props.cancelFetchReleases();
    this.props.clearSeries();
    this.props.clearIssueFiles();
    this.props.clearQueueDetails();
    this.props.clearReleases();
  };

  //
  // Listeners

  onMonitorTogglePress = (monitored) => {
    this.props.toggleVolumeMonitored({
      authorId: this.props.id,
      monitored
    });
  };

  onRefreshPress = () => {
    this.props.executeCommand({
      name: commandNames.REFRESH_AUTHOR,
      authorId: this.props.id
    });
  };

  onSearchPress = () => {
    this.props.executeCommand({
      name: commandNames.AUTHOR_SEARCH,
      authorId: this.props.id
    });
  };

  onSaveSelected = (payload) => {
    this.props.saveIssueEditor(payload);
  };

  //
  // Render

  render() {
    return (
      <VolumeDetails
        {...this.props}
        onMonitorTogglePress={this.onMonitorTogglePress}
        onRefreshPress={this.onRefreshPress}
        onSearchPress={this.onSearchPress}
        onSaveSelected={this.onSaveSelected}
      />
    );
  }
}

VolumeDetailsConnector.propTypes = {
  id: PropTypes.number.isRequired,
  titleSlug: PropTypes.string.isRequired,
  isVolumeRefreshing: PropTypes.bool.isRequired,
  allVolumeRefreshing: PropTypes.bool.isRequired,
  isRefreshing: PropTypes.bool.isRequired,
  isRenamingFiles: PropTypes.bool.isRequired,
  isRenamingVolume: PropTypes.bool.isRequired,
  fetchSeries: PropTypes.func.isRequired,
  clearSeries: PropTypes.func.isRequired,
  saveIssueEditor: PropTypes.func.isRequired,
  fetchIssueFiles: PropTypes.func.isRequired,
  clearIssueFiles: PropTypes.func.isRequired,
  toggleVolumeMonitored: PropTypes.func.isRequired,
  fetchQueueDetails: PropTypes.func.isRequired,
  clearQueueDetails: PropTypes.func.isRequired,
  clearReleases: PropTypes.func.isRequired,
  cancelFetchReleases: PropTypes.func.isRequired,
  executeCommand: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(VolumeDetailsConnector);
