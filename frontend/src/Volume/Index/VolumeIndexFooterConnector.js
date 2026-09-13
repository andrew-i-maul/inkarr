import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import createClientSideCollectionSelector from 'Store/Selectors/createClientSideCollectionSelector';
import createDeepEqualSelector from 'Store/Selectors/createDeepEqualSelector';
import VolumeIndexFooter from './VolumeIndexFooter';

function createUnoptimizedSelector() {
  return createSelector(
    createClientSideCollectionSelector('volumes', 'volumeIndex'),
    (volumes) => {
      return volumes.items.map((s) => {
        const {
          monitored,
          status,
          statistics
        } = s;

        return {
          monitored,
          status,
          statistics
        };
      });
    }
  );
}

function createVolumeSelector() {
  return createDeepEqualSelector(
    createUnoptimizedSelector(),
    (volume) => volume
  );
}

function createMapStateToProps() {
  return createSelector(
    createVolumeSelector(),
    (volume) => {
      return {
        volume
      };
    }
  );
}

export default connect(createMapStateToProps)(VolumeIndexFooter);
