import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { toggleBooksMonitored } from 'Store/Actions/bookActions';
import createBookSelector from 'Store/Selectors/createBookSelector';
import createDimensionsSelector from 'Store/Selectors/createDimensionsSelector';
import createUISettingsSelector from 'Store/Selectors/createUISettingsSelector';
import IssueDetailsHeader from './IssueDetailsHeader';

const selectOverview = createSelector(
  (state) => state.editions,
  (editions) => {
    const monitored = editions.items.find((e) => e.monitored === true);
    return monitored?.overview;
  }
);

function createMapStateToProps() {
  return createSelector(
    createIssueSelector(),
    selectOverview,
    createUISettingsSelector(),
    createDimensionsSelector(),
    (book, overview, uiSettings, dimensions) => {

      return {
        ...book,
        overview,
        shortDateFormat: uiSettings.shortDateFormat,
        isSmallScreen: dimensions.isSmallScreen
      };
    }
  );
}

const mapDispatchToProps = {
  toggleIssuesMonitored
};

class IssueDetailsHeaderConnector extends Component {

  //
  // Listeners

  onMonitorTogglePress = (monitored) => {
    this.props.toggleIssuesMonitored({
      bookIds: [this.props.bookId],
      monitored
    });
  };

  //
  // Render

  render() {
    return (
      <IssueDetailsHeader
        {...this.props}
        onMonitorTogglePress={this.onMonitorTogglePress}
      />
    );
  }
}

IssueDetailsHeaderConnector.propTypes = {
  bookId: PropTypes.number,
  toggleIssuesMonitored: PropTypes.func.isRequired,
  author: PropTypes.object
};

export default connect(createMapStateToProps, mapDispatchToProps)(IssueDetailsHeaderConnector);
