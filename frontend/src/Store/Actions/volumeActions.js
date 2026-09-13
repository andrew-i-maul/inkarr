import _ from 'lodash';
import { createAction } from 'redux-actions';
import { batchActions } from 'redux-batched-actions';
import { filterTypePredicates, filterTypes, sortDirections } from 'Helpers/Props';
import { createThunk, handleThunks } from 'Store/thunks';
import createAjaxRequest from 'Utilities/createAjaxRequest';
import dateFilterPredicate from 'Utilities/Date/dateFilterPredicate';
import { set, updateItem } from './baseActions';
import { fetchIssues } from './issueActions';
import createFetchHandler from './Creators/createFetchHandler';
import createHandleActions from './Creators/createHandleActions';
import createRemoveItemHandler from './Creators/createRemoveItemHandler';
import createSaveProviderHandler from './Creators/createSaveProviderHandler';
import createSetSettingValueReducer from './Creators/Reducers/createSetSettingValueReducer';

//
// Variables

export const section = 'volumes';

export const filters = [
  {
    key: 'all',
    label: 'All',
    filters: []
  },
  {
    key: 'monitored',
    label: 'Monitored Only',
    filters: [
      {
        key: 'monitored',
        value: true,
        type: filterTypes.EQUAL
      }
    ]
  },
  {
    key: 'unmonitored',
    label: 'Unmonitored Only',
    filters: [
      {
        key: 'monitored',
        value: false,
        type: filterTypes.EQUAL
      }
    ]
  },
  {
    key: 'continuing',
    label: 'Continuing Only',
    filters: [
      {
        key: 'status',
        value: 'continuing',
        type: filterTypes.EQUAL
      }
    ]
  },
  {
    key: 'ended',
    label: 'Ended Only',
    filters: [
      {
        key: 'status',
        value: 'ended',
        type: filterTypes.EQUAL
      }
    ]
  },
  {
    key: 'missing',
    label: 'Missing Issues',
    filters: [
      {
        key: 'missing',
        value: true,
        type: filterTypes.EQUAL
      }
    ]
  }
];

export const filterPredicates = {
  missing: function(item) {
    const { statistics = {} } = item;

    return statistics.issueCount - statistics.issueFileCount > 0;
  },

  nextIssue: function(item, filterValue, type) {
    return dateFilterPredicate(item.nextIssue, filterValue, type);
  },

  lastIssue: function(item, filterValue, type) {
    return dateFilterPredicate(item.lastIssue, filterValue, type);
  },

  added: function(item, filterValue, type) {
    return dateFilterPredicate(item.added, filterValue, type);
  },

  ratings: function(item, filterValue, type) {
    const predicate = filterTypePredicates[type];

    return predicate(item.ratings.value * 10, filterValue);
  },

  issueCount: function(item, filterValue, type) {
    const predicate = filterTypePredicates[type];
    const issueCount = item.statistics ? item.statistics.issueCount : 0;

    return predicate(issueCount, filterValue);
  },

  sizeOnDisk: function(item, filterValue, type) {
    const predicate = filterTypePredicates[type];
    const sizeOnDisk = item.statistics && item.statistics.sizeOnDisk ?
      item.statistics.sizeOnDisk :
      0;

    return predicate(sizeOnDisk, filterValue);
  }
};

export const sortPredicates = {
  status: function(item) {
    let result = 0;

    if (item.monitored) {
      result += 2;
    }

    if (item.status === 'continuing') {
      result++;
    }

    return result;
  },

  sizeOnDisk: function(item) {
    const { statistics = {} } = item;

    return statistics.sizeOnDisk || 0;
  }
};

//
// State

export const defaultState = {
  isFetching: false,
  isPopulated: false,
  error: null,
  isSaving: false,
  saveError: null,
  items: [],
  sortKey: 'sortName',
  sortDirection: sortDirections.ASCENDING,
  pendingChanges: {}
};

//
// Actions Types

export const FETCH_VOLUME = 'volumes/fetchVolume';
export const SET_VOLUME_VALUE = 'volumes/setVolumeValue';
export const SAVE_VOLUME = 'volumes/saveVolume';
export const DELETE_VOLUME = 'volumes/deleteVolume';

export const TOGGLE_VOLUME_MONITORED = 'volumes/toggleVolumeMonitored';
export const TOGGLE_ISSUE_MONITORED = 'volumes/toggleIssueMonitored';
export const UPDATE_ISSUE_MONITORED = 'volumes/updateIssueMonitored';

