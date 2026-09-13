import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { clearPendingChanges } from 'Store/Actions/baseActions';
import EditVolumeModal from './EditVolumeModal';

const mapDispatchToProps = {
  clearPendingChanges
};

class EditVolumeModalConnector extends Component {

  //
  // Listeners

  onModalClose = () => {
    this.props.clearPendingChanges({ section: 'volume' });
    this.props.onModalClose();
  };

  //
  // Render

  render() {
    return (
      <EditVolumeModal
        {...this.props}
        onModalClose={this.onModalClose}
      />
    );
  }
}

EditVolumeModalConnector.propTypes = {
  onModalClose: PropTypes.func.isRequired,
  clearPendingChanges: PropTypes.func.isRequired
};

export default connect(undefined, mapDispatchToProps)(EditVolumeModalConnector);
