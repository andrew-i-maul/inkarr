import ModelBase from 'App/ModelBase';

export type VolumeStatus = 'continuing' | 'ended';

interface Volume extends ModelBase {
  added: string;
  genres: string[];
  monitored: boolean;
  overview: string;
  path: string;
  qualityProfileId: number;
  metadataProfileId: number;
  rootFolderPath: string;
  sortName: string;
  status: VolumeStatus;
  tags: number[];
  volumeName: string;
  isSaving?: boolean;
}

export default Volume;
