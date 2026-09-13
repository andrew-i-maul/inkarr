import PropTypes from 'prop-types';
import React from 'react';
import VolumePoster from 'Volume/VolumePoster';
import styles from './IssueSearchResult.css';

function IssueSearchResult(props) {
  const {
    name,
    images
  } = props;

  return (
    <div className={styles.result}>
      <VolumePoster
        className={styles.poster}
        images={images}
        coverType={'cover'}
        size={250}
        lazy={false}
        overflow={true}
      />

      <div className={styles.titles}>
        <div className={styles.title}>
          {name}
        </div>
      </div>
    </div>
  );
}

IssueSearchResult.propTypes = {
  name: PropTypes.string.isRequired,
  images: PropTypes.arrayOf(PropTypes.object).isRequired,
  tags: PropTypes.arrayOf(PropTypes.object).isRequired,
  match: PropTypes.object.isRequired
};

export default IssueSearchResult;
