import PropTypes from 'prop-types';
import React from 'react';
import Button from 'Components/Link/Button';
import { kinds } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import styles from './NoVolume.css';

function NoVolume(props) {
  const {
    totalItems,
    itemType
  } = props;

  if (totalItems > 0) {
    return (
      <div>
        <div className={styles.message}>
          {`All ${itemType} are hidden due to the applied filter.`}
        </div>
      </div>
    );
  }

  return (
    <div>
      <div className={styles.message}>
        {`No ${itemType} found, to get started you'll want to add a new volume or issue or add an existing library location (Root Folder) and update.`}
      </div>

      <div className={styles.buttonContainer}>
        <Button
          to="/settings/mediamanagement"
          kind={kinds.PRIMARY}
        >
          {translate('AddRootFolder')}
        </Button>
      </div>

      <div className={styles.buttonContainer}>
        <Button
          to="/add/search"
          kind={kinds.PRIMARY}
        >
          {translate('AddNewAuthor')}
        </Button>
      </div>
    </div>
  );
}

NoVolume.propTypes = {
  totalItems: PropTypes.number.isRequired,
  itemType: PropTypes.string.isRequired
};

NoVolume.defaultProps = {
  itemType: 'volumes'
};

export default NoVolume;
