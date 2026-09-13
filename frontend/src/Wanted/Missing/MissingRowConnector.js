import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import createVolumeSelector from 'Store/Selectors/createVolumeSelector';
import MissingRow from './MissingRow';

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

export default connect(createMapStateToProps)(MissingRow);
