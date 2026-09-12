import PropTypes from 'prop-types';
import React from 'react';
import Modal from 'Components/Modal/Modal';
import OrganizeVolumeModalContentConnector from './OrganizeVolumeModalContentConnector';

function OrganizeVolumeModal(props) {
  const {
    isOpen,
    onModalClose,
    ...otherProps
  } = props;

  return (
    <Modal
      isOpen={isOpen}
      onModalClose={onModalClose}
    >
      <OrganizeVolumeModalContentConnector
        {...otherProps}
        onModalClose={onModalClose}
      />
    </Modal>
  );
}

OrganizeVolumeModal.propTypes = {
  isOpen: PropTypes.bool.isRequired,
  onModalClose: PropTypes.func.isRequired
};

export default OrganizeVolumeModal;