//
// Action Creators

export const fetchVolume = createThunk(FETCH_VOLUME);
export const saveVolume = createThunk(SAVE_VOLUME, (payload) => {
  const newPayload = {
    ...payload
  };

  if (payload.moveFiles) {
    newPayload.queryParams = {
      moveFiles: true
    };
  }

  delete newPayload.moveFiles;

  return newPayload;
});

export const deleteVolume = createThunk(DELETE_VOLUME, (payload) => {
  return {
    ...payload,
    queryParams: {
      deleteFiles: payload.deleteFiles,
      addImportListExclusion: payload.addImportListExclusion
    }
  };
});

export const toggleVolumeMonitored = createThunk(TOGGLE_VOLUME_MONITORED);
export const toggleIssueMonitored = createThunk(TOGGLE_ISSUE_MONITORED);
export const updateIssueMonitor = createThunk(UPDATE_ISSUE_MONITORED);

export const setVolumeValue = createAction(SET_VOLUME_VALUE, (payload) => {
  return {
    section,
    ...payload
  };
});

//
// Helpers

function getSaveAjaxOptions({ ajaxOptions, payload }) {
  if (payload.moveFolder) {
    ajaxOptions.url = `${ajaxOptions.url}?moveFolder=true`;
  }

  return ajaxOptions;
}

//
// Action Handlers

export const actionHandlers = handleThunks({

  [FETCH_VOLUME]: createFetchHandler(section, '/volume'),
  [SAVE_VOLUME]: createSaveProviderHandler(section, '/volume', { getAjaxOptions: getSaveAjaxOptions }),
  [DELETE_VOLUME]: createRemoveItemHandler(section, '/volume'),

  [TOGGLE_VOLUME_MONITORED]: (getState, payload, dispatch) => {
    const {
      volumeId: id,
      monitored
    } = payload;

    const volume = _.find(getState().volumes.items, { id });

    dispatch(updateItem({
      id,
      section,
      isSaving: true
    }));

    const promise = createAjaxRequest({
      url: `/volume/${id}`,
      method: 'PUT',
      data: JSON.stringify({
        ...volume,
        monitored
      }),
      dataType: 'json'
    }).request;

    promise.done((data) => {
      dispatch(updateItem({
        id,
        section,
        isSaving: false,
        monitored
      }));
    });

    promise.fail((xhr) => {
      dispatch(updateItem({
        id,
        section,
        isSaving: false
      }));
    });
  },

  [TOGGLE_ISSUE_MONITORED]: function(getState, payload, dispatch) {
    const {
      volumeId: id,
      seasonNumber,
      monitored
    } = payload;

    const volume = _.find(getState().volumes.items, { id });
    const seasons = _.cloneDeep(volume.seasons);
    const season = _.find(seasons, { seasonNumber });

    season.isSaving = true;

    dispatch(updateItem({
      id,
      section,
      seasons
    }));

    season.monitored = monitored;

    const promise = createAjaxRequest({
      url: `/volume/${id}`,
      method: 'PUT',
      data: JSON.stringify({
        ...volume,
        seasons
      }),
      dataType: 'json'
    }).request;

    promise.done((data) => {
      const issues = _.filter(getState().issues.items, { volumeId: id, seasonNumber });

      dispatch(batchActions([
        updateItem({
          id,
          section,
          ...data
        }),

        ...issues.map((issue) => {
          return updateItem({
            id: issue.id,
            section: 'issues',
            monitored
          });
        })
      ]));
    });

    promise.fail((xhr) => {
      dispatch(updateItem({
        id,
        section,
        seasons: volume.seasons
      }));
    });
  },

  [UPDATE_ISSUE_MONITORED]: function(getState, payload, dispatch) {
    const {
      id,
      monitor
    } = payload;

    const volumeToUpdate = { id };

    if (monitor !== 'None') {
      volumeToUpdate.monitored = true;
    }

    dispatch(set({
      section,
      isSaving: true
    }));

    const promise = createAjaxRequest({
      url: '/bookshelf',
      method: 'POST',
      data: JSON.stringify({
        volumes: [{ id }],
        monitoringOptions: { monitor }
      }),
      dataType: 'json'
    }).request;

    promise.done((data) => {
      dispatch(fetchIssues({ volumeId: id }));

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

  [SET_VOLUME_VALUE]: createSetSettingValueReducer(section)

}, defaultState, section);
