import PropTypes from 'prop-types';
import React, { Component } from 'react';
import TextTruncate from 'react-text-truncate';
import VolumePoster from 'Volume/VolumePoster';
import CheckInput from 'Components/Form/CheckInput';
import SpinnerButton from 'Components/Link/SpinnerButton';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { kinds } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import AddVolumeOptionsForm from '../Common/AddVolumeOptionsForm.js';
import styles from './AddNewVolumeModalContent.css';

class AddNewVolumeModalContent extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      searchForMissingIssues: false
    };
  }

  //
  // Listeners

  onSearchForMissingIssuesChange = ({ value }) => {
    this.setState({ searchForMissingIssues: value });
  };

  onAddVolumePress = () => {
    this.props.onAddVolumePress(this.state.searchForMissingIssues);
  };

  //
  // Render

  render() {
    const {
      volumeName,
      disambiguation,
      overview,
      images,
      isAdding,
      isSmallScreen,
      onModalClose,
      ...otherProps
    } = this.props;

    return (
      <ModalContent onModalClose={onModalClose}>
        <ModalHeader>
          {translate('AddNewAuthor')}
        </ModalHeader>

        <ModalBody>
          <div className={styles.container}>
            {
              isSmallScreen ?
                null:
                <div className={styles.poster}>
                  <VolumePoster
                    className={styles.poster}
                    images={images}
                    size={250}
                  />
                </div>
            }

            <div className={styles.info}>
              <div className={styles.name}>
                {volumeName}
              </div>

              {
                !!disambiguation &&
                  <span className={styles.disambiguation}>({disambiguation})</span>
              }

              {
                overview ?
                  <div className={styles.overview}>
                    <TextTruncate
                      truncateText="…"
                      line={8}
                      text={overview}
                    />
                  </div> :
                  null
              }

              <AddVolumeOptionsForm
                includeNoneMetadataProfile={false}
                {...otherProps}
              />

            </div>
          </div>
        </ModalBody>

        <ModalFooter className={styles.modalFooter}>
          <label className={styles.searchForMissingIssuesLabelContainer}>
            <span className={styles.searchForMissingIssuesLabel}>
              Start search for missing issues
            </span>

            <CheckInput
              containerClassName={styles.searchForMissingIssuesContainer}
              className={styles.searchForMissingIssuesInput}
              name="searchForMissingIssues"
              value={this.state.searchForMissingIssues}
              onChange={this.onSearchForMissingIssuesChange}
            />
          </label>

          <SpinnerButton
            className={styles.addButton}
            kind={kinds.SUCCESS}
            isSpinning={isAdding}
            onPress={this.onAddVolumePress}
          >
            Add {volumeName}
          </SpinnerButton>
        </ModalFooter>
      </ModalContent>
    );
  }
}

AddNewVolumeModalContent.propTypes = {
  volumeName: PropTypes.string.isRequired,
  disambiguation: PropTypes.string,
  overview: PropTypes.string,
  images: PropTypes.arrayOf(PropTypes.object).isRequired,
  isAdding: PropTypes.bool.isRequired,
  addError: PropTypes.object,
  isSmallScreen: PropTypes.bool.isRequired,
  onModalClose: PropTypes.func.isRequired,
  onAddVolumePress: PropTypes.func.isRequired
};

export default AddNewVolumeModalContent;
