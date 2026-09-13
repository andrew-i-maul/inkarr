import PropTypes from 'prop-types';
import React, { Component } from 'react';
import TextTruncate from 'react-text-truncate';
import VolumePoster from 'Volume/VolumePoster';
import DeleteVolumeModal from 'Volume/Delete/DeleteVolumeModal';
import EditVolumeModalConnector from 'Volume/Edit/EditVolumeModalConnector';
import IssueIndexProgressBar from 'Issue/Index/ProgressBar/IssueIndexProgressBar';
import CheckInput from 'Components/Form/CheckInput';
import IconButton from 'Components/Link/IconButton';
import Link from 'Components/Link/Link';
import SpinnerIconButton from 'Components/Link/SpinnerIconButton';
import { icons } from 'Helpers/Props';
import dimensions from 'Styles/Variables/dimensions';
import fonts from 'Styles/Variables/fonts';
import createAjaxRequest from 'Utilities/createAjaxRequest';
import stripHtml from 'Utilities/String/stripHtml';
import translate from 'Utilities/String/translate';
import IssueIndexOverviewInfo from './IssueIndexOverviewInfo';
import styles from './IssueIndexOverview.css';

const columnPadding = parseInt(dimensions.authorIndexColumnPadding);
const columnPaddingSmallScreen = parseInt(dimensions.authorIndexColumnPaddingSmallScreen);
const defaultFontSize = parseInt(fonts.defaultFontSize);
const lineHeight = parseFloat(fonts.lineHeight);

// Hardcoded height beased on line-height of 32 + bottom margin of 10.
// Less side-effecty than using react-measure.
const titleRowHeight = 42;

function getContentHeight(rowHeight, isSmallScreen) {
  const padding = isSmallScreen ? columnPaddingSmallScreen : columnPadding;

  return rowHeight - padding;
}

class IssueIndexOverview extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      isEditVolumeModalOpen: false,
      isDeleteVolumeModalOpen: false,
      overview: ''
    };
  }

  componentDidMount() {
    const { id } = this.props;

    // Note that this component is lazy loaded by the virtualised view.
    // We want to avoid storing overviews for *all* books which is
    // why it's not put into the redux store
    const promise = createAjaxRequest({
      url: `/book/${id}/overview`
    }).request;

    promise.done((data) => {
      this.setState({ overview: data.overview });
    });
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
      monitored,
      titleSlug,
      nextAiring,
      statistics,
      images,
      posterWidth,
      posterHeight,
      qualityProfile,
      overviewOptions,
      showSearchAction,
      showRelativeDates,
      shortDateFormat,
      longDateFormat,
      timeFormat,
      rowHeight,
      isSmallScreen,
      isRefreshingIssue,
      isSearchingIssue,
      onRefreshIssuePress,
      onSearchPress,
      isEditorActive,
      isSelected,
      ...otherProps
    } = this.props;

    const {
      bookCount,
      sizeOnDisk,
      bookFileCount,
      totalIssueCount
    } = statistics;

    const {
      overview,
      isEditVolumeModalOpen,
      isDeleteVolumeModalOpen
    } = this.state;

    const link = `/book/${titleSlug}`;

    const elementStyle = {
      width: `${posterWidth}px`,
      height: `${posterHeight}px`,
      objectFit: 'contain'
    };

    const contentHeight = getContentHeight(rowHeight, isSmallScreen);
    const overviewHeight = contentHeight - titleRowHeight;

    return (
      <div className={styles.container}>
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
                coverType={'cover'}
                size={250}
                lazy={false}
                overflow={true}
                blurBackground={true}
              />
            </Link>

            <IssueIndexProgressBar
              monitored={monitored}
              bookCount={bookCount}
              bookFileCount={bookFileCount}
              totalIssueCount={totalIssueCount}
              posterWidth={posterWidth}
              detailedProgressBar={overviewOptions.detailedProgressBar}
            />
          </div>

          <div className={styles.info} style={{ maxHeight: contentHeight }}>
            <div className={styles.titleRow}>
              <Link
                className={styles.title}
                to={link}
              >
                {title}
              </Link>

              <div className={styles.actions}>
                <SpinnerIconButton
                  name={icons.REFRESH}
                  title={translate('RefreshBook')}
                  isSpinning={isRefreshingIssue}
                  onPress={onRefreshIssuePress}
                />

                {
                  showSearchAction &&
                    <SpinnerIconButton
                      className={styles.action}
                      name={icons.SEARCH}
                      title={translate('SearchForMonitoredBooks')}
                      isSpinning={isSearchingIssue}
                      onPress={onSearchPress}
                    />
                }

                <IconButton
                  name={icons.EDIT}
                  title={translate('EditAuthor')}
                  onPress={this.onEditVolumePress}
                />
              </div>
            </div>

            <div className={styles.details}>

              <Link
                className={styles.overview}
                to={link}
              >
                <TextTruncate
                  line={Math.floor(overviewHeight / (defaultFontSize * lineHeight))}
                  text={stripHtml(overview)}
                />
              </Link>

              <IssueIndexOverviewInfo
                height={overviewHeight}
                monitored={monitored}
                sizeOnDisk={sizeOnDisk}
                qualityProfile={qualityProfile}
                showRelativeDates={showRelativeDates}
                shortDateFormat={shortDateFormat}
                longDateFormat={longDateFormat}
                timeFormat={timeFormat}
                {...overviewOptions}
                {...otherProps}
              />
            </div>
          </div>
        </div>

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
    );
  }
}

IssueIndexOverview.propTypes = {
  id: PropTypes.number.isRequired,
  title: PropTypes.string.isRequired,
  monitored: PropTypes.bool.isRequired,
  titleSlug: PropTypes.string.isRequired,
  nextAiring: PropTypes.string,
  statistics: PropTypes.object.isRequired,
  images: PropTypes.arrayOf(PropTypes.object).isRequired,
  posterWidth: PropTypes.number.isRequired,
  posterHeight: PropTypes.number.isRequired,
  rowHeight: PropTypes.number.isRequired,
  qualityProfile: PropTypes.object.isRequired,
  overviewOptions: PropTypes.object.isRequired,
  showSearchAction: PropTypes.bool.isRequired,
  showRelativeDates: PropTypes.bool.isRequired,
  shortDateFormat: PropTypes.string.isRequired,
  longDateFormat: PropTypes.string.isRequired,
  timeFormat: PropTypes.string.isRequired,
  isSmallScreen: PropTypes.bool.isRequired,
  isRefreshingIssue: PropTypes.bool.isRequired,
  isSearchingIssue: PropTypes.bool.isRequired,
  onRefreshIssuePress: PropTypes.func.isRequired,
  onSearchPress: PropTypes.func.isRequired,
  isEditorActive: PropTypes.bool.isRequired,
  isSelected: PropTypes.bool,
  onSelectedChange: PropTypes.func.isRequired
};

IssueIndexOverview.defaultProps = {
  statistics: {
    bookCount: 0,
    bookFileCount: 0,
    totalIssueCount: 0
  }
};

export default IssueIndexOverview;
