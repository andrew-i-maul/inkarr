import PropTypes from 'prop-types';
import React, { Component } from 'react';
import VolumePoster from 'Volume/VolumePoster';
import DeleteVolumeModal from 'Volume/Delete/DeleteVolumeModal';
import EditVolumeModalConnector from 'Volume/Edit/EditVolumeModalConnector';
import EditIssueModalConnector from 'Issue/Edit/EditIssueModalConnector';
import IssueIndexProgressBar from 'Issue/Index/ProgressBar/IssueIndexProgressBar';
import CheckInput from 'Components/Form/CheckInput';
import Label from 'Components/Label';
import IconButton from 'Components/Link/IconButton';
import Link from 'Components/Link/Link';
import SpinnerIconButton from 'Components/Link/SpinnerIconButton';
import { icons } from 'Helpers/Props';
import getRelativeDate from 'Utilities/Date/getRelativeDate';
import translate from 'Utilities/String/translate';
import IssueIndexPosterInfo from './IssueIndexPosterInfo';
import styles from './IssueIndexPoster.css';

class IssueIndexPoster extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      hasPosterError: false,
      isEditVolumeModalOpen: false,
      isDeleteVolumeModalOpen: false,
      isEditIssueModalOpen: false
    };
  }

  //
  // Listeners

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

  onPosterLoad = () => {
    if (this.state.hasPosterError) {
      this.setState({ hasPosterError: false });
    }
  };

  onPosterLoadError = () => {
    if (!this.state.hasPosterError) {
      this.setState({ hasPosterError: true });
    }
  };

  onChange = ({ value, shiftKey }) => {
    const {
      id,
      onSelectedChange
    } = this.props;

    onSelectedChange({ id, value, shiftKey });
  };

  //
  // Render

  render() {
    const {
      id,
      title,
      authorId,
      author,
      monitored,
      titleSlug,
      nextAiring,
      statistics,
      images,
      posterWidth,
      posterHeight,
      detailedProgressBar,
      showTitle,
      showVolume,
      showMonitored,
      showQualityProfile,
      qualityProfile,
      showSearchAction,
      showRelativeDates,
      shortDateFormat,
      timeFormat,
      isRefreshingIssue,
      isSearchingIssue,
      onRefreshIssuePress,
      onSearchPress,
      isEditorActive,
      isSelected,
      onSelectedChange,
      ...otherProps
    } = this.props;

    const {
      bookCount,
      sizeOnDisk,
      bookFileCount,
      totalIssueCount
    } = statistics;

    const {
      hasPosterError,
      isEditVolumeModalOpen,
      isDeleteVolumeModalOpen,
      isEditIssueModalOpen
    } = this.state;

    const link = `/book/${titleSlug}`;

    const elementStyle = {
      width: `${posterWidth}px`,
      height: `${posterHeight}px`,
      objectFit: 'contain'
    };

    return (
      <div>
        <div className={styles.content}>
          <div className={styles.posterContainer}>
            {
              isEditorActive &&
                <div className={styles.editorSelect}>
                  <CheckInput
                    className={styles.checkInput}
                    name={id.toString()}
                    value={isSelected}
                    onChange={this.onChange}
                  />
                </div>
            }

            <Label className={styles.controls}>
              <SpinnerIconButton
                className={styles.action}
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
                className={styles.action}
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
            </Label>

            <Link
              className={styles.link}
              style={elementStyle}
              to={link}
            >
              <VolumePoster
                className={styles.poster}
                style={elementStyle}
                images={images}
                coverType={'cover'}
                size={250}
                lazy={false}
                overflow={true}
                blurBackground={true}
                onError={this.onPosterLoadError}
                onLoad={this.onPosterLoad}
              />

              {
                hasPosterError &&
                  <div className={styles.overlayTitle}>
                    {title}
                  </div>
              }

            </Link>
          </div>

          <IssueIndexProgressBar
            monitored={monitored}
            bookCount={bookCount}
            bookFileCount={bookFileCount}
            totalIssueCount={totalIssueCount}
            posterWidth={posterWidth}
            detailedProgressBar={detailedProgressBar}
          />

          {
            showTitle &&
              <div className={styles.title}>
                {title}
              </div>
          }

          {
            showVolume &&
              <div className={styles.title}>
                {author.authorName}
              </div>
          }

          {
            showMonitored &&
              <div className={styles.title}>
                {monitored ? 'Monitored' : 'Unmonitored'}
              </div>
          }

          {showQualityProfile && !!qualityProfile?.name ? (
            <div className={styles.title} title={translate('QualityProfile')}>
              {qualityProfile.name}
            </div>
          ) : null}

          {
            nextAiring &&
              <div className={styles.nextAiring}>
                {
                  getRelativeDate(
                    nextAiring,
                    shortDateFormat,
                    showRelativeDates,
                    {
                      timeFormat,
                      timeForToday: true
                    }
                  )
                }
              </div>
          }
          <IssueIndexPosterInfo
            author={author}
            bookFileCount={bookFileCount}
            sizeOnDisk={sizeOnDisk}
            qualityProfile={qualityProfile}
            showQualityProfile={showQualityProfile}
            showRelativeDates={showRelativeDates}
            shortDateFormat={shortDateFormat}
            timeFormat={timeFormat}
            {...otherProps}
          />

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
        </div>
      </div>
    );
  }
}

IssueIndexPoster.propTypes = {
  id: PropTypes.number.isRequired,
  title: PropTypes.string.isRequired,
  authorId: PropTypes.number.isRequired,
  author: PropTypes.object.isRequired,
  monitored: PropTypes.bool.isRequired,
  titleSlug: PropTypes.string.isRequired,
  nextAiring: PropTypes.string,
  statistics: PropTypes.object.isRequired,
  images: PropTypes.arrayOf(PropTypes.object).isRequired,
  posterWidth: PropTypes.number.isRequired,
  posterHeight: PropTypes.number.isRequired,
  detailedProgressBar: PropTypes.bool.isRequired,
  showTitle: PropTypes.bool.isRequired,
  showVolume: PropTypes.bool.isRequired,
  showMonitored: PropTypes.bool.isRequired,
  showQualityProfile: PropTypes.bool.isRequired,
  qualityProfile: PropTypes.object.isRequired,
  showSearchAction: PropTypes.bool.isRequired,
  showRelativeDates: PropTypes.bool.isRequired,
  shortDateFormat: PropTypes.string.isRequired,
  timeFormat: PropTypes.string.isRequired,
  isRefreshingIssue: PropTypes.bool.isRequired,
  isSearchingIssue: PropTypes.bool.isRequired,
  onRefreshIssuePress: PropTypes.func.isRequired,
  onSearchPress: PropTypes.func.isRequired,
  isEditorActive: PropTypes.bool.isRequired,
  isSelected: PropTypes.bool,
  onSelectedChange: PropTypes.func.isRequired
};

IssueIndexPoster.defaultProps = {
  statistics: {
    bookCount: 0,
    bookFileCount: 0,
    totalIssueCount: 0
  }
};

export default IssueIndexPoster;
