import getNewVolume from 'Utilities/Volume/getNewVolume';

function getNewIssue(issue, payload) {
  const {
    searchForNewIssue = false
  } = payload;

  if (!('id' in issue.volume) || issue.volume.id === 0) {
    getNewVolume(issue.volume, payload);

    if (payload.monitor === 'specificIssue') {
      delete issue.volume.addOptions.monitor;
      issue.volume.addOptions.issuesToMonitor = [issue.foreignIssueId];
    }
  }

  issue.addOptions = {
    searchForNewIssue
  };
  issue.monitored = true;

  return issue;
}

export default getNewIssue;
