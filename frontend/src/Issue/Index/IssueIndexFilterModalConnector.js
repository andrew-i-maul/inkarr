import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import FilterModal from 'Components/Filter/FilterModal';
import { setBookFilter as setIssueFilter } from 'Store/Actions/bookIndexActions';

function createMapStateToProps() {
  return createSelector(
    (state) => state.books.items,
    (state) => state.bookIndex.filterBuilderProps,
    (sectionItems, filterBuilderProps) => {
      return {
        sectionItems,
        filterBuilderProps,
        customFilterType: 'bookIndex'
      };
    }
  );
}

const mapDispatchToProps = {
  dispatchSetFilter: setIssueFilter
};

export default connect(createMapStateToProps, mapDispatchToProps)(FilterModal);
