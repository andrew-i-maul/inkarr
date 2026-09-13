import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { Tab, TabList, TabPanel, Tabs } from 'react-tabs';
import VolumeHistoryTable from 'Volume/History/VolumeHistoryTable';
import DeleteIssueModal from 'Issue/Delete/DeleteIssueModal';
import EditIssueModalConnector from 'Issue/Edit/EditIssueModalConnector';
import IssueFileEditorTable from 'IssueFile/Editor/IssueFileEditorTable';
import IconButton from 'Components/Link/IconButton';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import PageToolbar from 'Components/Page/Toolbar/PageToolbar';
import PageToolbarButton from 'Components/Page/Toolbar/PageToolbarButton';
import PageToolbarSection from 'Components/Page/Toolbar/PageToolbarSection';
import PageToolbarSeparator from 'Components/Page/Toolbar/PageToolbarSeparator';
import SwipeHeaderConnector from 'Components/Swipe/SwipeHeaderConnector';
import { icons } from 'Helpers/Props';
import InteractiveSearchFilterMenuConnector from 'InteractiveSearch/InteractiveSearchFilterMenuConnector';
import InteractiveSearchTable from 'InteractiveSearch/InteractiveSearchTable';
import OrganizePreviewModalConnector from 'Organize/OrganizePreviewModalConnector';
import RetagPreviewModalConnector from 'Retag/RetagPreviewModalConnector';
import translate from 'Utilities/String/translate';
import IssueDetailsHeaderConnector from './IssueDetailsHeaderConnector';
import styles from './IssueDetails.css';

