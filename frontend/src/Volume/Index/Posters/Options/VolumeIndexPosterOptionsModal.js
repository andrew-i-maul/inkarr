import PropTypes from 'prop-types';
import React from 'react';
import Modal from 'Components/Modal/Modal';
import VolumeIndexPosterOptionsModalContentConnector from './VolumeIndexPosterOptionsModalContentConnector';

function VolumeIndexPosterOptionsModal({ isOpen, onModalClose, ...otherProps }) {
  return (
    <Modal
      isOpen={isOpen}
      onModalClose={onModalClose}
    >
      <VolumeIndexPosterOptionsModalContentConnector
        {...otherProps}
        onModalClose={onModalClose}
      />
    </Modal>
  );
}

VolumeIndexPosterOptionsModal.propTypes = {
  isOpen: PropTypes.bool.isRequired,
  onModalClose: PropTypes.func.isRequired
};

export default VolumeIndexPosterOptionsModal;
