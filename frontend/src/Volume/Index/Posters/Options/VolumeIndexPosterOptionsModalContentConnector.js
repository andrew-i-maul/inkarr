import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { setVolumePosterOption as setVolumePosterOption } from 'Store/Actions/volumeIndexActions';
import VolumeIndexPosterOptionsModalContent from './VolumeIndexPosterOptionsModalContent';

function createMapStateToProps() {
  return createSelector(
    (state) => state.volumeIndex,
    (volumeIndex) => {
      return volumeIndex.posterOptions;
    }
  );
}

function createMapDispatchToProps(dispatch, props) {
  return {
    onChangePosterOption(payload) {
      dispatch(setVolumePosterOption(payload));
    }
  };
}

export default connect(createMapStateToProps, createMapDispatchToProps)(VolumeIndexPosterOptionsModalContent);
