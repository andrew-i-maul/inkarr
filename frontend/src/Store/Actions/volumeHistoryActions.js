import { createAction } from 'redux-actions';
import { batchActions } from 'redux-batched-actions';
import { createThunk, handleThunks } from 'Store/thunks';
import createAjaxRequest from 'Utilities/createAjaxRequest';
import { set, update } from './baseActions';
import createHandleActions from './Creators/createHandleActions';

//
// Variables

export const section = 'volumeHistory';

//
// State

export const defaultState = {
  isFetching: false,
  isPopulated: false,
  error: null,
  items: []
};

//
// Actions Types

export const FETCH_VOLUME_HISTORY = 'volumeHistory/fetchVolumeHistory';
export const CLEAR_VOLUME_HISTORY = 'volumeHistory/clearVolumeHistory';
export const VOLUME_HISTORY_MARK_AS_FAILED = 'volumeHistory/volumeHistoryMarkAsFailed';

//
// Action Creators

export const fetchVolumeHistory = createThunk(FETCH_VOLUME_HISTORY);
export const clearVolumeHistory = createAction(CLEAR_VOLUME_HISTORY);
export const volumeHistoryMarkAsFailed = createThunk(VOLUME_HISTORY_MARK_AS_FAILED);

//
// Action Handlers

export const actionHandlers = handleThunks({

  [FETCH_VOLUME_HISTORY]: function(getState, payload, dispatch) {
    dispatch(set({ section, isFetching: true }));

    const promise = createAjaxRequest({
      url: '/history/volume',
      data: payload
    }).request;

    promise.done((data) => {
      dispatch(batchActions([
        update({ section, data }),

        set({
          section,
          isFetching: false,
          isPopulated: true,
          error: null
        })
      ]));
    });

    promise.fail((xhr) => {
      dispatch(set({
        section,
        isFetching: false,
        isPopulated: false,
        error: xhr
      }));
    });
  },

  [VOLUME_HISTORY_MARK_AS_FAILED]: function(getState, payload, dispatch) {
    const {
      historyId,
      volumeId,
      issueId
    } = payload;

    const promise = createAjaxRequest({
      url: `/history/failed/${historyId}`,
      method: 'POST'
    }).request;

    promise.done(() => {
      dispatch(fetchVolumeHistory({ volumeId, issueId }));
    });
  }
});

//
// Reducers

export const reducers = createHandleActions({

  [CLEAR_VOLUME_HISTORY]: (state) => {
    return Object.assign({}, state, defaultState);
  }

}, defaultState, section);

