import PropTypes from 'prop-types';
import React from 'react';
import Modal from 'Components/Modal/Modal';
import VolumeIndexOverviewOptionsModalContentConnector from './VolumeIndexOverviewOptionsModalContentConnector';

function VolumeIndexOverviewOptionsModal({ isOpen, onModalClose, ...otherProps }) {
  return (
    <Modal
      isOpen={isOpen}
      onModalClose={onModalClose}
    >
      <VolumeIndexOverviewOptionsModalContentConnector
        {...otherProps}
        onModalClose={onModalClose}
      />
    </Modal>
  );
}

VolumeIndexOverviewOptionsModal.propTypes = {
  isOpen: PropTypes.bool.isRequired,
  onModalClose: PropTypes.func.isRequired
};

export default VolumeIndexOverviewOptionsModal;
