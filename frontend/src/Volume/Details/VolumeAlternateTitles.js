import PropTypes from 'prop-types';
import React from 'react';
import styles from './VolumeAlternateTitles.css';

function VolumeAlternateTitles({ alternateTitles }) {
  return (
    <ul>
      {
        alternateTitles.map((alternateTitle) => {
          return (
            <li
              key={alternateTitle}
              className={styles.alternateTitle}
            >
              {alternateTitle}
            </li>
          );
        })
      }
    </ul>
  );
}

VolumeAlternateTitles.propTypes = {
  alternateTitles: PropTypes.arrayOf(PropTypes.string).isRequired
};

export default VolumeAlternateTitles;
