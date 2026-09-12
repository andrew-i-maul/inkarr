import { connect } from 'react-redux';
import { setAuthorTableOption as setVolumeTableOption } from 'Store/Actions/authorIndexActions';
import VolumeIndexHeader from './VolumeIndexHeader';

function createMapDispatchToProps(dispatch, props) {
  return {
    onTableOptionChange(payload) {
      dispatch(setVolumeTableOption(payload));
    }
  };
}

export default connect(undefined, createMapDispatchToProps)(VolumeIndexHeader);
