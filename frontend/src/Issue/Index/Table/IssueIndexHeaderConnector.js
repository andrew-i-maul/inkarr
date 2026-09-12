import { connect } from 'react-redux';
import { setBookTableOption as setIssueTableOption } from 'Store/Actions/bookIndexActions';
import IssueIndexHeader from './IssueIndexHeader';

function createMapDispatchToProps(dispatch, props) {
  return {
    onTableOptionChange(payload) {
      dispatch(setIssueTableOption(payload));
    }
  };
}

export default connect(undefined, createMapDispatchToProps)(IssueIndexHeader);
