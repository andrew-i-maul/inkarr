import classNames from 'classnames';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import VolumeBanner from 'Volume/VolumeBanner';
import VolumeNameLink from 'Volume/VolumeNameLink';
import DeleteVolumeModal from 'Volume/Delete/DeleteVolumeModal';
import EditVolumeModalConnector from 'Volume/Edit/EditVolumeModalConnector';
import IssueTitleLink from 'Issue/IssueTitleLink';
import HeartRating from 'Components/HeartRating';
import IconButton from 'Components/Link/IconButton';
import Link from 'Components/Link/Link';
import SpinnerIconButton from 'Components/Link/SpinnerIconButton';
import ProgressBar from 'Components/ProgressBar';
import RelativeDateCellConnector from 'Components/Table/Cells/RelativeDateCellConnector';
import VirtualTableRowCell from 'Components/Table/Cells/VirtualTableRowCell';
import VirtualTableSelectCell from 'Components/Table/Cells/VirtualTableSelectCell';
import TagListConnector from 'Components/TagListConnector';
import { icons } from 'Helpers/Props';
import getProgressBarKind from 'Utilities/Volume/getProgressBarKind';
import formatBytes from 'Utilities/Number/formatBytes';
import translate from 'Utilities/String/translate';
import VolumeStatusCell from './VolumeStatusCell';
import hasGrowableColumns from './hasGrowableColumns';
import styles from './VolumeIndexRow.css';

class VolumeIndexRow extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      hasBannerError: false,
      isEditVolumeModalOpen: false,
      isDeleteVolumeModalOpen: false
    };
  }

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

  onUseSceneNumberingChange = () => {
    // Mock handler to satisfy `onChange` being required for `CheckInput`.
    //
  };

  onBannerLoad = () => {
    if (this.state.hasBannerError) {
      this.setState({ hasBannerError: false });
    }
  };

  onBannerLoadError = () => {
    if (!this.state.hasBannerError) {
      this.setState({ hasBannerError: true });
    }
  };

  //
  // Render

  render() {
    const {
      id,
      monitored,
      status,
      authorName,
      authorNameLastFirst,
      titleSlug,
      qualityProfile,
      metadataProfile,
      nextIssue,
      lastIssue,
      added,
      statistics = {},
      genres,
      ratings,
      path,
      tags,
      images,
      showBanners,
      showTitle,
      showSearchAction,
      columns,
      isRefreshingVolume,
      isSearchingVolume,
      isEditorActive,
      isSelected,
      onRefreshVolumePress,
      onSearchPress,
      onSelectedChange
    } = this.props;

    const {
      bookCount = 0,
      availableIssueCount = 0,
      bookFileCount = 0,
      totalIssueCount = 0,
      sizeOnDisk = 0
    } = statistics;

    const {
      hasBannerError,
      isEditVolumeModalOpen,
      isDeleteVolumeModalOpen
    } = this.state;

    return (
      <>
        {
          columns.map((column) => {
            const {
              name,
              isVisible
            } = column;

            if (!isVisible) {
              return null;
            }

            if (isEditorActive && name === 'select') {
              return (
                <VirtualTableSelectCell
                  inputClassName={styles.checkInput}
                  id={id}
                  key={name}
                  isSelected={isSelected}
                  isDisabled={false}
                  onSelectedChange={onSelectedChange}
                />
              );
            }

            if (name === 'status') {
              return (
                <VolumeStatusCell
                  key={name}
                  className={styles[name]}
                  monitored={monitored}
                  status={status}
                  component={VirtualTableRowCell}
                />
              );
            }

            if (name === 'sortName') {
              return (
                <VirtualTableRowCell
                  key={name}
                  className={classNames(
                    styles[name],
                    showBanners && styles.banner,
                    showBanners && !hasGrowableColumns(columns) && styles.bannerGrow
                  )}
                >
                  {
                    showBanners ?
                      <Link
                        className={styles.link}
                        to={`/author/${titleSlug}`}
                      >
                        <VolumeBanner
                          className={styles.bannerImage}
                          images={images}
                          lazy={false}
                          overflow={true}
                          onError={this.onBannerLoadError}
                          onLoad={this.onBannerLoad}
                        />

                        {
                          hasBannerError &&
                            <div className={styles.overlayTitle}>
                              {showTitle === 'firstLast' ? authorName : authorNameLastFirst}
                            </div>
                        }
                      </Link> :

                      <VolumeNameLink
                        titleSlug={titleSlug}
                        authorName={showTitle === 'firstLast' ? authorName : authorNameLastFirst}
                      />
                  }
                </VirtualTableRowCell>
              );
            }

            if (name === 'qualityProfileId') {
              return (
                <VirtualTableRowCell
                  key={name}
                  className={styles[name]}
                >
                  {qualityProfile?.name ?? ''}
                </VirtualTableRowCell>
              );
            }

            if (name === 'metadataProfileId') {
              return (
                <VirtualTableRowCell
                  key={name}
                  className={styles[name]}
                >
                  {metadataProfile?.name ?? ''}
                </VirtualTableRowCell>
              );
            }

            if (name === 'nextIssue') {
              if (nextIssue) {
                return (
                  <VirtualTableRowCell
                    key={name}
                    className={styles[name]}
                  >
                    <IssueTitleLink
                      title={nextIssue.title}
                      disambiguation={nextIssue.disambiguation}
                      titleSlug={nextIssue.titleSlug}
                    />
                  </VirtualTableRowCell>
                );
              }
              return (
                <VirtualTableRowCell
                  key={name}
                  className={styles[name]}
                >
                  None
                </VirtualTableRowCell>
              );
            }

            if (name === 'lastIssue') {
              if (lastIssue) {
                return (
                  <VirtualTableRowCell
                    key={name}
                    className={styles[name]}
                  >
                    <IssueTitleLink
                      title={lastIssue.title}
                      disambiguation={lastIssue.disambiguation}
                      titleSlug={lastIssue.titleSlug}
                    />
                  </VirtualTableRowCell>
                );
              }
              return (
                <VirtualTableRowCell
                  key={name}
                  className={styles[name]}
                >
                  None
                </VirtualTableRowCell>
              );
            }

            if (name === 'added') {
              return (
                <RelativeDateCellConnector
                  key={name}
                  className={styles[name]}
                  date={added}
                  component={VirtualTableRowCell}
                />
              );
            }

            if (name === 'bookProgress') {
              const progress = bookCount ? (availableIssueCount / bookCount) * 100 : 100;

              return (
                <VirtualTableRowCell
                  key={name}
                  className={styles[name]}
                >
                  <ProgressBar
                    progress={progress}
                    kind={getProgressBarKind(status, monitored, progress)}
                    showText={true}
                    text={`${availableIssueCount} / ${bookCount}`}
                    title={translate('VolumeProgressBarText', { bookCount, availableIssueCount, bookFileCount, totalIssueCount })}
                    width={125}
                  />
                </VirtualTableRowCell>
              );
            }

            if (name === 'path') {
              return (
                <VirtualTableRowCell
                  key={name}
                  className={styles[name]}
                >
                  {path}
                </VirtualTableRowCell>
              );
            }

            if (name === 'sizeOnDisk') {
              return (
                <VirtualTableRowCell
                  key={name}
                  className={styles[name]}
                >
                  {formatBytes(sizeOnDisk)}
                </VirtualTableRowCell>
              );
            }

            if (name === 'genres') {
              const joinedGenres = genres.join(', ');

              return (
                <VirtualTableRowCell
                  key={name}
                  className={styles[name]}
                >
                  <span title={joinedGenres}>
                    {joinedGenres}
                  </span>
                </VirtualTableRowCell>
              );
            }

            if (name === 'ratings') {
              return (
                <VirtualTableRowCell
                  key={name}
                  className={styles[name]}
                >
                  <HeartRating
                    rating={ratings.value}
                  />
                </VirtualTableRowCell>
              );
            }

            if (name === 'tags') {
              return (
                <VirtualTableRowCell
                  key={name}
                  className={styles[name]}
                >
                  <TagListConnector
                    tags={tags}
                  />
                </VirtualTableRowCell>
              );
            }

            if (name === 'actions') {
              return (
                <VirtualTableRowCell
                  key={name}
                  className={styles[name]}
                >
                  <SpinnerIconButton
                    name={icons.REFRESH}
                    title={translate('RefreshAuthor')}
                    isSpinning={isRefreshingVolume}
                    onPress={onRefreshVolumePress}
                  />

                  {
                    showSearchAction &&
                      <SpinnerIconButton
                        className={styles.action}
                        name={icons.SEARCH}
                        title={translate('SearchForMonitoredBooks')}
                        isSpinning={isSearchingVolume}
                        onPress={onSearchPress}
                      />
                  }

                  <IconButton
                    name={icons.EDIT}
                    title={translate('EditAuthor')}
                    onPress={this.onEditVolumePress}
                  />
                </VirtualTableRowCell>
              );
            }

            return null;
          })
        }

        <EditVolumeModalConnector
          isOpen={isEditVolumeModalOpen}
          authorId={id}
          onModalClose={this.onEditVolumeModalClose}
          onDeleteVolumePress={this.onDeleteVolumePress}
        />

        <DeleteVolumeModal
          isOpen={isDeleteVolumeModalOpen}
          authorId={id}
          onModalClose={this.onDeleteVolumeModalClose}
        />
      </>
    );
  }
}

