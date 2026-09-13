import _ from 'lodash';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { bulkDeleteAuthor } from 'Store/Actions/authorIndexActions';
import createAllAuthorSelector from 'Store/Selectors/createAllAuthorsSelector';
import DeleteVolumeModalContent from './DeleteVolumeModalContent';

function createMapStateToProps() {
  return createSelector(
    (state, { authorIds }) => authorIds,
    createAllAuthorSelector(),
    (authorIds, allVolumes) => {
      const selectedVolume = _.intersectionWith(allVolumes, authorIds, (s, id) => {
        return s.id === id;
      });

      const sortedVolume = _.orderBy(selectedVolume, 'sortName');
      const author = _.map(sortedVolume, (s) => {
        return {
          authorName: s.authorName,
          path: s.path
        };
      });

      return {
        author
      };
    }
  );
}

function createMapDispatchToProps(dispatch, props) {
  return {
    onDeleteSelectedPress(deleteFiles) {
      dispatch(bulkDeleteVolume({
        authorIds: props.authorIds,
        deleteFiles
      }));

      props.onModalClose();
    }
  };
}

export default connect(createMapStateToProps, createMapDispatchToProps)(DeleteVolumeModalContent);
