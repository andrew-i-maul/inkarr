import _ from 'lodash';
import { update } from 'Store/Actions/baseActions';

function updateIssues(section, books, bookIds, options) {
  const data = _.reduce(books, (result, item) => {
    if (bookIds.indexOf(item.id) > -1) {
      result.push({
        ...item,
        ...options
      });
    } else {
      result.push(item);
    }

    return result;
  }, []);

  return update({ section, data });
}

export default updateIssues;