class IssueDetails extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      isOrganizeModalOpen: false,
      isRetagModalOpen: false,
      isEditIssueModalOpen: false,
      isDeleteIssueModalOpen: false,
      selectedTabIndex: 0
    };
  }

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

  onEditIssuePress = () => {
    this.setState({ isEditIssueModalOpen: true });
  };

  onEditIssueModalClose = () => {
    this.setState({ isEditIssueModalOpen: false });
  };

  onDeleteIssuePress = () => {
    this.setState({
      isEditIssueModalOpen: false,
      isDeleteIssueModalOpen: true
    });
  };

  onDeleteIssueModalClose = () => {
    this.setState({ isDeleteIssueModalOpen: false });
  };

  onTabSelect = (index, lastIndex) => {
    this.setState({ selectedTabIndex: index });
  };

  //
  // Render

  render() {
    const {
      id,
      title,
      isRefreshing,
      isFetching,
      isPopulated,
      bookFilesError,
      hasIssueFiles,
      author,
      previousIssue,
      nextIssue,
      isSearching,
      onRefreshPress,
      onSearchPress,
      statistics = {}
    } = this.props;

    const {
      bookFileCount = 0
    } = statistics;

    const {
      isOrganizeModalOpen,
      isRetagModalOpen,
      isEditIssueModalOpen,
      isDeleteIssueModalOpen,
      selectedTabIndex
    } = this.state;

    return (
      <PageContent title={title}>
        <PageToolbar>
          <PageToolbarSection>
            <PageToolbarButton
              label={translate('Refresh')}
              iconName={icons.REFRESH}
              spinningName={icons.REFRESH}
              title={translate('RefreshInformation')}
              isSpinning={isRefreshing}
              onPress={onRefreshPress}
            />

            <PageToolbarButton
              label={translate('SearchBook')}
              iconName={icons.SEARCH}
              isSpinning={isSearching}
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

            <PageToolbarSeparator />

            <PageToolbarButton
              label={translate('Edit')}
              iconName={icons.EDIT}
              onPress={this.onEditIssuePress}
            />

            <PageToolbarButton
              label={translate('Delete')}
              iconName={icons.DELETE}
              onPress={this.onDeleteIssuePress}
            />

          </PageToolbarSection>
        </PageToolbar>

        <PageContentBody innerClassName={styles.innerContentBody}>
          <SwipeHeaderConnector
            className={styles.header}
            nextLink={`/book/${nextIssue.titleSlug}`}
            nextComponent={(width) => (
              <IssueDetailsHeaderConnector
                bookId={nextIssue.id}
                author={author}
                width={width}
              />
            )}
            prevLink={`/book/${previousIssue.titleSlug}`}
            prevComponent={(width) => (
              <IssueDetailsHeaderConnector
                bookId={previousIssue.id}
                author={author}
                width={width}
              />
            )}
            currentComponent={(width) => (
              <IssueDetailsHeaderConnector
                bookId={id}
                author={author}
                width={width}
              />
            )}
          >
            <div className={styles.bookNavigationButtons}>
              <IconButton
                className={styles.bookNavigationButton}
                name={icons.ARROW_LEFT}
                size={30}
                title={translate('GoToInterp', [previousIssue.title])}
                to={`/book/${previousIssue.titleSlug}`}
              />

              <IconButton
                className={styles.bookUpButton}
                name={icons.ARROW_UP}
                size={30}
                title={translate('GoToInterp', [author.authorName])}
                to={`/author/${author.titleSlug}`}
              />

              <IconButton
                className={styles.bookNavigationButton}
                name={icons.ARROW_RIGHT}
                size={30}
                title={translate('GoToInterp', [nextIssue.title])}
                to={`/book/${nextIssue.titleSlug}`}
              />
            </div>
          </SwipeHeaderConnector>

          <div className={styles.contentContainer}>
            {
              !isPopulated && !bookFilesError &&
                <LoadingIndicator />
            }

            {
              !isFetching && bookFilesError &&
                <div>
                  {translate('LoadingBookFilesFailed')}
                </div>
            }

            <Tabs selectedIndex={this.state.tabIndex} onSelect={this.onTabSelect}>
              <TabList
                className={styles.tabList}
              >
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
                  {translate('FilesTotal', [bookFileCount])}
                </Tab>

                {
                  selectedTabIndex === 1 &&
                    <div className={styles.filterIcon}>
                      <InteractiveSearchFilterMenuConnector
                        type="book"
                      />
                    </div>
                }

              </TabList>

              <TabPanel>
                <VolumeHistoryTable
                  authorId={author.id}
                  bookId={id}
                />
              </TabPanel>

              <TabPanel>
                <InteractiveSearchTable
                  bookId={id}
                  type="book"
                />
              </TabPanel>

              <TabPanel>
                <IssueFileEditorTable
                  authorId={author.id}
                  bookId={id}
                />
              </TabPanel>
            </Tabs>
          </div>

          <OrganizePreviewModalConnector
            isOpen={isOrganizeModalOpen}
            authorId={author.id}
            bookId={id}
            onModalClose={this.onOrganizeModalClose}
          />

          <RetagPreviewModalConnector
            isOpen={isRetagModalOpen}
            authorId={author.id}
            bookId={id}
            onModalClose={this.onRetagModalClose}
          />

          <EditIssueModalConnector
            isOpen={isEditIssueModalOpen}
            bookId={id}
            authorId={author.id}
            onModalClose={this.onEditIssueModalClose}
            onDeleteVolumePress={this.onDeleteIssuePress}
          />

          <DeleteIssueModal
            isOpen={isDeleteIssueModalOpen}
            bookId={id}
            authorSlug={author.titleSlug}
            onModalClose={this.onDeleteIssueModalClose}
          />

        </PageContentBody>
      </PageContent>
    );
  }
}

IssueDetails.propTypes = {
  id: PropTypes.number.isRequired,
  titleSlug: PropTypes.string.isRequired,
  title: PropTypes.string.isRequired,
  seriesTitle: PropTypes.string.isRequired,
  pageCount: PropTypes.number,
  overview: PropTypes.string,
  releaseDate: PropTypes.string.isRequired,
  ratings: PropTypes.object.isRequired,
  images: PropTypes.arrayOf(PropTypes.object).isRequired,
  links: PropTypes.arrayOf(PropTypes.object).isRequired,
  statistics: PropTypes.object.isRequired,
  monitored: PropTypes.bool.isRequired,
  shortDateFormat: PropTypes.string.isRequired,
  isSaving: PropTypes.bool.isRequired,
  isRefreshing: PropTypes.bool,
  isSearching: PropTypes.bool,
  isFetching: PropTypes.bool,
  isPopulated: PropTypes.bool,
  bookFilesError: PropTypes.object,
  hasIssueFiles: PropTypes.bool.isRequired,
  author: PropTypes.object,
  previousIssue: PropTypes.object,
  nextIssue: PropTypes.object,
  isSmallScreen: PropTypes.bool.isRequired,
  onMonitorTogglePress: PropTypes.func.isRequired,
  onRefreshPress: PropTypes.func,
  onSearchPress: PropTypes.func.isRequired
};

IssueDetails.defaultProps = {
  isSaving: false
};

export default IssueDetails;
