import classNames from 'classnames';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import VolumeNameLink from 'Volume/VolumeNameLink';
import DeleteVolumeModal from 'Volume/Delete/DeleteVolumeModal';
import EditVolumeModalConnector from 'Volume/Edit/EditVolumeModalConnector';
import IssueNameLink from 'Issue/IssueNameLink';
import EditIssueModalConnector from 'Issue/Edit/EditIssueModalConnector';
import HeartRating from 'Components/HeartRating';
import IconButton from 'Components/Link/IconButton';
import SpinnerIconButton from 'Components/Link/SpinnerIconButton';
import RelativeDateCellConnector from 'Components/Table/Cells/RelativeDateCellConnector';
import VirtualTableRowCell from 'Components/Table/Cells/VirtualTableRowCell';
import VirtualTableSelectCell from 'Components/Table/Cells/VirtualTableSelectCell';
import TagListConnector from 'Components/TagListConnector';
import { icons } from 'Helpers/Props';
import formatBytes from 'Utilities/Number/formatBytes';
import translate from 'Utilities/String/translate';
import IssueStatusCell from './IssueStatusCell';
import styles from './IssueIndexRow.css';

class IssueIndexRow extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      hasBannerError: false,
      isEditVolumeModalOpen: false,
      isDeleteVolumeModalOpen: false,
      isEditIssueModalOpen: false
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

  onEditIssuePress = () => {
    this.setState({ isEditIssueModalOpen: true });
  };

  onEditIssueModalClose = () => {
    this.setState({ isEditIssueModalOpen: false });
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
      authorId,
      monitored,
      title,
      author,
      titleSlug,
      qualityProfile,
      releaseDate,
      added,
      statistics,
      genres,
      ratings,
      tags,
      showSearchAction,
      columns,
      isRefreshingIssue,
      isSearchingIssue,
      isEditorActive,
      isSelected,
      onRefreshIssuePress,
      onSearchPress,
      onSelectedChange
    } = this.props;

    const {
      bookFileCount,
      sizeOnDisk
    } = statistics;

    const {
      isEditVolumeModalOpen,
      isDeleteVolumeModalOpen,
      isEditIssueModalOpen
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
                <IssueStatusCell
                  key={name}
                  className={styles[name]}
                  monitored={monitored}
                  status={status}
                  component={VirtualTableRowCell}
                />
              );
            }

            if (name === 'title') {
              return (
                <VirtualTableRowCell
                  key={name}
                  className={classNames(
                    styles[name]
                  )}
                >
                  <IssueNameLink
                    titleSlug={titleSlug}
                    title={title}
                  />
                </VirtualTableRowCell>
              );
            }

            if (name === 'authorName') {
              return (
                <VirtualTableRowCell
                  key={name}
                  className={classNames(
                    styles[name]
                  )}
                >
                  <VolumeNameLink
                    titleSlug={author.titleSlug}
                    authorName={author.authorName}
                  />
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

            if (name === 'releaseDate') {
              return (
                <RelativeDateCellConnector
                  key={name}
                  className={styles[name]}
                  date={releaseDate}
                  component={VirtualTableRowCell}
                />
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

            if (name === 'bookFileCount') {
              return (
                <VirtualTableRowCell
                  key={name}
                  className={styles[name]}
                >
                  {bookFileCount}
                </VirtualTableRowCell>
              );
            }

            if (name === 'path') {
              return (
                <VirtualTableRowCell
                  key={name}
                  className={styles[name]}
                >
                  {author.path}
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
                    title={translate('RefreshIssue')}
                    isSpinning={isRefreshingIssue}
                    onPress={onRefreshIssuePress}
                  />

                  {
                    showSearchAction &&
                      <SpinnerIconButton
                        className={styles.action}
                        name={icons.SEARCH}
                        title={translate('SearchForMonitoredIssues')}
                        isSpinning={isSearchingIssue}
                        onPress={onSearchPress}
                      />
                  }

                  <IconButton
                    name={icons.INTERACTIVE}
                    title={translate('EditVolume')}
                    onPress={this.onEditVolumePress}
                  />

                  <IconButton
                    className={styles.action}
                    name={icons.EDIT}
                    title={translate('EditIssue')}
                    onPress={this.onEditIssuePress}
                  />
                </VirtualTableRowCell>
              );
            }

            return null;
          })
        }

        <EditVolumeModalConnector
          isOpen={isEditVolumeModalOpen}
          authorId={authorId}
          onModalClose={this.onEditVolumeModalClose}
          onDeleteVolumePress={this.onDeleteVolumePress}
        />

        <DeleteVolumeModal
          isOpen={isDeleteVolumeModalOpen}
          authorId={authorId}
          onModalClose={this.onDeleteVolumeModalClose}
        />

        <EditIssueModalConnector
          isOpen={isEditIssueModalOpen}
          authorId={authorId}
          bookId={id}
          onModalClose={this.onEditIssueModalClose}
        />
      </>
    );
  }
}

IssueIndexRow.propTypes = {
  id: PropTypes.number.isRequired,
  authorId: PropTypes.number.isRequired,
  monitored: PropTypes.bool.isRequired,
  title: PropTypes.string.isRequired,
  titleSlug: PropTypes.string.isRequired,
  author: PropTypes.object.isRequired,
  qualityProfile: PropTypes.object.isRequired,
  releaseDate: PropTypes.string,
  added: PropTypes.string,
  statistics: PropTypes.object.isRequired,
  genres: PropTypes.arrayOf(PropTypes.string).isRequired,
  ratings: PropTypes.object.isRequired,
  tags: PropTypes.arrayOf(PropTypes.number).isRequired,
  images: PropTypes.arrayOf(PropTypes.object).isRequired,
  showSearchAction: PropTypes.bool.isRequired,
  columns: PropTypes.arrayOf(PropTypes.object).isRequired,
  isRefreshingIssue: PropTypes.bool.isRequired,
  isSearchingIssue: PropTypes.bool.isRequired,
  onRefreshIssuePress: PropTypes.func.isRequired,
  onSearchPress: PropTypes.func.isRequired,
  isEditorActive: PropTypes.bool.isRequired,
  isSelected: PropTypes.bool,
  onSelectedChange: PropTypes.func.isRequired
};

IssueIndexRow.defaultProps = {
  statistics: {
    bookCount: 0,
    bookFileCount: 0,
    totalIssueCount: 0
  },
  genres: [],
  tags: []
};

export default IssueIndexRow;
