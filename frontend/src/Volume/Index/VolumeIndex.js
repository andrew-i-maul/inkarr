import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import RetagVolumeModal from 'Volume/Editor/AudioTags/RetagVolumeModal';
import VolumeEditorFooter from 'Volume/Editor/VolumeEditorFooter';
import OrganizeVolumeModal from 'Volume/Editor/Organize/OrganizeVolumeModal';
import NoVolume from 'Volume/NoVolume';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import PageJumpBar from 'Components/Page/PageJumpBar';
import PageToolbar from 'Components/Page/Toolbar/PageToolbar';
import PageToolbarButton from 'Components/Page/Toolbar/PageToolbarButton';
import PageToolbarSection from 'Components/Page/Toolbar/PageToolbarSection';
import PageToolbarSeparator from 'Components/Page/Toolbar/PageToolbarSeparator';
import TableOptionsModalWrapper from 'Components/Table/TableOptions/TableOptionsModalWrapper';
import { align, icons, sortDirections } from 'Helpers/Props';
import getErrorMessage from 'Utilities/Object/getErrorMessage';
import hasDifferentItemsOrOrder from 'Utilities/Object/hasDifferentItemsOrOrder';
import translate from 'Utilities/String/translate';
import getSelectedIds from 'Utilities/Table/getSelectedIds';
import selectAll from 'Utilities/Table/selectAll';
import toggleSelected from 'Utilities/Table/toggleSelected';
import VolumeIndexFooterConnector from './VolumeIndexFooterConnector';
import VolumeIndexFilterMenu from './Menus/VolumeIndexFilterMenu';
import VolumeIndexSortMenu from './Menus/VolumeIndexSortMenu';
import VolumeIndexViewMenu from './Menus/VolumeIndexViewMenu';
import VolumeIndexOverviewsConnector from './Overview/VolumeIndexOverviewsConnector';
import VolumeIndexOverviewOptionsModal from './Overview/Options/VolumeIndexOverviewOptionsModal';
import VolumeIndexPostersConnector from './Posters/VolumeIndexPostersConnector';
import VolumeIndexPosterOptionsModal from './Posters/Options/VolumeIndexPosterOptionsModal';
import VolumeIndexTableConnector from './Table/VolumeIndexTableConnector';
import VolumeIndexTableOptionsConnector from './Table/VolumeIndexTableOptionsConnector';
import styles from './VolumeIndex.css';

function getViewComponent(view) {
  if (view === 'posters') {
    return VolumeIndexPostersConnector;
  }

  if (view === 'overview') {
    return VolumeIndexOverviewsConnector;
  }

  return VolumeIndexTableConnector;
}

