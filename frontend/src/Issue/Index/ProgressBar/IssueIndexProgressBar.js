import PropTypes from 'prop-types';
import React from 'react';
import ProgressBar from 'Components/ProgressBar';
import { sizes } from 'Helpers/Props';
import getProgressBarKind from 'Utilities/Volume/getProgressBarKind';
import translate from 'Utilities/String/translate';
import styles from './IssueIndexProgressBar.css';

function IssueIndexProgressBar(props) {
  const {
    monitored,
    bookCount,
    bookFileCount,
    totalIssueCount,
    posterWidth,
    detailedProgressBar
  } = props;

  const progress = bookFileCount && bookCount ? (totalIssueCount / bookCount) * 100 : 0;
  const text = `${bookFileCount ? bookCount : 0} / ${totalIssueCount}`;

  return (
    <ProgressBar
      className={styles.progressBar}
      containerClassName={styles.progress}
      progress={100}
      kind={getProgressBarKind('ended', monitored, progress)}
      size={detailedProgressBar ? sizes.MEDIUM : sizes.SMALL}
      showText={detailedProgressBar}
      text={text}
      title={translate('IssueProgressBarText', {
        bookCount: bookFileCount ? bookCount : 0,
        bookFileCount,
        totalIssueCount
      })}
      width={posterWidth}
    />
  );
}

IssueIndexProgressBar.propTypes = {
  monitored: PropTypes.bool.isRequired,
  bookCount: PropTypes.number.isRequired,
  bookFileCount: PropTypes.number.isRequired,
  totalIssueCount: PropTypes.number.isRequired,
  posterWidth: PropTypes.number.isRequired,
  detailedProgressBar: PropTypes.bool.isRequired
};

export default IssueIndexProgressBar;
