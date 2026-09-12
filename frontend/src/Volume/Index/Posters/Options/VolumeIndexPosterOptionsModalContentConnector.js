import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { setAuthorPosterOption as setVolumePosterOption } from 'Store/Actions/authorIndexActions';
import VolumeIndexPosterOptionsModalContent from './VolumeIndexPosterOptionsModalContent';

function createMapStateToProps() {
  return createSelector(
    (state) => state.authorIndex,
    (authorIndex) => {
      return authorIndex.posterOptions;
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
