import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import createDimensionsSelector from 'Store/Selectors/createDimensionsSelector';
import createExistingVolumeSelector from 'Store/Selectors/createExistingVolumeSelector';
import AddNewVolumeSearchResult from './AddNewVolumeSearchResult';

function createMapStateToProps() {
  return createSelector(
    createExistingVolumeSelector(),
    createDimensionsSelector(),
    (isExistingVolume, dimensions) => {
      return {
        isExistingVolume,
        isSmallScreen: dimensions.isSmallScreen
      };
    }
  );
}

export default connect(createMapStateToProps)(AddNewVolumeSearchResult);
