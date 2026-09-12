import PropTypes from 'prop-types';
import React, { Component } from 'react';
import VolumePoster from 'Volume/VolumePoster';
import DeleteVolumeModal from 'Volume/Delete/DeleteVolumeModal';
import EditVolumeModalConnector from 'Volume/Edit/EditVolumeModalConnector';
import VolumeIndexProgressBar from 'Volume/Index/ProgressBar/VolumeIndexProgressBar';
import CheckInput from 'Components/Form/CheckInput';
import Label from 'Components/Label';
import IconButton from 'Components/Link/IconButton';
import Link from 'Components/Link/Link';
import SpinnerIconButton from 'Components/Link/SpinnerIconButton';
import { icons } from 'Helpers/Props';
import getRelativeDate from 'Utilities/Date/getRelativeDate';
import translate from 'Utilities/String/translate';
import VolumeIndexPosterInfo from './VolumeIndexPosterInfo';
import styles from './VolumeIndexPoster.css';

class VolumeIndexPoster extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      hasPosterError: false,
      isEditVolumeModalOpen: false,
      isDeleteVolumeModalOpen: false
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
      authorName,
      authorNameLastFirst,
      monitored,
      titleSlug,
      status,
      nextAiring,
      statistics = {},
      images,
      posterWidth,
      posterHeight,
      detailedProgressBar,
      showTitle,
      showMonitored,
      showQualityProfile,
      qualityProfile,
      metadataProfile,
      showSearchAction,
      showRelativeDates,
      shortDateFormat,
      timeFormat,
      isRefreshingVolume,
      isSearchingVolume,
      onRefreshVolumePress,
      onSearchPress,
      isEditorActive,
      isSelected,
      onSelectedChange,
      ...otherProps
    } = this.props;

    const {
      bookCount = 0,
      availableIssueCount = 0,
      bookFileCount = 0,
      totalIssueCount = 0,
      sizeOnDisk = 0
    } = statistics;

    const {
      hasPosterError,
      isEditVolumeModalOpen,
      isDeleteVolumeModalOpen
    } = this.state;

    const link = `/author/${titleSlug}`;

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
                title={translate('RefreshVolume')}
                isSpinning={isRefreshingVolume}
                onPress={onRefreshVolumePress}
              />

              {
                showSearchAction &&
                  <SpinnerIconButton
                    className={styles.action}
                    name={icons.SEARCH}
                    title={translate('SearchForMonitoredIssues')}
                    isSpinning={isSearchingVolume}
                    onPress={onSearchPress}
                  />
              }

              <IconButton
                className={styles.action}
                name={icons.EDIT}
                title={translate('EditVolume')}
                onPress={this.onEditVolumePress}
              />
            </Label>

            {
              status === 'ended' &&
                <div
                  className={styles.ended}
                  title={translate('Ended')}
                />
            }

            <Link
              className={styles.link}
              style={elementStyle}
              to={link}
            >
              <VolumePoster
                className={styles.poster}
                style={elementStyle}
                images={images}
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
                    {authorName}
                  </div>
              }

            </Link>
          </div>

          <VolumeIndexProgressBar
            monitored={monitored}
            status={status}
            bookCount={bookCount}
            availableIssueCount={availableIssueCount}
            bookFileCount={bookFileCount}
            totalIssueCount={totalIssueCount}
            posterWidth={posterWidth}
            detailedProgressBar={detailedProgressBar}
          />

          {
            showTitle !== 'no' &&
              <div className={styles.title}>
                {showTitle === 'firstLast' ? authorName : authorNameLastFirst}
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
          <VolumeIndexPosterInfo
            bookCount={bookCount}
            sizeOnDisk={sizeOnDisk}
            qualityProfile={qualityProfile}
            showQualityProfile={showQualityProfile}
            metadataProfile={metadataProfile}
            showRelativeDates={showRelativeDates}
            shortDateFormat={shortDateFormat}
            timeFormat={timeFormat}
            {...otherProps}
          />

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
        </div>
      </div>
    );
  }
}

VolumeIndexPoster.propTypes = {
  id: PropTypes.number.isRequired,
  authorName: PropTypes.string.isRequired,
  authorNameLastFirst: PropTypes.string.isRequired,
  monitored: PropTypes.bool.isRequired,
  status: PropTypes.string.isRequired,
  titleSlug: PropTypes.string.isRequired,
  nextAiring: PropTypes.string,
  statistics: PropTypes.object.isRequired,
  images: PropTypes.arrayOf(PropTypes.object).isRequired,
  posterWidth: PropTypes.number.isRequired,
  posterHeight: PropTypes.number.isRequired,
  detailedProgressBar: PropTypes.bool.isRequired,
  showTitle: PropTypes.string.isRequired,
  showMonitored: PropTypes.bool.isRequired,
  showQualityProfile: PropTypes.bool.isRequired,
  qualityProfile: PropTypes.object.isRequired,
  metadataProfile: PropTypes.object.isRequired,
  showSearchAction: PropTypes.bool.isRequired,
  showRelativeDates: PropTypes.bool.isRequired,
  shortDateFormat: PropTypes.string.isRequired,
  timeFormat: PropTypes.string.isRequired,
  isRefreshingVolume: PropTypes.bool.isRequired,
  isSearchingVolume: PropTypes.bool.isRequired,
  onRefreshVolumePress: PropTypes.func.isRequired,
  onSearchPress: PropTypes.func.isRequired,
  isEditorActive: PropTypes.bool.isRequired,
  isSelected: PropTypes.bool,
  onSelectedChange: PropTypes.func.isRequired
};

VolumeIndexPoster.defaultProps = {
  statistics: {
    bookCount: 0,
    bookFileCount: 0,
    totalIssueCount: 0
  }
};

export default VolumeIndexPoster;
