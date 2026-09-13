import { createAction } from 'redux-actions';
import { sortDirections } from 'Helpers/Props';
import { createThunk, handleThunks } from 'Store/thunks';
import { set } from './baseActions';
import { filterPredicates, sortPredicates } from './issueActions';
import createHandleActions from './Creators/createHandleActions';
import createSetClientSideCollectionSortReducer from './Creators/Reducers/createSetClientSideCollectionSortReducer';

//
// Variables

export const section = 'volumeDetails';

//
// State

export const defaultState = {
  sortKey: 'releaseDate',
  sortDirection: sortDirections.DESCENDING,
  secondarySortKey: 'releaseDate',
  secondarySortDirection: sortDirections.DESCENDING,

  selectedFilterKey: 'volumeId',

  sortPredicates: {
    ...sortPredicates
  },

  filters: [
    {
      key: 'volumeId',
      label: 'Volume',
      filters: [
        {
          key: 'volumeId',
          value: 0
        }
      ]
    }
  ],

  filterPredicates

};

export const persistState = [
  'volumeDetails.sortKey',
  'volumeDetails.sortDirection'
];

//
// Actions Types

export const SET_VOLUME_DETAILS_SORT = 'volumeIndex/setVolumeDetailsSort';
export const SET_VOLUME_DETAILS_ID = 'volumeIndex/setVolumeDetailsId';

//
// Action Creators

export const setVolumeDetailsSort = createAction(SET_VOLUME_DETAILS_SORT);
export const setVolumeDetailsId = createThunk(SET_VOLUME_DETAILS_ID);

//
// Action Handlers

export const actionHandlers = handleThunks({
  [SET_VOLUME_DETAILS_ID]: function(getState, payload, dispatch) {
    const {
      volumeId
    } = payload;

    dispatch(set({
      section,
      filters: [
        {
          key: 'volumeId',
          label: 'Volume',
          filters: [
            {
              key: 'volumeId',
              value: volumeId
            }
          ]
        }
      ]
    }));
  }
});

//
// Reducers

export const reducers = createHandleActions({

  [SET_VOLUME_DETAILS_SORT]: createSetClientSideCollectionSortReducer(section)

}, defaultState, section);
