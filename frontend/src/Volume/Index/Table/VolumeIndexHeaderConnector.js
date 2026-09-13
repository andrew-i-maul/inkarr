import { connect } from 'react-redux';
import { setVolumeTableOption as setVolumeTableOption } from 'Store/Actions/volumeIndexActions';
import VolumeIndexHeader from './VolumeIndexHeader';

function createMapDispatchToProps(dispatch, props) {
  return {
    onTableOptionChange(payload) {
      dispatch(setVolumeTableOption(payload));
    }
  };
}

export default connect(undefined, createMapDispatchToProps)(VolumeIndexHeader);
