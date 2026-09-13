import migrateAddVolumeDefaults from './migrateAddVolumeDefaults';
import migrateVolumeSortKey from './migrateVolumeSortKey';
import migrateBlacklistToBlocklist from './migrateBlacklistToBlocklist';

export default function migrate(persistedState) {
  migrateAddVolumeDefaults(persistedState);
  migrateVolumeSortKey(persistedState);
  migrateBlacklistToBlocklist(persistedState);
}
