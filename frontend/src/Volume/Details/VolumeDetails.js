import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { Tab, TabList, TabPanel, Tabs } from 'react-tabs';
import DeleteVolumeModal from 'Volume/Delete/DeleteVolumeModal';
import EditVolumeModalConnector from 'Volume/Edit/EditVolumeModalConnector';
import VolumeHistoryTable from 'Volume/History/VolumeHistoryTable';
import MonitoringOptionsModal from 'Volume/MonitoringOptions/MonitoringOptionsModal';
import IssueEditorFooter from 'Issue/Editor/IssueEditorFooter';
import IssueFileEditorTable from 'IssueFile/Editor/IssueFileEditorTable';
import Alert from 'Components/Alert';
import IconButton from 'Components/Link/IconButton';
import Link from 'Components/Link/Link';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import PageToolbar from 'Components/Page/Toolbar/PageToolbar';
import PageToolbarButton from 'Components/Page/Toolbar/PageToolbarButton';
import PageToolbarSection from 'Components/Page/Toolbar/PageToolbarSection';
import PageToolbarSeparator from 'Components/Page/Toolbar/PageToolbarSeparator';
import SwipeHeaderConnector from 'Components/Swipe/SwipeHeaderConnector';
import { align, icons, kinds } from 'Helpers/Props';
import InteractiveSearchFilterMenuConnector from 'InteractiveSearch/InteractiveSearchFilterMenuConnector';
import InteractiveSearchTable from 'InteractiveSearch/InteractiveSearchTable';
import OrganizePreviewModalConnector from 'Organize/OrganizePreviewModalConnector';
import RetagPreviewModalConnector from 'Retag/RetagPreviewModalConnector';
import translate from 'Utilities/String/translate';
import getSelectedIds from 'Utilities/Table/getSelectedIds';
import selectAll from 'Utilities/Table/selectAll';
import toggleSelected from 'Utilities/Table/toggleSelected';
import InteractiveImportModal from '../../InteractiveImport/InteractiveImportModal';
import VolumeDetailsHeaderConnector from './VolumeDetailsHeaderConnector';
import VolumeDetailsSeasonConnector from './VolumeDetailsSeasonConnector';
import VolumeDetailsSeriesConnector from './VolumeDetailsSeriesConnector';
import styles from './VolumeDetails.css';

function getExpandedState(newState) {
  return {
    allExpanded: newState.allSelected,
    allCollapsed: newState.allUnselected,
    expandedState: newState.selectedState
  };
}

