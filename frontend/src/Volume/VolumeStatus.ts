import { VolumeStatus } from 'Volume/Volume';
import { icons } from 'Helpers/Props';
import translate from 'Utilities/String/translate';

export function getVolumeStatusDetails(status: VolumeStatus) {
  let statusDetails = {
    icon: icons.VOLUME_CONTINUING,
    title: translate('StatusEndedContinuing'),
    message: translate('ContinuingMoreBooksAreExpected'),
  };

  if (status === 'ended') {
    statusDetails = {
      icon: icons.VOLUME_ENDED,
      title: translate('StatusEndedEnded'),
      message: translate('ContinuingNoAdditionalBooksAreExpected'),
    };
  }

  return statusDetails;
}
