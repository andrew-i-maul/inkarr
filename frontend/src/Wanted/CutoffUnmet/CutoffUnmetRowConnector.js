import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import createVolumeSelector from 'Store/Selectors/createVolumeSelector';
import CutoffUnmetRow from './CutoffUnmetRow';

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

export default connect(createMapStateToProps)(CutoffUnmetRow);
