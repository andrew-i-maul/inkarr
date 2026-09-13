import PropTypes from 'prop-types';
import React, { Component } from 'react';
import VolumeNameLink from 'Volume/VolumeNameLink';
import { getVolumeStatusDetails } from 'Volume/VolumeStatus';
import Icon from 'Components/Icon';
import MonitorToggleButton from 'Components/MonitorToggleButton';
import VirtualTableRowCell from 'Components/Table/Cells/VirtualTableRowCell';
import VirtualTableSelectCell from 'Components/Table/Cells/VirtualTableSelectCell';
import BookshelfIssue from './BookshelfIssue';
import styles from './BookshelfRow.css';

class BookshelfRow extends Component {

  //
  // Render

  render() {
    const {
      volumeId,
      status,
      titleSlug,
      volumeName,
      monitored,
      issues,
      isSaving,
      isSelected,
      onSelectedChange,
      onVolumeMonitoredPress,
      onIssueMonitoredPress
    } = this.props;

    const statusDetails = getVolumeStatusDetails(status);

    return (
      <>
        <VirtualTableSelectCell
          className={styles.selectCell}
          id={volumeId}
          isSelected={isSelected}
          onSelectedChange={onSelectedChange}
          isDisabled={false}
        />

        <VirtualTableRowCell className={styles.monitored}>
          <MonitorToggleButton
            monitored={monitored}
            size={14}
            isSaving={isSaving}
            onPress={onVolumeMonitoredPress}
          />
        </VirtualTableRowCell>

        <VirtualTableRowCell className={styles.status}>
          <Icon
            className={styles.statusIcon}
            name={statusDetails.icon}
            title={statusDetails.title}
          />
        </VirtualTableRowCell>

        <VirtualTableRowCell className={styles.title}>
          <VolumeNameLink
            titleSlug={titleSlug}
            volumeName={volumeName}
          />
        </VirtualTableRowCell>

        <VirtualTableRowCell className={styles.issues}>
          {
            issues.map((issue) => {
              return (
                <BookshelfIssue
                  key={issue.id}
                  {...issue}
                  onIssueMonitoredPress={onIssueMonitoredPress}
                />
              );
            })
          }
        </VirtualTableRowCell>
      </>
    );
  }
}

BookshelfRow.propTypes = {
  volumeId: PropTypes.number.isRequired,
  status: PropTypes.string.isRequired,
  titleSlug: PropTypes.string.isRequired,
  volumeName: PropTypes.string.isRequired,
  monitored: PropTypes.bool.isRequired,
  issues: PropTypes.arrayOf(PropTypes.object).isRequired,
  isSaving: PropTypes.bool.isRequired,
  isSelected: PropTypes.bool,
  onSelectedChange: PropTypes.func.isRequired,
  onVolumeMonitoredPress: PropTypes.func.isRequired,
  onIssueMonitoredPress: PropTypes.func.isRequired
};

BookshelfRow.defaultProps = {
  isSaving: false
};

export default BookshelfRow;
