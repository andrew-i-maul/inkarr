import { get } from 'lodash';
import monitorOptions from 'Utilities/Volume/monitorOptions';

export default function migrateAddVolumeDefaults(persistedState) {
  const monitor = get(persistedState, 'addVolume.defaults.monitor');

  if (!monitor) {
    return;
  }

  if (!monitorOptions.find((option) => option.key === monitor)) {
    persistedState.addVolume.defaults.monitor = monitorOptions[0].key;
  }
}
