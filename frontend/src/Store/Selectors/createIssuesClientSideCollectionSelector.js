import _ from 'lodash';
import { createSelector } from 'reselect';
import filterCollection from 'Utilities/Array/filterCollection';
import sortCollection from 'Utilities/Array/sortCollection';
import createCustomFiltersSelector from './createCustomFiltersSelector';

function createIssuesClientSideCollectionSelector(uiSection) {
  return createSelector(
    (state) => _.get(state, 'issues'),
    (state) => _.get(state, 'volumes'),
    (state) => _.get(state, uiSection),
    createCustomFiltersSelector('issues', uiSection),
    (issueState, volumeState, uiSectionState = {}, customFilters) => {
      const state = Object.assign({}, issueState, uiSectionState, { customFilters });

      const issues = state.items;
      for (const issue of issues) {
        issue.volume = volumeState.items[volumeState.itemMap[issue.volumeId]];
      }

      const filtered = filterCollection(issues, state);
      const sorted = sortCollection(filtered, state);

      return {
        ...issueState,
        ...uiSectionState,
        customFilters,
        items: sorted,
        totalItems: state.items.length
      };
    }
  );
}

export default createIssuesClientSideCollectionSelector;
