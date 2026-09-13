import { createAction } from 'redux-actions';
import { batchActions } from 'redux-batched-actions';
import { filterBuilderTypes, filterBuilderValueTypes, filterTypePredicates, sortDirections } from 'Helpers/Props';
import { createThunk, handleThunks } from 'Store/thunks';
import sortByName from 'Utilities/Array/sortByName';
import createAjaxRequest from 'Utilities/createAjaxRequest';
import { filterPredicates, filters, sortPredicates } from './volumeActions';
import { set, updateItem } from './baseActions';
import createHandleActions from './Creators/createHandleActions';
import createSetClientSideCollectionFilterReducer from './Creators/Reducers/createSetClientSideCollectionFilterReducer';
import createSetClientSideCollectionSortReducer from './Creators/Reducers/createSetClientSideCollectionSortReducer';
import createSetTableOptionReducer from './Creators/Reducers/createSetTableOptionReducer';

//
// Variables

export const section = 'volumeIndex';

//
// State

export const defaultState = {
  isSaving: false,
  saveError: null,
  isDeleting: false,
  deleteError: null,
  sortKey: 'sortNameLastFirst',
  sortDirection: sortDirections.ASCENDING,
  secondarySortKey: 'sortNameLastFirst',
  secondarySortDirection: sortDirections.ASCENDING,
  view: 'posters',

  posterOptions: {
    detailedProgressBar: false,
    size: 'large',
    showTitle: 'lastFirst',
    showMonitored: true,
    showQualityProfile: true,
    showSearchAction: false
  },

  overviewOptions: {
    showTitle: 'lastFirst',
    detailedProgressBar: false,
    size: 'medium',
    showMonitored: true,
    showQualityProfile: true,
    showLastIssue: false,
    showAdded: false,
    showIssueCount: true,
    showPath: false,
    showSizeOnDisk: false,
    showSearchAction: false
  },

  tableOptions: {
    showTitle: 'lastFirst',
    showBanners: false,
    showSearchAction: false
  },

  columns: [
    {
      name: 'select',
      columnLabel: 'Select',
      isSortable: false,
      isVisible: true,
      isModifiable: false,
      isHidden: true
    },
    {
      name: 'status',
      columnLabel: 'Status',
      isSortable: true,
      isVisible: true,
      isModifiable: false
    },
    {
      name: 'sortName',
      label: 'Volume Name',
      isSortable: true,
      isVisible: true,
      isModifiable: false
    },
    {
      name: 'qualityProfileId',
      label: 'Quality Profile',
      isSortable: true,
      isVisible: true
    },
    {
      name: 'metadataProfileId',
      label: 'Metadata Profile',
      isSortable: true,
      isVisible: false
    },
    {
      name: 'nextIssue',
      label: 'Next Issue',
      isSortable: true,
      isVisible: true
    },
    {
      name: 'lastIssue',
      label: 'Last Issue',
      isSortable: true,
      isVisible: false
    },
    {
      name: 'added',
      label: 'Added',
      isSortable: true,
      isVisible: false
    },
    {
      name: 'issueProgress',
      label: 'Issues',
      isSortable: true,
      isVisible: true
    },
    {
      name: 'path',
      label: 'Path',
      isSortable: true,
      isVisible: false
    },
    {
      name: 'sizeOnDisk',
      label: 'Size on Disk',
      isSortable: true,
      isVisible: false
    },
    {
      name: 'genres',
      label: 'Genres',
      isSortable: false,
      isVisible: false
    },
    {
      name: 'ratings',
      label: 'Rating',
      isSortable: true,
      isVisible: false
    },
    {
      name: 'tags',
      label: 'Tags',
      isSortable: false,
      isVisible: false
    },
    {
      name: 'actions',
      columnLabel: 'Actions',
      isVisible: true,
      isModifiable: false
    }
  ],

  sortPredicates: {
    ...sortPredicates,

    issueProgress: function(item) {
      const { statistics = {} } = item;

      const {
        issueCount = 0,
        issueFileCount
      } = statistics;

      const progress = issueCount ? issueFileCount / issueCount * 100 : 100;

      return progress + issueCount / 1000000;
    },

    nextIssue: function(item) {
      if (item.nextIssue) {
        return item.nextIssue.releaseDate;
      }
      return '1/1/1000';
    },

    lastIssue: function(item) {
      if (item.lastIssue) {
        return item.lastIssue.releaseDate;
      }
      return '1/1/1000';
    },

    issueCount: function(item) {
      const { statistics = {} } = item;

      return statistics.issueCount || 0;
    },

    ratings: function(item) {
      const { ratings = {} } = item;

      return ratings.value;
    }
  },

  selectedFilterKey: 'all',

  filters,

  filterPredicates: {
    ...filterPredicates,

    issueProgress: function(item, filterValue, type) {
      const { statistics = {} } = item;

      const {
        issueCount = 0,
        issueFileCount
      } = statistics;

      const progress = issueCount ?
        issueFileCount / issueCount * 100 :
        100;

      const predicate = filterTypePredicates[type];

      return predicate(progress, filterValue);
    }
  },

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
      name: 'nextIssue',
      label: 'Next Issue',
      type: filterBuilderTypes.DATE,
      valueType: filterBuilderValueTypes.DATE
    },
    {
      name: 'lastIssue',
      label: 'Last Issue',
      type: filterBuilderTypes.DATE,
      valueType: filterBuilderValueTypes.DATE
    },
    {
      name: 'added',
      label: 'Added',
      type: filterBuilderTypes.DATE,
      valueType: filterBuilderValueTypes.DATE
    },
    {
      name: 'issueCount',
      label: 'Issue Count',
      type: filterBuilderTypes.NUMBER
    },
    {
      name: 'issueProgress',
      label: 'Issue Progress',
      type: filterBuilderTypes.NUMBER
    },
    {
      name: 'path',
      label: 'Path',
      type: filterBuilderTypes.STRING
    },
    {
      name: 'sizeOnDisk',
      label: 'Size on Disk',
      type: filterBuilderTypes.NUMBER,
      valueType: filterBuilderValueTypes.BYTES
    },
    {
      name: 'genres',
      label: 'Genres',
      type: filterBuilderTypes.ARRAY,
      optionsSelector: function(items) {
        const tagList = items.reduce((acc, volume) => {
          volume.genres.forEach((genre) => {
            acc.push({
              id: genre,
              name: genre
            });
          });

          return acc;
        }, []);

        return tagList.sort(sortByName);
      }
    },
    {
      name: 'ratings',
      label: 'Rating',
      type: filterBuilderTypes.NUMBER
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
  'volumeIndex.sortKey',
  'volumeIndex.sortDirection',
  'volumeIndex.selectedFilterKey',
  'volumeIndex.customFilters',
  'volumeIndex.view',
  'volumeIndex.columns',
  'volumeIndex.posterOptions',
  'volumeIndex.bannerOptions',
  'volumeIndex.overviewOptions',
  'volumeIndex.tableOptions'
];

