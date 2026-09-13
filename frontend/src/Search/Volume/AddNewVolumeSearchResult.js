import PropTypes from 'prop-types';
import React, { Component } from 'react';
import TextTruncate from 'react-text-truncate';
import VolumePoster from 'Volume/VolumePoster';
import HeartRating from 'Components/HeartRating';
import Icon from 'Components/Icon';
import Label from 'Components/Label';
import Link from 'Components/Link/Link';
import { icons, kinds, sizes } from 'Helpers/Props';
import dimensions from 'Styles/Variables/dimensions';
import fonts from 'Styles/Variables/fonts';
import stripHtml from 'Utilities/String/stripHtml';
import translate from 'Utilities/String/translate';
import AddNewVolumeModal from './AddNewVolumeModal';
import styles from './AddNewVolumeSearchResult.css';

const columnPadding = parseInt(dimensions.volumeIndexColumnPadding);
const columnPaddingSmallScreen = parseInt(dimensions.volumeIndexColumnPaddingSmallScreen);
const defaultFontSize = parseInt(fonts.defaultFontSize);
const lineHeight = parseFloat(fonts.lineHeight);

function calculateHeight(rowHeight, isSmallScreen) {
  let height = rowHeight - 45;

  if (isSmallScreen) {
    height -= columnPaddingSmallScreen;
  } else {
    height -= columnPadding;
  }

  return height;
}

class AddNewVolumeSearchResult extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      isNewAddVolumeModalOpen: false
    };
  }

  componentDidUpdate(prevProps) {
    if (!prevProps.isExistingVolume && this.props.isExistingVolume) {
      this.onAddVolumeModalClose();
    }
  }

  //
  // Listeners

  onPress = () => {
    this.setState({ isNewAddVolumeModalOpen: true });
  };

  onAddVolumeModalClose = () => {
    this.setState({ isNewAddVolumeModalOpen: false });
  };

  onMBLinkPress = (event) => {
    event.stopPropagation();
  };

  //
  // Render

  render() {
    const {
      foreignVolumeId,
      titleSlug,
      volumeName,
      year,
      disambiguation,
      status,
      overview,
      ratings,
      folder,
      images,
      links,
      isExistingVolume,
      isSmallScreen
    } = this.props;

    const comicVineLink = links && links.find((link) => link.name === 'ComicVine');

    const {
      isNewAddVolumeModalOpen
    } = this.state;

    const linkProps = isExistingVolume ? { to: `/volume/${titleSlug}` } : { onPress: this.onPress };

    const endedString = 'Deceased';

    const height = calculateHeight(230, isSmallScreen);

    return (
      <div className={styles.searchResult}>
        <Link
          className={styles.underlay}
          {...linkProps}
        />

        <div className={styles.overlay}>
          {
            isSmallScreen ?
              null :
              <VolumePoster
                className={styles.poster}
                images={images}
                size={250}
                overflow={true}
                lazy={false}
              />
          }

          <div className={styles.content}>
            <div className={styles.nameRow}>
              <div className={styles.nameContainer}>
                <div className={styles.name}>
                  {volumeName}

                  {
                    !volumeName.contains(year) && year ?
                      <span className={styles.year}>
                        ({year})
                      </span> :
                      null
                  }
                  {
                    !!disambiguation &&
                      <span className={styles.year}>({disambiguation})</span>
                  }
                </div>
              </div>

              <div className={styles.icons}>
                {
                  isExistingVolume ?
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

            <div>
              {
                ratings.votes > 0 ?
                  <Label size={sizes.LARGE}>
                    <HeartRating
                      rating={ratings.value}
                      iconSize={13}
                    />
                  </Label> :
                  null
              }

              {
                status === 'ended' ?
                  <Label
                    kind={kinds.DANGER}
                    size={sizes.LARGE}
                  >
                    {endedString}
                  </Label> :
                  null
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

        <AddNewVolumeModal
          isOpen={isNewAddVolumeModalOpen && !isExistingVolume}
          foreignVolumeId={foreignVolumeId}
          volumeName={volumeName}
          disambiguation={disambiguation}
          year={year}
          overview={overview}
          folder={folder}
          images={images}
          onModalClose={this.onAddVolumeModalClose}
        />
      </div>
    );
  }
}

AddNewVolumeSearchResult.propTypes = {
  foreignVolumeId: PropTypes.string.isRequired,
  titleSlug: PropTypes.string.isRequired,
  volumeName: PropTypes.string.isRequired,
  year: PropTypes.number,
  disambiguation: PropTypes.string,
  status: PropTypes.string.isRequired,
  overview: PropTypes.string,
  ratings: PropTypes.object.isRequired,
  folder: PropTypes.string.isRequired,
  images: PropTypes.arrayOf(PropTypes.object).isRequired,
  links: PropTypes.arrayOf(PropTypes.object),
  isExistingVolume: PropTypes.bool.isRequired,
  isSmallScreen: PropTypes.bool.isRequired
};

export default AddNewVolumeSearchResult;