class VolumeDetails extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      isOrganizeModalOpen: false,
      isRetagModalOpen: false,
      isEditVolumeModalOpen: false,
      isDeleteVolumeModalOpen: false,
      isInteractiveImportModalOpen: false,
      isMonitorOptionsModalOpen: false,
      isEditorActive: false,
      allExpanded: false,
      allCollapsed: false,
      expandedState: {},
      allSelected: false,
      allUnselected: false,
      lastToggled: null,
      selectedState: {},
      selectedTabIndex: 0
    };
  }

  //
  // Control

  setSelectedState = (items) => {
    const {
      selectedState
    } = this.state;

    const newSelectedState = {};

    items.forEach((item) => {
      const isItemSelected = selectedState[item.id];

      if (isItemSelected) {
        newSelectedState[item.id] = isItemSelected;
      } else {
        newSelectedState[item.id] = false;
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
  };

  getSelectedIds = () => {
    return getSelectedIds(this.state.selectedState);
  };

  //
  // Listeners

  onOrganizePress = () => {
    this.setState({ isOrganizeModalOpen: true });
  };

  onOrganizeModalClose = () => {
    this.setState({ isOrganizeModalOpen: false });
  };

  onRetagPress = () => {
    this.setState({ isRetagModalOpen: true });
  };

  onRetagModalClose = () => {
    this.setState({ isRetagModalOpen: false });
  };

  onInteractiveImportPress = () => {
    this.setState({ isInteractiveImportModalOpen: true });
  };

  onInteractiveImportModalClose = () => {
    this.setState({ isInteractiveImportModalOpen: false });
  };

  onEditVolumePress = () => {
    this.setState({ isEditVolumeModalOpen: true });
  };

  onEditVolumeModalClose = () => {
    this.setState({ isEditVolumeModalOpen: false });
  };

  onDeleteVolumePress = () => {
    this.setState({
      isEditVolumeModalOpen: false,
      isDeleteVolumeModalOpen: true
    });
  };

  onDeleteVolumeModalClose = () => {
    this.setState({ isDeleteVolumeModalOpen: false });
  };

  onMonitorOptionsPress = () => {
    this.setState({ isMonitorOptionsModalOpen: true });
  };

  onMonitorOptionsClose = () => {
    this.setState({ isMonitorOptionsModalOpen: false });
  };

  onIssueEditorTogglePress = () => {
    this.setState({ isEditorActive: !this.state.isEditorActive });
  };

  onExpandAllPress = () => {
    const {
      allExpanded,
      expandedState
    } = this.state;

    this.setState(getExpandedState(selectAll(expandedState, !allExpanded)));
  };

  onExpandPress = (issueId, isExpanded) => {
    this.setState((state) => {
      const convertedState = {
        allSelected: state.allExpanded,
        allUnselected: state.allCollapsed,
        selectedState: state.expandedState
      };

      const newState = toggleSelected(convertedState, [], issueId, isExpanded, false);

      return getExpandedState(newState);
    });
  };

  onSelectAllChange = ({ value }) => {
    this.setState(selectAll(this.state.selectedState, value));
  };

  onSelectAllPress = () => {
    this.onSelectAllChange({ value: !this.state.allSelected });
  };

  onSelectedChange = (items, id, value, shiftKey = false) => {
    this.setState((state) => {
      return toggleSelected(state, items, id, value, shiftKey);
    });
  };

  onSaveSelected = (changes) => {
    this.props.onSaveSelected({
      issueIds: this.getSelectedIds(),
      ...changes
    });
  };

  onTabSelect = (index, lastIndex) => {
    this.setState({ selectedTabIndex: index });
  };

  //
  // Render

  render() {
    const {
      id,
      volumeName,
      path,
      monitored,
      isRefreshing,
      isSearching,
      isFetching,
      isPopulated,
      issuesError,
      issueFilesError,
      hasIssues,
      hasMonitoredIssues,
      hasSeries,
      series,
      hasIssueFiles,
      previousVolume,
      nextVolume,
      onRefreshPress,
      onSearchPress,
      isSaving,
      saveError,
      isDeleting,
      deleteError,
      statistics = {}
    } = this.props;

    const {
      issueFileCount = 0,
      totalIssueCount = 0
    } = statistics;

    const {
      isOrganizeModalOpen,
      isRetagModalOpen,
      isEditVolumeModalOpen,
      isDeleteVolumeModalOpen,
      isInteractiveImportModalOpen,
      isMonitorOptionsModalOpen,
      isEditorActive,
      allSelected,
      selectedState,
      allExpanded,
      allCollapsed,
      expandedState,
      selectedTabIndex
    } = this.state;

    let expandIcon = icons.EXPAND_INDETERMINATE;

    if (allExpanded) {
      expandIcon = icons.COLLAPSE;
    } else if (allCollapsed) {
      expandIcon = icons.EXPAND;
    }

    const selectedIssueIds = this.getSelectedIds();

    return (
      <PageContent title={volumeName}>
        <PageToolbar>
          <PageToolbarSection>
            <PageToolbarButton
              label={translate('RefreshAndScan')}
              iconName={icons.REFRESH}
              spinningName={icons.REFRESH}
              title={translate('RefreshInformationAndScanDisk')}
              isSpinning={isRefreshing}
              onPress={onRefreshPress}
            />

            <PageToolbarButton
              label={translate('SearchMonitored')}
              iconName={icons.SEARCH}
              isDisabled={!monitored || !hasMonitoredIssues || !hasIssues}
              isSpinning={isSearching}
              title={hasMonitoredIssues ? undefined : translate('HasMonitoredBooksNoMonitoredBooksForThisAuthor')}
              onPress={onSearchPress}
            />

            <PageToolbarSeparator />

            <PageToolbarButton
              label={translate('PreviewRename')}
              iconName={icons.ORGANIZE}
              isDisabled={!hasIssueFiles}
              onPress={this.onOrganizePress}
            />

            <PageToolbarButton
              label={translate('PreviewRetag')}
              iconName={icons.RETAG}
              isDisabled={!hasIssueFiles}
              onPress={this.onRetagPress}
            />

            <PageToolbarButton
              label={translate('ManualImport')}
              iconName={icons.INTERACTIVE}
              onPress={this.onInteractiveImportPress}
            />

            <PageToolbarSeparator />

            <PageToolbarButton
              label={translate('BookMonitoring')}
              iconName={icons.MONITORED}
              onPress={this.onMonitorOptionsPress}
            />

            <PageToolbarButton
              label={translate('Edit')}
              iconName={icons.EDIT}
              onPress={this.onEditVolumePress}
            />

            <PageToolbarButton
              label={translate('Delete')}
              iconName={icons.DELETE}
              onPress={this.onDeleteVolumePress}
            />

            <PageToolbarSeparator />

            {
              isEditorActive ?
                <PageToolbarButton
                  label={translate('BookList')}
                  iconName={icons.VOLUME_CONTINUING}
                  onPress={this.onIssueEditorTogglePress}
                /> :
                <PageToolbarButton
                  label={translate('BookEditor')}
                  iconName={icons.EDIT}
                  onPress={this.onIssueEditorTogglePress}
                />
            }

            {
              isEditorActive ?
                <PageToolbarButton
                  label={allSelected ? translate('UnselectAll') : translate('SelectAll')}
                  iconName={icons.CHECK_SQUARE}
                  onPress={this.onSelectAllPress}
                /> :
                null
            }

          </PageToolbarSection>

          <PageToolbarSection alignContent={align.RIGHT}>
            <PageToolbarButton
              label={allExpanded ? translate('AllExpandedCollapseAll') : translate('AllExpandedExpandAll')}
              iconName={expandIcon}
              onPress={this.onExpandAllPress}
            />
          </PageToolbarSection>
        </PageToolbar>

        <PageContentBody innerClassName={styles.innerContentBody}>
          <SwipeHeaderConnector
            className={styles.header}
            nextLink={`/volume/${nextVolume.titleSlug}`}
            nextComponent={(width) => <VolumeDetailsHeaderConnector volumeId={nextVolume.id} width={width} />}
            prevLink={`/volume/${previousVolume.titleSlug}`}
            prevComponent={(width) => <VolumeDetailsHeaderConnector volumeId={previousVolume.id} width={width} />}
            currentComponent={(width) => <VolumeDetailsHeaderConnector volumeId={id} width={width} />}
          >
            <div className={styles.volumeNavigationButtons}>
              <IconButton
                className={styles.volumeNavigationButton}
                name={icons.ARROW_LEFT}
                size={30}
                title={translate('GoToInterp', [previousVolume.volumeName])}
                to={`/volume/${previousVolume.titleSlug}`}
              />

              <IconButton
                className={styles.volumeUpButton}
                name={icons.ARROW_UP}
                size={30}
                title={translate('GoToAuthorListing')}
                to={'/'}
              />

              <IconButton
                className={styles.volumeNavigationButton}
                name={icons.ARROW_RIGHT}
                size={30}
                title={translate('GoToInterp', [nextVolume.volumeName])}
                to={`/volume/${nextVolume.titleSlug}`}
              />
            </div>
          </SwipeHeaderConnector>

          <div className={styles.contentContainer}>
            {
              !isPopulated && !issuesError && !issueFilesError ?
                <LoadingIndicator /> :
                null
            }

            {
              !isFetching && issuesError ?
                <Alert kind={kinds.DANGER}>
                  {translate('LoadingBooksFailed')}
                </Alert> :
                null
            }

            {
              !isFetching && issueFilesError ?
                <Alert kind={kinds.DANGER}>
                  {translate('LoadingBookFilesFailed')}
                </Alert> :
                null
            }

            {
              isPopulated &&
                <Tabs selectedIndex={this.state.tabIndex} onSelect={this.onTabSelect}>
                  <TabList
                    className={styles.tabList}
                  >
                    <Tab
                      className={styles.tab}
                      selectedClassName={styles.selectedTab}
                    >
                      {translate('IssuesTotal', [totalIssueCount])}
                    </Tab>

                    <Tab
                      className={styles.tab}
                      selectedClassName={styles.selectedTab}
                    >
                      {translate('SeriesTotal', [series.length])}
                    </Tab>

                    <Tab
                      className={styles.tab}
                      selectedClassName={styles.selectedTab}
                    >
                      {translate('History')}
                    </Tab>

                    <Tab
                      className={styles.tab}
                      selectedClassName={styles.selectedTab}
                    >
                      {translate('Search')}
                    </Tab>

                    <Tab
                      className={styles.tab}
                      selectedClassName={styles.selectedTab}
                    >
                      {translate('FilesTotal', [issueFileCount])}
                    </Tab>

                    {
                      selectedTabIndex === 3 &&
                        <div className={styles.filterIcon}>
                          <InteractiveSearchFilterMenuConnector
                            type="volume"
                          />
                        </div>
                    }
                  </TabList>

                  <TabPanel>
                    <VolumeDetailsSeasonConnector
                      volumeId={id}
                      isExpanded={true}
                      selectedState={selectedState}
                      onExpandPress={this.onExpandPress}
                      setSelectedState={this.setSelectedState}
                      onSelectedChange={this.onSelectedChange}
                      isEditorActive={isEditorActive}
                    />
                  </TabPanel>

                  <TabPanel>
                    {
                      isPopulated && hasSeries &&
                        <div>
                          {
                            series.map((item) => {
                              return (
                                <VolumeDetailsSeriesConnector
                                  key={item.id}
                                  seriesId={item.id}
                                  volumeId={id}
                                  isExpanded={expandedState[item.id]}
                                  onExpandPress={this.onExpandPress}
                                />
                              );
                            })
                          }
                        </div>
                    }
                  </TabPanel>

                  <TabPanel>
                    <VolumeHistoryTable
                      volumeId={id}
                    />
                  </TabPanel>

                  <TabPanel>
                    <InteractiveSearchTable
                      type="volume"
                      volumeId={id}
                    />
                  </TabPanel>

                  <TabPanel>
                    <IssueFileEditorTable
                      volumeId={id}
                    />
                  </TabPanel>
                </Tabs>
            }
          </div>

          <div className={styles.metadataMessage}>
            {translate('TooManyBooks')}
            <Link to='/settings/profiles'> {translate('MetadataProfile')} </Link>
            or manually
            <Link to={`/add/search?term=${encodeURIComponent(volumeName)}`}> {translate('Search')} </Link>
            for new items!
          </div>

          <OrganizePreviewModalConnector
            isOpen={isOrganizeModalOpen}
            volumeId={id}
            onModalClose={this.onOrganizeModalClose}
          />

          <RetagPreviewModalConnector
            isOpen={isRetagModalOpen}
            volumeId={id}
            onModalClose={this.onRetagModalClose}
          />

          <EditVolumeModalConnector
            isOpen={isEditVolumeModalOpen}
            volumeId={id}
            onModalClose={this.onEditVolumeModalClose}
            onDeleteVolumePress={this.onDeleteVolumePress}
          />

          <DeleteVolumeModal
            isOpen={isDeleteVolumeModalOpen}
            volumeId={id}
            onModalClose={this.onDeleteVolumeModalClose}
          />

          <InteractiveImportModal
            isOpen={isInteractiveImportModalOpen}
            volumeId={id}
            folder={path}
            allowVolumeChange={false}
            showFilterExistingFiles={true}
            showImportMode={false}
            onModalClose={this.onInteractiveImportModalClose}
          />

          <MonitoringOptionsModal
            isOpen={isMonitorOptionsModalOpen}
            volumeId={id}
            onModalClose={this.onMonitorOptionsClose}
          />
        </PageContentBody>

        {
          isEditorActive &&
            <IssueEditorFooter
              issueIds={selectedIssueIds}
              selectedCount={selectedIssueIds.length}
              isSaving={isSaving}
              saveError={saveError}
              isDeleting={isDeleting}
              deleteError={deleteError}
              onSaveSelected={this.onSaveSelected}
            />
        }
      </PageContent>
    );
  }
}

VolumeDetails.propTypes = {
  id: PropTypes.number.isRequired,
  volumeName: PropTypes.string.isRequired,
  ratings: PropTypes.object.isRequired,
  path: PropTypes.string.isRequired,
  statistics: PropTypes.object.isRequired,
  qualityProfileId: PropTypes.number.isRequired,
  monitored: PropTypes.bool.isRequired,
  status: PropTypes.string.isRequired,
  overview: PropTypes.string,
  links: PropTypes.arrayOf(PropTypes.object).isRequired,
  images: PropTypes.arrayOf(PropTypes.object).isRequired,
  alternateTitles: PropTypes.arrayOf(PropTypes.string).isRequired,
  tags: PropTypes.arrayOf(PropTypes.number).isRequired,
  isRefreshing: PropTypes.bool.isRequired,
  isSearching: PropTypes.bool.isRequired,
  isFetching: PropTypes.bool.isRequired,
  isPopulated: PropTypes.bool.isRequired,
  issuesError: PropTypes.object,
  issueFilesError: PropTypes.object,
  hasIssues: PropTypes.bool.isRequired,
  hasMonitoredIssues: PropTypes.bool.isRequired,
  hasSeries: PropTypes.bool.isRequired,
  series: PropTypes.arrayOf(PropTypes.object).isRequired,
  hasIssueFiles: PropTypes.bool.isRequired,
  previousVolume: PropTypes.object.isRequired,
  nextVolume: PropTypes.object.isRequired,
  isSmallScreen: PropTypes.bool.isRequired,
  onMonitorTogglePress: PropTypes.func.isRequired,
  onRefreshPress: PropTypes.func.isRequired,
  onSearchPress: PropTypes.func.isRequired,
  isSaving: PropTypes.bool.isRequired,
  saveError: PropTypes.object,
  isDeleting: PropTypes.bool.isRequired,
  deleteError: PropTypes.object,
  onSaveSelected: PropTypes.func.isRequired
};

VolumeDetails.defaultProps = {
  statistics: {},
  tags: []
};

export default VolumeDetails;
