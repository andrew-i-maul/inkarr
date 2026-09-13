import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import createVolumeSelector from 'Store/Selectors/createVolumeSelector';
import createQueueItemSelector from 'Store/Selectors/createQueueItemSelector';
import createUISettingsSelector from 'Store/Selectors/createUISettingsSelector';
import CalendarEvent from './CalendarEvent';

function createMapStateToProps() {
  return createSelector(
    createVolumeSelector(),
    createQueueItemSelector(),
    createUISettingsSelector(),
    (volume, queueItem, uiSettings) => {
      return {
        volume,
        queueItem,
        timeFormat: uiSettings.timeFormat,
        colorImpairedMode: uiSettings.enableColorImpairedMode
      };
    }
  );
}

export default connect(createMapStateToProps)(CalendarEvent);
