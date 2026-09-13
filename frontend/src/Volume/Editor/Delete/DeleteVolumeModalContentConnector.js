import _ from 'lodash';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { bulkDeleteVolume } from 'Store/Actions/volumeIndexActions';
import createAllVolumeSelector from 'Store/Selectors/createAllVolumesSelector';
import DeleteVolumeModalContent from './DeleteVolumeModalContent';

function createMapStateToProps() {
  return createSelector(
    (state, { volumeIds }) => volumeIds,
    createAllVolumeSelector(),
    (volumeIds, allVolumes) => {
      const selectedVolume = _.intersectionWith(allVolumes, volumeIds, (s, id) => {
        return s.id === id;
      });

      const sortedVolume = _.orderBy(selectedVolume, 'sortName');
      const volume = _.map(sortedVolume, (s) => {
        return {
          volumeName: s.volumeName,
          path: s.path
        };
      });

      return {
        volume
      };
    }
  );
}

function createMapDispatchToProps(dispatch, props) {
  return {
    onDeleteSelectedPress(deleteFiles) {
      dispatch(bulkDeleteVolume({
        volumeIds: props.volumeIds,
        deleteFiles
      }));

      props.onModalClose();
    }
  };
}

export default connect(createMapStateToProps, createMapDispatchToProps)(DeleteVolumeModalContent);
