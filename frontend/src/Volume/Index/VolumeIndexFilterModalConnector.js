import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import FilterModal from 'Components/Filter/FilterModal';
import { setVolumeFilter as setVolumeFilter } from 'Store/Actions/volumeIndexActions';

function createMapStateToProps() {
  return createSelector(
    (state) => state.volumes.items,
    (state) => state.volumeIndex.filterBuilderProps,
    (sectionItems, filterBuilderProps) => {
      return {
        sectionItems,
        filterBuilderProps,
        customFilterType: 'volumeIndex'
      };
    }
  );
}

const mapDispatchToProps = {
  dispatchSetFilter: setVolumeFilter
};

export default connect(createMapStateToProps, mapDispatchToProps)(FilterModal);