class VolumeIndex extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      scroller: null,
      jumpBarItems: { order: [] },
      jumpToCharacter: null,
      isPosterOptionsModalOpen: false,
      isOverviewOptionsModalOpen: false,
      isEditorActive: false,
      isOrganizingVolumeModalOpen: false,
      isRetaggingVolumeModalOpen: false,
      allSelected: false,
      allUnselected: false,
      lastToggled: null,
      selectedState: {}
    };
  }

  componentDidMount() {
    this.setJumpBarItems();
    this.setSelectedState();
  }

  componentDidUpdate(prevProps) {
    const {
      items,
      sortKey,
      sortDirection
    } = this.props;

    if (sortKey !== prevProps.sortKey ||
        sortDirection !== prevProps.sortDirection ||
        hasDifferentItemsOrOrder(prevProps.items, items)
    ) {
      this.setJumpBarItems();
      this.setSelectedState();
    }

    if (this.state.jumpToCharacter != null) {
      this.setState({ jumpToCharacter: null });
    }
  }

  //
  // Control

  setScrollerRef = (ref) => {
    this.setState({ scroller: ref });
  };

  getSelectedIds = () => {
    if (this.state.allUnselected) {
      return [];
    }
    return getSelectedIds(this.state.selectedState);
  };

  setSelectedState() {
    const {
      items
    } = this.props;

    const {
      selectedState
    } = this.state;

    const newSelectedState = {};

    items.forEach((author) => {
      const isItemSelected = selectedState[author.id];

      if (isItemSelected) {
        newSelectedState[author.id] = isItemSelected;
      } else {
        newSelectedState[author.id] = false;
      }
    });

    const selectedCount = getSelectedIds(newSelectedState).length;
    const newStateCount = Object.keys(newSelectedState).length;
    let isAllSelected = false;
    let isAllUnselected = false;

    if (selectedCount === 0) {
      isAllUnselected = true;
    } else if (selectedCount === newStateCount) {
      isAllSelected = true;
    }

    this.setState({ selectedState: newSelectedState, allSelected: isAllSelected, allUnselected: isAllUnselected });
  }

  setJumpBarItems() {
    const {
      items,
      sortKey,
      sortDirection
    } = this.props;

    // Reset if not sorting by sortName
    if (sortKey !== 'sortName' && sortKey !== 'sortNameLastFirst') {
      this.setState({ jumpBarItems: { order: [] } });
      return;
    }

    const characters = _.reduce(items, (acc, item) => {
      let char = item[sortKey].charAt(0);

      if (!isNaN(char)) {
        char = '#';
      }

      if (char in acc) {
        acc[char] = acc[char] + 1;
      } else {
        acc[char] = 1;
      }

      return acc;
    }, {});

    const order = Object.keys(characters).sort();

    // Reverse if sorting descending
    if (sortDirection === sortDirections.DESCENDING) {
      order.reverse();
    }

    const jumpBarItems = {
      characters,
      order
    };

    this.setState({ jumpBarItems });
  }

  //
  // Listeners

  onPosterOptionsPress = () => {
    this.setState({ isPosterOptionsModalOpen: true });
  };

  onPosterOptionsModalClose = () => {
    this.setState({ isPosterOptionsModalOpen: false });
  };

  onOverviewOptionsPress = () => {
    this.setState({ isOverviewOptionsModalOpen: true });
  };

  onOverviewOptionsModalClose = () => {
    this.setState({ isOverviewOptionsModalOpen: false });
  };

  onEditorTogglePress = () => {
    if (this.state.isEditorActive) {
      this.setState({ isEditorActive: false });
    } else {
      const newState = selectAll(this.state.selectedState, false);
      newState.isEditorActive = true;
      this.setState(newState);
    }
  };

  onJumpBarItemPress = (jumpToCharacter) => {
    this.setState({ jumpToCharacter });
  };

  onSelectAllChange = ({ value }) => {
    this.setState(selectAll(this.state.selectedState, value));
  };

  onSelectAllPress = () => {
    this.onSelectAllChange({ value: !this.state.allSelected });
  };

  onSelectedChange = ({ id, value, shiftKey = false }) => {
    this.setState((state) => {
      return toggleSelected(state, this.props.items, id, value, shiftKey);
    });
  };

  onSaveSelected = (changes) => {
    this.props.onSaveSelected({
      authorIds: this.getSelectedIds(),
      ...changes
    });
  };

  onOrganizeVolumePress = () => {
    this.setState({ isOrganizingVolumeModalOpen: true });
  };

  onOrganizeVolumeModalClose = (organized) => {
    this.setState({ isOrganizingVolumeModalOpen: false });

    if (organized === true) {
      this.onSelectAllChange({ value: false });
    }
  };

  onRetagVolumePress = () => {
    this.setState({ isRetaggingVolumeModalOpen: true });
  };

  onRetagVolumeModalClose = (organized) => {
    this.setState({ isRetaggingVolumeModalOpen: false });

    if (organized === true) {
      this.onSelectAllChange({ value: false });
    }
  };

  onRefreshVolumePress = () => {
    const selectedIds = this.getSelectedIds();
    const refreshIds = this.state.isEditorActive && selectedIds.length > 0 ? selectedIds : [];

    this.props.onRefreshVolumePress(refreshIds);
  };

  //
  // Render

  render() {
    const {
      isFetching,
      isPopulated,
      error,
      totalItems,
      items,
      columns,
      selectedFilterKey,
      filters,
      customFilters,
      sortKey,
      sortDirection,
      view,
      isRefreshingVolume,
      isRssSyncExecuting,
      isOrganizingVolume,
      isRetaggingVolume,
      isSaving,
      saveError,
      isDeleting,
      deleteError,
      onScroll,
      onSortSelect,
      onFilterSelect,
      onViewSelect,
      onRssSyncPress,
      ...otherProps
    } = this.props;

    const {
      scroller,
      jumpBarItems,
      jumpToCharacter,
      isPosterOptionsModalOpen,
      isOverviewOptionsModalOpen,
      isEditorActive,
      selectedState,
      allSelected,
      allUnselected
    } = this.state;

    const selectedVolumeIds = this.getSelectedIds();

    const ViewComponent = getViewComponent(view);
    const isLoaded = !!(!error && isPopulated && items.length && scroller);
    const hasNoVolume = !totalItems;

    const refreshLabel = isEditorActive && selectedVolumeIds.length > 0 ? translate('UpdateSelected') : translate('UpdateAll');

    return (
      <PageContent>
        <PageToolbar>
          <PageToolbarSection>
            <PageToolbarButton
              label={refreshLabel}
              iconName={icons.REFRESH}
              spinningName={icons.REFRESH}
              isSpinning={isRefreshingVolume}
              onPress={this.onRefreshVolumePress}
            />

            <PageToolbarButton
              label={translate('RSSSync')}
              iconName={icons.RSS}
              isSpinning={isRssSyncExecuting}
              isDisabled={hasNoVolume}
              onPress={onRssSyncPress}
            />

            <PageToolbarSeparator />

            {
              isEditorActive ?
                <PageToolbarButton
                  label={translate('VolumeIndex')}
                  iconName={icons.AUTHOR_CONTINUING}
                  isDisabled={hasNoVolume}
                  onPress={this.onEditorTogglePress}
                /> :
                <PageToolbarButton
                  label={translate('VolumeEditor')}
                  iconName={icons.EDIT}
                  isDisabled={hasNoVolume}
                  onPress={this.onEditorTogglePress}
                />
            }

            {
              isEditorActive ?
                <PageToolbarButton
                  label={allSelected ? translate('UnselectAll') : translate('SelectAll')}
                  iconName={icons.CHECK_SQUARE}
                  isDisabled={hasNoVolume}
                  onPress={this.onSelectAllPress}
                /> :
                null
            }

          </PageToolbarSection>

          <PageToolbarSection
            alignContent={align.RIGHT}
            collapseButtons={false}
          >
            {
              view === 'table' ?
                <TableOptionsModalWrapper
                  {...otherProps}
                  columns={columns}
                  optionsComponent={VolumeIndexTableOptionsConnector}
                >
                  <PageToolbarButton
                    label={translate('Options')}
                    iconName={icons.TABLE}
                  />
                </TableOptionsModalWrapper> :
                null
            }

            {
              view === 'posters' ?
                <PageToolbarButton
                  label={translate('Options')}
                  iconName={icons.POSTER}
                  isDisabled={hasNoVolume}
                  onPress={this.onPosterOptionsPress}
                /> :
                null
            }

            {
              view === 'overview' ?
                <PageToolbarButton
                  label={translate('Options')}
                  iconName={icons.OVERVIEW}
                  isDisabled={hasNoVolume}
                  onPress={this.onOverviewOptionsPress}
                /> :
                null
            }

            <PageToolbarSeparator />

            <VolumeIndexViewMenu
              view={view}
              isDisabled={hasNoVolume}
              onViewSelect={onViewSelect}
            />

            <VolumeIndexSortMenu
              sortKey={sortKey}
              sortDirection={sortDirection}
              isDisabled={hasNoVolume}
              onSortSelect={onSortSelect}
            />

            <VolumeIndexFilterMenu
              selectedFilterKey={selectedFilterKey}
              filters={filters}
              customFilters={customFilters}
              isDisabled={hasNoVolume}
              onFilterSelect={onFilterSelect}
            />
          </PageToolbarSection>
        </PageToolbar>

        <div className={styles.pageContentBodyWrapper}>
          <PageContentBody
            registerScroller={this.setScrollerRef}
            className={styles.contentBody}
            innerClassName={styles[`${view}InnerContentBody`]}
            onScroll={onScroll}
          >
            {
              isFetching && !isPopulated &&
                <LoadingIndicator />
            }

            {
              !isFetching && !!error &&
                <div className={styles.errorMessage}>
                  {getErrorMessage(error, 'Failed to load author from API')}
                </div>
            }

            {
              isLoaded &&
                <div className={styles.contentBodyContainer}>
                  <ViewComponent
                    scroller={scroller}
                    items={items}
                    filters={filters}
                    sortKey={sortKey}
                    sortDirection={sortDirection}
                    jumpToCharacter={jumpToCharacter}
                    isEditorActive={isEditorActive}
                    allSelected={allSelected}
                    allUnselected={allUnselected}
                    onSelectedChange={this.onSelectedChange}
                    onSelectAllChange={this.onSelectAllChange}
                    selectedState={selectedState}
                    {...otherProps}
                  />

                  <VolumeIndexFooterConnector />
                </div>
            }

            {
              !error && isPopulated && !items.length &&
                <NoVolume totalItems={totalItems} />
            }
          </PageContentBody>

          {
            isLoaded && !!jumpBarItems.order.length &&
              <PageJumpBar
                items={jumpBarItems}
                onItemPress={this.onJumpBarItemPress}
              />
          }
        </div>

        {
          isLoaded && isEditorActive &&
            <VolumeEditorFooter
              authorIds={selectedVolumeIds}
              selectedCount={selectedVolumeIds.length}
              isSaving={isSaving}
              saveError={saveError}
              isDeleting={isDeleting}
              deleteError={deleteError}
              isOrganizingVolume={isOrganizingVolume}
              isRetaggingVolume={isRetaggingVolume}
              showMetadataProfile={true}
              onSaveSelected={this.onSaveSelected}
              onOrganizeVolumePress={this.onOrganizeVolumePress}
              onRetagVolumePress={this.onRetagVolumePress}
            />
        }

        <VolumeIndexPosterOptionsModal
          isOpen={isPosterOptionsModalOpen}
          onModalClose={this.onPosterOptionsModalClose}
        />

        <VolumeIndexOverviewOptionsModal
          isOpen={isOverviewOptionsModalOpen}
          onModalClose={this.onOverviewOptionsModalClose}
        />

        <OrganizeVolumeModal
          isOpen={this.state.isOrganizingVolumeModalOpen}
          authorIds={selectedVolumeIds}
          onModalClose={this.onOrganizeVolumeModalClose}
        />

        <RetagVolumeModal
          isOpen={this.state.isRetaggingVolumeModalOpen}
          authorIds={selectedVolumeIds}
          onModalClose={this.onRetagVolumeModalClose}
        />

      </PageContent>
    );
  }
}

