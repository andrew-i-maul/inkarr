import { createAction } from 'redux-actions';
import { filterBuilderTypes, filterBuilderValueTypes, sortDirections } from 'Helpers/Props';
import { createThunk, handleThunks } from 'Store/thunks';
import createAjaxRequest from 'Utilities/createAjaxRequest';
import { filterPredicates, filters } from './volumeActions';
import { set } from './baseActions';
import createHandleActions from './Creators/createHandleActions';
import createSetClientSideCollectionFilterReducer from './Creators/Reducers/createSetClientSideCollectionFilterReducer';
import createSetClientSideCollectionSortReducer from './Creators/Reducers/createSetClientSideCollectionSortReducer';

//
// Variables

export const section = 'bookshelf';

//
// State

export const defaultState = {
  isSaving: false,
  saveError: null,
  sortKey: 'sortName',
  sortDirection: sortDirections.ASCENDING,
  secondarySortKey: 'sortName',
  secondarySortDirection: sortDirections.ASCENDING,
  selectedFilterKey: 'all',
  filters,
  filterPredicates,

  filterBuilderProps: [
    {
      name: 'monitored',
      label: 'Monitored',
      type: filterBuilderTypes.EXACT,
      valueType: filterBuilderValueTypes.BOOL
    },
    {
      name: 'status',
      label: 'Status',
      type: filterBuilderTypes.EXACT,
      valueType: filterBuilderValueTypes.VOLUME_STATUS
    },
    {
      name: 'qualityProfileId',
      label: 'Quality Profile',
      type: filterBuilderTypes.EXACT,
      valueType: filterBuilderValueTypes.QUALITY_PROFILE
    },
    {
      name: 'metadataProfileId',
      label: 'Metadata Profile',
      type: filterBuilderTypes.EXACT,
      valueType: filterBuilderValueTypes.METADATA_PROFILE
    },
    {
      name: 'rootFolderPath',
      label: 'Root Folder Path',
      type: filterBuilderTypes.EXACT
    },
    {
      name: 'tags',
      label: 'Tags',
      type: filterBuilderTypes.ARRAY,
      valueType: filterBuilderValueTypes.TAG
    }
  ]
};

export const persistState = [
  'bookshelf.sortKey',
  'bookshelf.sortDirection',
  'bookshelf.selectedFilterKey',
  'bookshelf.customFilters'
];

//
// Actions Types

export const SET_ISSUESHELF_SORT = 'bookshelf/setBookshelfSort';
export const SET_ISSUESHELF_FILTER = 'bookshelf/setBookshelfFilter';
export const SAVE_ISSUESHELF = 'bookshelf/saveBookshelf';

//
// Action Creators

export const setBookshelfSort = createAction(SET_ISSUESHELF_SORT);
export const setBookshelfFilter = createAction(SET_ISSUESHELF_FILTER);
export const saveBookshelf = createThunk(SAVE_ISSUESHELF);

//
// Action Handlers

export const actionHandlers = handleThunks({

  [SAVE_ISSUESHELF]: function(getState, payload, dispatch) {
    const {
      volumeIds,
      monitored,
      monitor,
      monitorNewItems
    } = payload;

    const volumes = [];

    volumeIds.forEach((id) => {
      const volumeToUpdate = { id };

      if (payload.hasOwnProperty('monitored')) {
        volumeToUpdate.monitored = monitored;
      }

      volumes.push(volumeToUpdate);
    });

    dispatch(set({
      section,
      isSaving: true
    }));

    const promise = createAjaxRequest({
      url: '/bookshelf',
      method: 'POST',
      data: JSON.stringify({
        volumes,
        monitoringOptions: { monitor },
        monitorNewItems
      }),
      dataType: 'json'
    }).request;

    promise.done((data) => {
      dispatch(set({
        section,
        isSaving: false,
        saveError: null
      }));
    });

    promise.fail((xhr) => {
      dispatch(set({
        section,
        isSaving: false,
        saveError: xhr
      }));
    });
  }
});

//
// Reducers

export const reducers = createHandleActions({

  [SET_ISSUESHELF_SORT]: createSetClientSideCollectionSortReducer(section),
  [SET_ISSUESHELF_FILTER]: createSetClientSideCollectionFilterReducer(section)

}, defaultState, section);

