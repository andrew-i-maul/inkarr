import PropTypes from 'prop-types';
import React from 'react';
import ProgressBar from 'Components/ProgressBar';
import { sizes } from 'Helpers/Props';
import getProgressBarKind from 'Utilities/Volume/getProgressBarKind';
import translate from 'Utilities/String/translate';
import styles from './VolumeIndexProgressBar.css';

function VolumeIndexProgressBar(props) {
  const {
    monitored,
    status,
    bookCount,
    availableIssueCount,
    bookFileCount,
    totalIssueCount,
    posterWidth,
    detailedProgressBar
  } = props;

  const progress = bookCount ? (availableIssueCount / bookCount) * 100 : 100;
  const text = `${availableIssueCount} / ${bookCount}`;

  return (
    <ProgressBar
      className={styles.progressBar}
      containerClassName={styles.progress}
      progress={progress}
      kind={getProgressBarKind(status, monitored, progress)}
      size={detailedProgressBar ? sizes.MEDIUM : sizes.SMALL}
      showText={detailedProgressBar}
      text={text}
      title={translate('VolumeProgressBarText', { bookCount, availableIssueCount, bookFileCount, totalIssueCount })}
      width={posterWidth}
    />
  );
}

VolumeIndexProgressBar.propTypes = {
  monitored: PropTypes.bool.isRequired,
  status: PropTypes.string.isRequired,
  bookCount: PropTypes.number.isRequired,
  availableIssueCount: PropTypes.number.isRequired,
  bookFileCount: PropTypes.number.isRequired,
  totalIssueCount: PropTypes.number.isRequired,
  posterWidth: PropTypes.number.isRequired,
  detailedProgressBar: PropTypes.bool.isRequired
};

export default VolumeIndexProgressBar;
