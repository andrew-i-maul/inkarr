import { push } from 'connected-react-router';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import createAllVolumeSelector from 'Store/Selectors/createAllVolumesSelector';
import createDeepEqualSelector from 'Store/Selectors/createDeepEqualSelector';
import createTagsSelector from 'Store/Selectors/createTagsSelector';
import VolumeSearchInput from './VolumeSearchInput';

function createCleanVolumeSelector() {
  return createSelector(
    createAllVolumeSelector(),
    createTagsSelector(),
    (allVolumes, allTags) => {
      return allVolumes.map((volume) => {
        const {
          volumeName,
          sortName,
          images,
          titleSlug,
          tags = []
        } = volume;

        return {
          type: 'volume',
          name: volumeName,
          sortName,
          titleSlug,
          images,
          firstCharacter: volumeName.charAt(0).toLowerCase(),
          tags: tags.reduce((acc, id) => {
            const matchingTag = allTags.find((tag) => tag.id === id);

            if (matchingTag) {
              acc.push(matchingTag);
            }

            return acc;
          }, [])
        };
      });
    }
  );
}

function createCleanIssueSelector() {
  return createSelector(
    (state) => state.issues.items,
    (allIssues) => {
      return allIssues.map((issue) => {
        const {
          title,
          images,
          titleSlug
        } = issue;

        return {
          type: 'issue',
          name: title,
          sortName: title,
          titleSlug,
          images,
          firstCharacter: title.charAt(0).toLowerCase(),
          tags: []
        };
      });
    }
  );
}

function createMapStateToProps() {
  return createDeepEqualSelector(
    createCleanVolumeSelector(),
    createCleanIssueSelector(),
    (volumes, issues) => {
      const items = [
        ...volumes,
        ...issues
      ];
      return {
        items
      };
    }
  );
}

function createMapDispatchToProps(dispatch, props) {
  return {
    onGoToVolume(titleSlug) {
      dispatch(push(`${window.Inkarr.urlBase}/volume/${titleSlug}`));
    },

    onGoToIssue(titleSlug) {
      dispatch(push(`${window.Inkarr.urlBase}/issue/${titleSlug}`));
    },

    onGoToAddNewVolume(query) {
      dispatch(push(`${window.Inkarr.urlBase}/add/search?term=${encodeURIComponent(query)}`));
    }
  };
}

export default connect(createMapStateToProps, createMapDispatchToProps)(VolumeSearchInput);
