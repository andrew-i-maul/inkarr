import PropTypes from 'prop-types';
import React from 'react';
import VolumeNameLink from 'Volume/VolumeNameLink';
import issueEntities from 'Issue/issueEntities';
import IssueSearchCellConnector from 'Issue/IssueSearchCellConnector';
import IssueTitleLink from 'Issue/IssueTitleLink';
import RelativeDateCellConnector from 'Components/Table/Cells/RelativeDateCellConnector';
import TableRowCell from 'Components/Table/Cells/TableRowCell';
import TableSelectCell from 'Components/Table/Cells/TableSelectCell';
import TableRow from 'Components/Table/TableRow';

function CutoffUnmetRow(props) {
  const {
    id,
    volume,
    releaseDate,
    titleSlug,
    title,
    lastSearchTime,
    disambiguation,
    isSelected,
    columns,
    onSelectedChange
  } = props;

  if (!volume) {
    return null;
  }

  return (
    <TableRow>
      <TableSelectCell
        id={id}
        isSelected={isSelected}
        onSelectedChange={onSelectedChange}
      />

      {
        columns.map((column) => {
          const {
            name,
            isVisible
          } = column;

          if (!isVisible) {
            return null;
          }

          if (name === 'volumeMetadata.sortName') {
            return (
              <TableRowCell key={name}>
                <VolumeNameLink
                  titleSlug={volume.titleSlug}
                  volumeName={volume.volumeName}
                />
              </TableRowCell>
            );
          }

          if (name === 'issues.title') {
            return (
              <TableRowCell key={name}>
                <IssueTitleLink
                  titleSlug={titleSlug}
                  title={title}
                  disambiguation={disambiguation}
                />
              </TableRowCell>
            );
          }

          if (name === 'issues.lastSearchTime') {
            return (
              <RelativeDateCellConnector
                key={name}
                date={lastSearchTime}
              />
            );
          }

          if (name === 'releaseDate') {
            return (
              <RelativeDateCellConnector
                key={name}
                date={releaseDate}
              />
            );
          }

          if (name === 'actions') {
            return (
              <IssueSearchCellConnector
                key={name}
                issueId={id}
                volumeId={volume.id}
                issueTitle={title}
                volumeName={volume.volumeName}
                issueEntity={issueEntities.WANTED_CUTOFF_UNMET}
                showOpenVolumeButton={true}
              />
            );
          }

          return null;
        })
      }
    </TableRow>
  );
}

CutoffUnmetRow.propTypes = {
  id: PropTypes.number.isRequired,
  issueFileId: PropTypes.number,
  volume: PropTypes.object.isRequired,
  releaseDate: PropTypes.string.isRequired,
  titleSlug: PropTypes.string.isRequired,
  title: PropTypes.string.isRequired,
  lastSearchTime: PropTypes.string,
  disambiguation: PropTypes.string,
  isSelected: PropTypes.bool,
  columns: PropTypes.arrayOf(PropTypes.object).isRequired,
  onSelectedChange: PropTypes.func.isRequired
};

export default CutoffUnmetRow;
