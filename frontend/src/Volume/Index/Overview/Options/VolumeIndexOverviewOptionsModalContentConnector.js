import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { setVolumeOverviewOption as setVolumeOverviewOption } from 'Store/Actions/volumeIndexActions';
import VolumeIndexOverviewOptionsModalContent from './VolumeIndexOverviewOptionsModalContent';

function createMapStateToProps() {
  return createSelector(
    (state) => state.volumeIndex,
    (volumeIndex) => {
      return volumeIndex.overviewOptions;
    }
  );
}

function createMapDispatchToProps(dispatch, props) {
  return {
    onChangeOverviewOption(payload) {
      dispatch(setVolumeOverviewOption(payload));
    }
  };
}

export default connect(createMapStateToProps, createMapDispatchToProps)(VolumeIndexOverviewOptionsModalContent);
