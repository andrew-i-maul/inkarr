import getNewVolume from 'Utilities/Volume/getNewVolume';

function getNewIssue(book, payload) {
  const {
    searchForNewIssue = false
  } = payload;

  if (!('id' in book.author) || book.author.id === 0) {
    getNewVolume(book.author, payload);

    if (payload.monitor === 'specificIssue') {
      delete book.author.addOptions.monitor;
      book.author.addOptions.booksToMonitor = [book.foreignIssueId];
    }
  }

  book.addOptions = {
    searchForNewIssue
  };
  book.monitored = true;

  return book;
}

export default getNewIssue;
