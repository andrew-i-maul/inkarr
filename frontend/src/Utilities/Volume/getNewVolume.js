
function getNewVolume(volume, payload) {
  const {
    rootFolderPath,
    monitor,
    monitorNewItems,
    qualityProfileId,
    metadataProfileId,
    tags,
    searchForMissingIssues = false
  } = payload;

  const addOptions = {
    monitor,
    searchForMissingIssues
  };

  volume.addOptions = addOptions;
  volume.monitored = true;
  volume.monitorNewItems = monitorNewItems;
  volume.qualityProfileId = qualityProfileId;
  volume.metadataProfileId = metadataProfileId;
  volume.rootFolderPath = rootFolderPath;
  volume.tags = tags;

  return volume;
}

export default getNewVolume;
