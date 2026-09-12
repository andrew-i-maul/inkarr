import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { setBookPosterOption as setIssuePosterOption } from 'Store/Actions/bookIndexActions';
import IssueIndexPosterOptionsModalContent from './IssueIndexPosterOptionsModalContent';

function createMapStateToProps() {
  return createSelector(
    (state) => state.bookIndex,
    (bookIndex) => {
      return bookIndex.posterOptions;
    }
  );
}

function createMapDispatchToProps(dispatch, props) {
  return {
    onChangePosterOption(payload) {
      dispatch(setIssuePosterOption(payload));
    }
  };
}

export default connect(createMapStateToProps, createMapDispatchToProps)(IssueIndexPosterOptionsModalContent);
