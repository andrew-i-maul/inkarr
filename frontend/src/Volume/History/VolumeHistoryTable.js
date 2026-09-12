import React from 'react';
import VolumeHistoryContentConnector from 'Volume/History/VolumeHistoryContentConnector';
import VolumeHistoryTableContent from 'Volume/History/VolumeHistoryTableContent';
import styles from './VolumeHistoryTable.css';

function VolumeHistoryTable(props) {
  const {
    ...otherProps
  } = props;

  return (
    <div className={styles.container}>
      <VolumeHistoryContentConnector
        component={VolumeHistoryTableContent}
        {...otherProps}
      />
    </div>
  );
}

VolumeHistoryTable.propTypes = {
};

export default VolumeHistoryTable;
