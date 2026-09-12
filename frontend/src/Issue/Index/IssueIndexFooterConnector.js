import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import createClientSideCollectionSelector from 'Store/Selectors/createClientSideCollectionSelector';
import createDeepEqualSelector from 'Store/Selectors/createDeepEqualSelector';
import IssueIndexFooter from './IssueIndexFooter';

function createUnoptimizedSelector() {
  return createSelector(
    createClientSideCollectionSelector('books', 'bookIndex'),
    (books) => {
      return books.items.map((s) => {
        const {
          authorId,
          monitored,
          status,
          statistics
        } = s;

        return {
          authorId,
          monitored,
          status,
          statistics
        };
      });
    }
  );
}

function createIssueSelector() {
  return createDeepEqualSelector(
    createUnoptimizedSelector(),
    (book) => book
  );
}

function createMapStateToProps() {
  return createSelector(
    createIssueSelector(),
    (book) => {
      return {
        book
      };
    }
  );
}

export default connect(createMapStateToProps)(IssueIndexFooter);