//
// Actions Types

export const SET_VOLUME_SORT = 'volumeIndex/setVolumeSort';
export const SET_VOLUME_FILTER = 'volumeIndex/setVolumeFilter';
export const SET_VOLUME_VIEW = 'volumeIndex/setVolumeView';
export const SET_VOLUME_TABLE_OPTION = 'volumeIndex/setVolumeTableOption';
export const SET_VOLUME_POSTER_OPTION = 'volumeIndex/setVolumePosterOption';
export const SET_VOLUME_BANNER_OPTION = 'volumeIndex/setVolumeBannerOption';
export const SET_VOLUME_OVERVIEW_OPTION = 'volumeIndex/setVolumeOverviewOption';
export const SAVE_VOLUME_EDITOR = 'volumeIndex/saveVolumeEditor';
export const BULK_DELETE_VOLUME = 'volumeIndex/bulkDeleteVolume';

//
// Action Creators

export const setVolumeSort = createAction(SET_VOLUME_SORT);
export const setVolumeFilter = createAction(SET_VOLUME_FILTER);
export const setVolumeView = createAction(SET_VOLUME_VIEW);
export const setVolumeTableOption = createAction(SET_VOLUME_TABLE_OPTION);
export const setVolumePosterOption = createAction(SET_VOLUME_POSTER_OPTION);
export const setVolumeBannerOption = createAction(SET_VOLUME_BANNER_OPTION);
export const setVolumeOverviewOption = createAction(SET_VOLUME_OVERVIEW_OPTION);
export const saveVolumeEditor = createThunk(SAVE_VOLUME_EDITOR);
export const bulkDeleteVolume = createThunk(BULK_DELETE_VOLUME);

//
// Action Handlers

export const actionHandlers = handleThunks({
  [SAVE_VOLUME_EDITOR]: function(getState, payload, dispatch) {
    dispatch(set({
      section,
      isSaving: true
    }));

    const promise = createAjaxRequest({
      url: '/volume/editor',
      method: 'PUT',
      data: JSON.stringify(payload),
      dataType: 'json'
    }).request;

    promise.done((data) => {
      dispatch(batchActions([
        ...data.map((volume) => {
          return updateItem({
            id: volume.id,
            section: 'volumes',
            ...volume
          });
        }),

        set({
          section,
          isSaving: false,
          saveError: null
        })
      ]));
    });

    promise.fail((xhr) => {
      dispatch(set({
        section,
        isSaving: false,
        saveError: xhr
      }));
    });
  },

  [BULK_DELETE_VOLUME]: function(getState, payload, dispatch) {
    dispatch(set({
      section,
      isDeleting: true
    }));

    const promise = createAjaxRequest({
      url: '/volume/editor',
      method: 'DELETE',
      data: JSON.stringify(payload),
      dataType: 'json'
    }).request;

    promise.done(() => {
      // SignaR will take care of removing the volume from the collection

      dispatch(set({
        section,
        isDeleting: false,
        deleteError: null
      }));
    });

    promise.fail((xhr) => {
      dispatch(set({
        section,
        isDeleting: false,
        deleteError: xhr
      }));
    });
  }
});

//
// Reducers

export const reducers = createHandleActions({

  [SET_VOLUME_SORT]: createSetClientSideCollectionSortReducer(section),
  [SET_VOLUME_FILTER]: createSetClientSideCollectionFilterReducer(section),

  [SET_VOLUME_VIEW]: function(state, { payload }) {
    return Object.assign({}, state, { view: payload.view });
  },

  [SET_VOLUME_TABLE_OPTION]: createSetTableOptionReducer(section),

  [SET_VOLUME_POSTER_OPTION]: function(state, { payload }) {
    const posterOptions = state.posterOptions;

    return {
      ...state,
      posterOptions: {
        ...posterOptions,
        ...payload
      }
    };
  },

  [SET_VOLUME_BANNER_OPTION]: function(state, { payload }) {
    const bannerOptions = state.bannerOptions;

    return {
      ...state,
      bannerOptions: {
        ...bannerOptions,
        ...payload
      }
    };
  },

  [SET_VOLUME_OVERVIEW_OPTION]: function(state, { payload }) {
    const overviewOptions = state.overviewOptions;

    return {
      ...state,
      overviewOptions: {
        ...overviewOptions,
        ...payload
      }
    };
  }

}, defaultState, section);
