import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import createVolumeSelector from 'Store/Selectors/createVolumeSelector';
import createTagsSelector from 'Store/Selectors/createTagsSelector';
import VolumeTags from './VolumeTags';

function createMapStateToProps() {
  return createSelector(
    createVolumeSelector(),
    createTagsSelector(),
    (volume, tagList) => {
      const tags = volume.tags
        .map((tagId) => tagList.find((tag) => tag.id === tagId))
        .filter((tag) => !!tag)
        .map((tag) => tag.label)
        .sort((a, b) => a.localeCompare(b));

      return {
        tags
      };
    }
  );
}

export default connect(createMapStateToProps)(VolumeTags);