VolumeIndex.propTypes = {
  isFetching: PropTypes.bool.isRequired,
  isPopulated: PropTypes.bool.isRequired,
  error: PropTypes.object,
  totalItems: PropTypes.number.isRequired,
  items: PropTypes.arrayOf(PropTypes.object).isRequired,
  columns: PropTypes.arrayOf(PropTypes.object).isRequired,
  selectedFilterKey: PropTypes.oneOfType([PropTypes.string, PropTypes.number]).isRequired,
  filters: PropTypes.arrayOf(PropTypes.object).isRequired,
  customFilters: PropTypes.arrayOf(PropTypes.object).isRequired,
  sortKey: PropTypes.string,
  sortDirection: PropTypes.oneOf(sortDirections.all),
  view: PropTypes.string.isRequired,
  isRefreshingVolume: PropTypes.bool.isRequired,
  isOrganizingVolume: PropTypes.bool.isRequired,
  isRetaggingVolume: PropTypes.bool.isRequired,
  isRssSyncExecuting: PropTypes.bool.isRequired,
  isSmallScreen: PropTypes.bool.isRequired,
  isSaving: PropTypes.bool.isRequired,
  saveError: PropTypes.object,
  isDeleting: PropTypes.bool.isRequired,
  deleteError: PropTypes.object,
  onSortSelect: PropTypes.func.isRequired,
  onFilterSelect: PropTypes.func.isRequired,
  onViewSelect: PropTypes.func.isRequired,
  onRefreshVolumePress: PropTypes.func.isRequired,
  onRssSyncPress: PropTypes.func.isRequired,
  onScroll: PropTypes.func.isRequired,
  onSaveSelected: PropTypes.func.isRequired
};

export default VolumeIndex;
