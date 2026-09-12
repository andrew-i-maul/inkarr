import { VolumeStatus } from 'Volume/Volume';
import { icons } from 'Helpers/Props';
import translate from 'Utilities/String/translate';

export function getVolumeStatusDetails(status: VolumeStatus) {
  let statusDetails = {
    icon: icons.AUTHOR_CONTINUING,
    title: translate('StatusEndedContinuing'),
    message: translate('ContinuingMoreIssuesAreExpected'),
  };

  if (status === 'ended') {
    statusDetails = {
      icon: icons.AUTHOR_ENDED,
      title: translate('StatusEndedEnded'),
      message: translate('ContinuingNoAdditionalIssuesAreExpected'),
    };
  }

  return statusDetails;
}
