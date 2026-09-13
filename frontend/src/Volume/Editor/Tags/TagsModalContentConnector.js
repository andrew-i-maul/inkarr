import _ from 'lodash';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import createAllVolumeSelector from 'Store/Selectors/createAllVolumesSelector';
import createTagsSelector from 'Store/Selectors/createTagsSelector';
import TagsModalContent from './TagsModalContent';

function createMapStateToProps() {
  return createSelector(
    (state, { volumeIds }) => volumeIds,
    createAllVolumeSelector(),
    createTagsSelector(),
    (volumeIds, allVolumes, tagList) => {
      const volume = _.intersectionWith(allVolumes, volumeIds, (s, id) => {
        return s.id === id;
      });

      const volumeTags = _.uniq(_.concat(..._.map(volume, 'tags')));

      return {
        volumeTags,
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
