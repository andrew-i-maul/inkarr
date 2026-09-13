import _ from 'lodash';
import { createAction } from 'redux-actions';
import { batchActions } from 'redux-batched-actions';
import { createThunk, handleThunks } from 'Store/thunks';
import getNewVolume from 'Utilities/Volume/getNewVolume';
import monitorNewItemsOptions from 'Utilities/Volume/monitorNewItemsOptions';
import monitorOptions from 'Utilities/Volume/monitorOptions';
import getNewIssue from 'Utilities/Issue/getNewIssue';
import createAjaxRequest from 'Utilities/createAjaxRequest';
import getSectionState from 'Utilities/State/getSectionState';
import updateSectionState from 'Utilities/State/updateSectionState';
import { set, update, updateItem } from './baseActions';
import createHandleActions from './Creators/createHandleActions';

//
// Variables

export const section = 'search';
let abortCurrentRequest = null;

//
// State

export const defaultState = {
  isFetching: false,
  isPopulated: false,
  error: null,
  isAdding: false,
  isAdded: false,
  addError: null,
  items: [],

  volumeDefaults: {
    rootFolderPath: '',
    monitor: monitorOptions[0].key,
    monitorNewItems: monitorNewItemsOptions[0].key,
    qualityProfileId: 0,
    metadataProfileId: 0,
    tags: []
  },

  issueDefaults: {
    rootFolderPath: '',
    monitor: monitorOptions[0].key,
    monitorNewItems: monitorNewItemsOptions[0].key,
    qualityProfileId: 0,
    metadataProfileId: 0,
    tags: []
  }
};

export const persistState = [
  'search.issueDefaults',
  'search.volumeDefaults'
];

//
// Actions Types

export const GET_SEARCH_RESULTS = 'search/getSearchResults';
export const ADD_VOLUME = 'search/addVolume';
export const ADD_ISSUE = 'search/addIssue';
export const CLEAR_SEARCH_RESULTS = 'search/clearSearchResults';
export const SET_VOLUME_ADD_DEFAULT = 'search/setVolumeAddDefault';
export const SET_ISSUE_ADD_DEFAULT = 'search/setIssueAddDefault';

//
// Action Creators

export const getSearchResults = createThunk(GET_SEARCH_RESULTS);
export const addVolume = createThunk(ADD_VOLUME);
export const addIssue = createThunk(ADD_ISSUE);
export const clearSearchResults = createAction(CLEAR_SEARCH_RESULTS);
export const setVolumeAddDefault = createAction(SET_VOLUME_ADD_DEFAULT);
export const setIssueAddDefault = createAction(SET_ISSUE_ADD_DEFAULT);

//
// Action Handlers

export const actionHandlers = handleThunks({

  [GET_SEARCH_RESULTS]: function(getState, payload, dispatch) {
    dispatch(set({ section, isFetching: true }));

    if (abortCurrentRequest) {
      abortCurrentRequest();
    }

    const { request, abortRequest } = createAjaxRequest({
      url: '/search',
      data: {
        term: payload.term
      }
    });

    abortCurrentRequest = abortRequest;

    request.done((data) => {
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

    request.fail((xhr) => {
      dispatch(set({
        section,
        isFetching: false,
        isPopulated: false,
        error: xhr.aborted ? null : xhr
      }));
    });
  },

  [ADD_VOLUME]: function(getState, payload, dispatch) {
    dispatch(set({ section, isAdding: true }));

    const foreignVolumeId = payload.foreignVolumeId;
    const items = getState().search.items;
    const itemToAdd = _.find(items, { foreignId: foreignVolumeId });
    const newVolume = getNewVolume(_.cloneDeep(itemToAdd.volume), payload);

    const promise = createAjaxRequest({
      url: '/volume',
      method: 'POST',
      dataType: 'json',
      contentType: 'application/json',
      data: JSON.stringify(newVolume)
    }).request;

    promise.done((data) => {
      dispatch(batchActions([
        updateItem({ section: 'volumes', ...data }),

        set({
          section,
          isAdding: false,
          isAdded: true,
          addError: null
        })
      ]));
    });

    promise.fail((xhr) => {
      dispatch(set({
        section,
        isAdding: false,
        isAdded: false,
        addError: xhr
      }));
    });
  },

  [ADD_ISSUE]: function(getState, payload, dispatch) {
    dispatch(set({ section, isAdding: true }));

    const foreignIssueId = payload.foreignIssueId;
    const items = getState().search.items;
    const itemToAdd = _.find(items, { foreignId: foreignIssueId });
    const newIssue = getNewIssue(_.cloneDeep(itemToAdd.issue), payload);

    const promise = createAjaxRequest({
      url: '/issue',
      method: 'POST',
      dataType: 'json',
      contentType: 'application/json',
      data: JSON.stringify(newIssue)
    }).request;

    promise.done((data) => {
      itemToAdd.issue = data;
      dispatch(batchActions([
        updateItem({ section: 'volumes', ...data.volume }),
        updateItem({ section: 'issues', ...data }),
        updateItem({ section, ...itemToAdd }),

        set({
          section,
          isAdding: false,
          isAdded: true,
          addError: null
        })
      ]));
    });

    promise.fail((xhr) => {
      dispatch(set({
        section,
        isAdding: false,
        isAdded: false,
        addError: xhr
      }));
    });
  }
});

//
// Reducers

export const reducers = createHandleActions({

  [SET_VOLUME_ADD_DEFAULT]: function(state, { payload }) {
    const newState = getSectionState(state, section);

    newState.volumeDefaults = {
      ...newState.volumeDefaults,
      ...payload
    };

    return updateSectionState(state, section, newState);
  },

  [SET_ISSUE_ADD_DEFAULT]: function(state, { payload }) {
    const newState = getSectionState(state, section);

    newState.issueDefaults = {
      ...newState.issueDefaults,
      ...payload
    };

    return updateSectionState(state, section, newState);
  },

  [CLEAR_SEARCH_RESULTS]: function(state) {
    const {
      volumeDefaults,
      issueDefaults,
      ...otherDefaultState
    } = defaultState;

    return Object.assign({}, state, otherDefaultState);
  }

}, defaultState, section);
