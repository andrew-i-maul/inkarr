import _ from 'lodash';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { bulkDeleteBook as bulkDeleteIssue } from 'Store/Actions/bookIndexActions';
import DeleteIssueModalContent from './DeleteIssueModalContent';

function createMapStateToProps() {
  return createSelector(
    (state, { bookIds }) => bookIds,
    (state) => state.books.items,
    (state) => state.bookFiles.items,
    (bookIds, allIssues, allIssueFiles) => {
      const selectedIssue = _.intersectionWith(allIssues, bookIds, (s, id) => {
        return s.id === id;
      });

      const sortedIssue = _.orderBy(selectedIssue, 'title');

      const selectedFiles = _.intersectionWith(allIssueFiles, bookIds, (s, id) => {
        return s.bookId === id;
      });

      const files = _.orderBy(selectedFiles, ['bookId', 'path']);

      const book = _.map(sortedIssue, (s) => {
        return {
          title: s.title,
          path: s.path
        };
      });

      return {
        book,
        files
      };
    }
  );
}

function createMapDispatchToProps(dispatch, props) {
  return {
    onDeleteSelectedPress(deleteFiles, addImportListExclusion) {
      dispatch(bulkDeleteIssue({
        bookIds: props.bookIds,
        deleteFiles,
        addImportListExclusion
      }));

      props.onModalClose();
    }
  };
}

export default connect(createMapStateToProps, createMapDispatchToProps)(DeleteIssueModalContent);
