import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { removeBlocklistItem } from 'Store/Actions/blocklistActions';
import createVolumeSelector from 'Store/Selectors/createVolumeSelector';
import BlocklistRow from './BlocklistRow';

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

function createMapDispatchToProps(dispatch, props) {
  return {
    onRemovePress() {
      dispatch(removeBlocklistItem({ id: props.id }));
    }
  };
}

export default connect(createMapStateToProps, createMapDispatchToProps)(BlocklistRow);