VolumeIndexRow.propTypes = {
  id: PropTypes.number.isRequired,
  monitored: PropTypes.bool.isRequired,
  status: PropTypes.string.isRequired,
  authorName: PropTypes.string.isRequired,
  authorNameLastFirst: PropTypes.string.isRequired,
  titleSlug: PropTypes.string.isRequired,
  qualityProfile: PropTypes.object.isRequired,
  metadataProfile: PropTypes.object.isRequired,
  nextIssue: PropTypes.object,
  lastIssue: PropTypes.object,
  added: PropTypes.string,
  statistics: PropTypes.object.isRequired,
  latestIssue: PropTypes.object,
  path: PropTypes.string.isRequired,
  genres: PropTypes.arrayOf(PropTypes.string).isRequired,
  ratings: PropTypes.object.isRequired,
  tags: PropTypes.arrayOf(PropTypes.number).isRequired,
  images: PropTypes.arrayOf(PropTypes.object).isRequired,
  showBanners: PropTypes.bool.isRequired,
  showTitle: PropTypes.string.isRequired,
  showSearchAction: PropTypes.bool.isRequired,
  columns: PropTypes.arrayOf(PropTypes.object).isRequired,
  isRefreshingVolume: PropTypes.bool.isRequired,
  isSearchingVolume: PropTypes.bool.isRequired,
  onRefreshVolumePress: PropTypes.func.isRequired,
  onSearchPress: PropTypes.func.isRequired,
  isEditorActive: PropTypes.bool.isRequired,
  isSelected: PropTypes.bool,
  onSelectedChange: PropTypes.func.isRequired
};

VolumeIndexRow.defaultProps = {
  statistics: {
    bookCount: 0,
    bookFileCount: 0,
    totalIssueCount: 0
  },
  genres: [],
  tags: []
};

export default VolumeIndexRow;
