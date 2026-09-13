import moment from 'moment';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import TextTruncate from 'react-text-truncate';
import IssueCover from 'Issue/IssueCover';
import HeartRating from 'Components/HeartRating';
import Icon from 'Components/Icon';
import Label from 'Components/Label';
import Link from 'Components/Link/Link';
import { icons, sizes } from 'Helpers/Props';
import dimensions from 'Styles/Variables/dimensions';
import fonts from 'Styles/Variables/fonts';
import stripHtml from 'Utilities/String/stripHtml';
import translate from 'Utilities/String/translate';
import AddNewIssueModal from './AddNewIssueModal';
import styles from './AddNewIssueSearchResult.css';

const columnPadding = parseInt(dimensions.volumeIndexColumnPadding);
const columnPaddingSmallScreen = parseInt(dimensions.volumeIndexColumnPaddingSmallScreen);
const defaultFontSize = parseInt(fonts.defaultFontSize);
const lineHeight = parseFloat(fonts.lineHeight);

function calculateHeight(rowHeight, isSmallScreen) {
  let height = rowHeight - 70;

  if (isSmallScreen) {
    height -= columnPaddingSmallScreen;
  } else {
    height -= columnPadding;
  }

  return height;
}

class AddNewIssueSearchResult extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      isNewAddIssueModalOpen: false
    };
  }

  componentDidUpdate(prevProps) {
    if (!prevProps.isExistingIssue && this.props.isExistingIssue) {
      this.onAddIssueModalClose();
    }
  }

  //
  // Listeners

  onPress = () => {
    this.setState({ isNewAddIssueModalOpen: true });
  };

  onAddIssueModalClose = () => {
    this.setState({ isNewAddIssueModalOpen: false });
  };

  onMBLinkPress = (event) => {
    event.stopPropagation();
  };

  //
  // Render

  render() {
    const {
      foreignIssueId,
      titleSlug,
      title,
      seriesTitle,
      releaseDate,
      disambiguation,
      overview,
      ratings,
      images,
      volume,
      links,
      isExistingIssue,
      isExistingVolume,
      isSmallScreen
    } = this.props;

    const comicVineLink = links && links.find((link) => link.name === 'ComicVine');

    const {
      isNewAddIssueModalOpen
    } = this.state;

    const linkProps = isExistingIssue ? { to: `/issue/${titleSlug}` } : { onPress: this.onPress };

    const height = calculateHeight(230, isSmallScreen);

    return (
      <div className={styles.searchResult}>
        <Link
          className={styles.underlay}
          {...linkProps}
        />

        <div className={styles.overlay}>
          {
            !isSmallScreen &&
              <IssueCover
                className={styles.poster}
                images={images}
                size={250}
                lazy={false}
              />
          }

          <div className={styles.content}>
            <div className={styles.titleRow}>
              <div className={styles.titleContainer}>
                <div className={styles.title}>
                  {title}

                  {
                    !!disambiguation &&
                      <span className={styles.year}>({disambiguation})</span>
                  }
                </div>
              </div>

              <div className={styles.icons}>
                {
                  isExistingIssue ?
                    <Icon
                      className={styles.alreadyExistsIcon}
                      name={icons.CHECK_CIRCLE}
                      size={36}
                      title={translate('AlreadyInYourLibrary')}
                    /> :
                    null
                }

                {
                  comicVineLink &&
                    <Link
                      className={styles.mbLink}
                      to={comicVineLink.url}
                      onPress={this.onMBLinkPress}
                    >
                      <Icon
                        className={styles.mbLinkIcon}
                        name={icons.EXTERNAL_LINK}
                        size={28}
                      />
                    </Link>
                }
              </div>
            </div>

            {
              seriesTitle &&
                <div className={styles.series}>
                  {seriesTitle}
                </div>
            }

            <div>
              <Label size={sizes.LARGE}>
                <HeartRating
                  rating={ratings.value}
                  iconSize={13}
                />
              </Label>

              {
                !!releaseDate &&
                  <Label size={sizes.LARGE}>
                    {moment(releaseDate).format('YYYY')}
                  </Label>
              }

            </div>

            <div
              className={styles.overview}
              style={{
                maxHeight: `${height}px`
              }}
            >
              <TextTruncate
                truncateText="…"
                line={Math.floor(height / (defaultFontSize * lineHeight))}
                text={stripHtml(overview)}
              />
            </div>
          </div>
        </div>

        <AddNewIssueModal
          isOpen={isNewAddIssueModalOpen && !isExistingIssue}
          isExistingVolume={isExistingVolume}
          foreignIssueId={foreignIssueId}
          issueTitle={title}
          seriesTitle={seriesTitle}
          disambiguation={disambiguation}
          volumeName={volume.volumeName}
          overview={overview}
          folder={volume.folder}
          images={images}
          onModalClose={this.onAddIssueModalClose}
        />
      </div>
    );
  }
}

AddNewIssueSearchResult.propTypes = {
  foreignIssueId: PropTypes.string.isRequired,
  titleSlug: PropTypes.string.isRequired,
  title: PropTypes.string.isRequired,
  seriesTitle: PropTypes.string,
  releaseDate: PropTypes.string,
  disambiguation: PropTypes.string,
  overview: PropTypes.string,
  ratings: PropTypes.object.isRequired,
  volume: PropTypes.object,
  images: PropTypes.arrayOf(PropTypes.object).isRequired,
  links: PropTypes.arrayOf(PropTypes.object),
  isExistingIssue: PropTypes.bool.isRequired,
  isExistingVolume: PropTypes.bool.isRequired,
  isSmallScreen: PropTypes.bool.isRequired
};

export default AddNewIssueSearchResult;
