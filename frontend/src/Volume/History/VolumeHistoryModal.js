import PropTypes from 'prop-types';
import React from 'react';
import Modal from 'Components/Modal/Modal';
import { sizes } from 'Helpers/Props';
import VolumeHistoryContentConnector from './VolumeHistoryContentConnector';
import VolumeHistoryModalContent from './VolumeHistoryModalContent';

function VolumeHistoryModal(props) {
  const {
    isOpen,
    onModalClose,
    ...otherProps
  } = props;

  return (
    <Modal
      isOpen={isOpen}
      size={sizes.EXTRA_LARGE}
      onModalClose={onModalClose}
    >
      <VolumeHistoryContentConnector
        component={VolumeHistoryModalContent}
        {...otherProps}
        onModalClose={onModalClose}
      />
    </Modal>
  );
}

VolumeHistoryModal.propTypes = {
  isOpen: PropTypes.bool.isRequired,
  onModalClose: PropTypes.func.isRequired
};

export default VolumeHistoryModal;
