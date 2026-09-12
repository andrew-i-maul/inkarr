import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { setAuthorOverviewOption as setVolumeOverviewOption } from 'Store/Actions/authorIndexActions';
import VolumeIndexOverviewOptionsModalContent from './VolumeIndexOverviewOptionsModalContent';

function createMapStateToProps() {
  return createSelector(
    (state) => state.authorIndex,
    (authorIndex) => {
      return authorIndex.overviewOptions;
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
