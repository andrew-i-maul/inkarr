import _ from 'lodash';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import createAllAuthorSelector from 'Store/Selectors/createAllAuthorsSelector';
import createTagsSelector from 'Store/Selectors/createTagsSelector';
import TagsModalContent from './TagsModalContent';

function createMapStateToProps() {
  return createSelector(
    (state, { authorIds }) => authorIds,
    createAllVolumeSelector(),
    createTagsSelector(),
    (authorIds, allVolumes, tagList) => {
      const author = _.intersectionWith(allVolumes, authorIds, (s, id) => {
        return s.id === id;
      });

      const authorTags = _.uniq(_.concat(..._.map(author, 'tags')));

      return {
        authorTags,
        tagList
      };
    }
  );
}

function createMapDispatchToProps(dispatch, props) {
  return {
    onAction() {
      // Do something
    }
  };
}

export default connect(createMapStateToProps, createMapDispatchToProps)(TagsModalContent);
