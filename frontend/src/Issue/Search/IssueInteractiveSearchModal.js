import PropTypes from 'prop-types';
import React from 'react';
import Modal from 'Components/Modal/Modal';
import { sizes } from 'Helpers/Props';
import IssueInteractiveSearchModalContent from './IssueInteractiveSearchModalContent';

function IssueInteractiveSearchModal(props) {
  const {
    isOpen,
    bookId,
    bookTitle,
    authorName,
    onModalClose
  } = props;

  return (
    <Modal
      isOpen={isOpen}
      size={sizes.EXTRA_EXTRA_LARGE}
      closeOnBackgroundClick={false}
      onModalClose={onModalClose}
    >
      <IssueInteractiveSearchModalContent
        bookId={bookId}
        bookTitle={bookTitle}
        authorName={authorName}
        onModalClose={onModalClose}
      />
    </Modal>
  );
}

IssueInteractiveSearchModal.propTypes = {
  isOpen: PropTypes.bool.isRequired,
  bookId: PropTypes.number.isRequired,
  bookTitle: PropTypes.string.isRequired,
  authorName: PropTypes.string.isRequired,
  onModalClose: PropTypes.func.isRequired
};

export default IssueInteractiveSearchModal;
