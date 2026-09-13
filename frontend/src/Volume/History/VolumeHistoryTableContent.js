import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Alert from 'Components/Alert';
import Icon from 'Components/Icon';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import Table from 'Components/Table/Table';
import TableBody from 'Components/Table/TableBody';
import { icons, kinds } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import VolumeHistoryRowConnector from './VolumeHistoryRowConnector';
import styles from './VolumeHistoryTableContent.css';

const columns = [
  {
    name: 'eventType',
    isVisible: true
  },
  {
    name: 'book',
    label: () => translate('Book'),
    isVisible: true
  },
  {
    name: 'sourceTitle',
    label: () => translate( 'SourceTitle'),
    isVisible: true
  },
  {
    name: 'quality',
    label: () => translate('Quality'),
    isVisible: true
  },
  {
    name: 'customFormats',
    label: () => translate('CustomFormats'),
    isSortable: false,
    isVisible: true
  },
  {
    name: 'customFormatScore',
    label: React.createElement(Icon, {
      name: icons.SCORE,
      title: () => translate('CustomFormatScore')
    }),
    isSortable: true,
    isVisible: true
  },
  {
    name: 'date',
    label: () => translate('Date'),
    isVisible: true
  },
  {
    name: 'actions',
    isVisible: true
  }
];

class VolumeHistoryTableContent extends Component {

  //
  // Render

  render() {
    const {
      bookId,
      isFetching,
      isPopulated,
      error,
      items,
      onMarkAsFailedPress
    } = this.props;

    const fullVolume = bookId == null;
    const hasItems = !!items.length;

    return (
      <div>
        {
          isFetching &&
            <LoadingIndicator />
        }

        {
          !isFetching && !!error &&
            <Alert kind={kinds.DANGER}>
              {translate('UnableToLoadHistory')}
            </Alert>
        }

        {
          isPopulated && !hasItems && !error &&
            <div className={styles.blankpad}>
              {translate('NoHistory')}
            </div>
        }

        {
          isPopulated && hasItems && !error &&
            <Table columns={columns}>
              <TableBody>
                {
                  items.map((item) => {
                    return (
                      <VolumeHistoryRowConnector
                        key={item.id}
                        fullVolume={fullVolume}
                        {...item}
                        onMarkAsFailedPress={onMarkAsFailedPress}
                      />
                    );
                  })
                }
              </TableBody>
            </Table>
        }
      </div>
    );
  }
}

VolumeHistoryTableContent.propTypes = {
  bookId: PropTypes.number,
  isFetching: PropTypes.bool.isRequired,
  isPopulated: PropTypes.bool.isRequired,
  error: PropTypes.object,
  items: PropTypes.arrayOf(PropTypes.object).isRequired,
  onMarkAsFailedPress: PropTypes.func.isRequired
};

export default VolumeHistoryTableContent;
