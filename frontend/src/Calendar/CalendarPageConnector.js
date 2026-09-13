import moment from 'moment';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import withCurrentPage from 'Components/withCurrentPage';
import { searchMissing, setCalendarDaysCount, setCalendarFilter } from 'Store/Actions/calendarActions';
import createVolumeCountSelector from 'Store/Selectors/createVolumeCountSelector';
import createCommandsSelector from 'Store/Selectors/createCommandsSelector';
import createUISettingsSelector from 'Store/Selectors/createUISettingsSelector';
import { isCommandExecuting } from 'Utilities/Command';
import isBefore from 'Utilities/Date/isBefore';
import CalendarPage from './CalendarPage';

function createMissingIssueIdsSelector() {
  return createSelector(
    (state) => state.calendar.start,
    (state) => state.calendar.end,
    (state) => state.calendar.items,
    (state) => state.queue.details.items,
    (start, end, issues, queueDetails) => {
      return issues.reduce((acc, issue) => {
        const releaseDate = issue.releaseDate;

        if (
          issue.percentOfIssues < 100 &&
          moment(releaseDate).isAfter(start) &&
          moment(releaseDate).isBefore(end) &&
          isBefore(issue.releaseDate) &&
          !queueDetails.some((details) => !!details.issue && details.issue.id === issue.id)
        ) {
          acc.push(issue.id);
        }

        return acc;
      }, []);
    }
  );
}

function createIsSearchingSelector() {
  return createSelector(
    (state) => state.calendar.searchMissingCommandId,
    createCommandsSelector(),
    (searchMissingCommandId, commands) => {
      if (searchMissingCommandId == null) {
        return false;
      }

      return isCommandExecuting(commands.find((command) => {
        return command.id === searchMissingCommandId;
      }));
    }
  );
}

function createMapStateToProps() {
  return createSelector(
    (state) => state.calendar.selectedFilterKey,
    (state) => state.calendar.filters,
    createVolumeCountSelector(),
    createUISettingsSelector(),
    createMissingIssueIdsSelector(),
    createIsSearchingSelector(),
    (
      selectedFilterKey,
      filters,
      volumeCount,
      uiSettings,
      missingIssueIds,
      isSearchingForMissing
    ) => {
      return {
        selectedFilterKey,
        filters,
        colorImpairedMode: uiSettings.enableColorImpairedMode,
        hasVolume: !!volumeCount.count,
        volumeError: volumeCount.error,
        volumeIsFetching: volumeCount.isFetching,
        volumeIsPopulated: volumeCount.isPopulated,
        missingIssueIds,
        isSearchingForMissing
      };
    }
  );
}

function createMapDispatchToProps(dispatch, props) {
  return {
    onSearchMissingPress(issueIds) {
      dispatch(searchMissing({ issueIds }));
    },
    onDaysCountChange(dayCount) {
      dispatch(setCalendarDaysCount({ dayCount }));
    },

    onFilterSelect(selectedFilterKey) {
      dispatch(setCalendarFilter({ selectedFilterKey }));
    }
  };
}

export default withCurrentPage(
  connect(createMapStateToProps, createMapDispatchToProps)(CalendarPage)
);
