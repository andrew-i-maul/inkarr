import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Alert from 'Components/Alert';
import CheckInput from 'Components/Form/CheckInput';
import Icon from 'Components/Icon';
import Button from 'Components/Link/Button';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { icons, kinds } from 'Helpers/Props';
import styles from './RetagVolumeModalContent.css';

class RetagVolumeModalContent extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      updateCovers: false,
      embedMetadata: false
    };
  }

  //
  // Listeners

  onCheckInputChange = ({ name, value }) => {
    this.setState({ [name]: value });
  };

  onRetagVolumePress = () => {
    this.props.onRetagVolumePress(this.state.updateCovers, this.state.embedMetadata);
  };

  //
  // Render

  render() {
    const {
      volumeNames,
      onModalClose
    } = this.props;

    return (
      <ModalContent onModalClose={onModalClose}>
        <ModalHeader>
          Retag Selected Volume
        </ModalHeader>

        <ModalBody>
          <Alert>
            Tip: To preview the tags that will be written... select "Cancel" then click any volume name and use the
            <Icon
              className={styles.retagIcon}
              name={icons.RETAG}
            />
          </Alert>

          <div className={styles.message}>
            Are you sure you want to re-tag all files in the {volumeNames.length} selected volume?
          </div>
          <ul>
            {
              volumeNames.map((volumeName) => {
                return (
                  <li key={volumeName}>
                    {volumeName}
                  </li>
                );
              })
            }
          </ul>
        </ModalBody>

        <ModalFooter>
          <label className={styles.searchForNewIssueLabelContainer}>
            <span className={styles.searchForNewIssueLabel}>
              Update Covers
            </span>

            <CheckInput
              containerClassName={styles.searchForNewIssueContainer}
              className={styles.searchForNewIssueInput}
              name="updateCovers"
              value={this.state.updateCovers}
              onChange={this.onCheckInputChange}
            />
          </label>

          <label className={styles.searchForNewIssueLabelContainer}>
            <span className={styles.searchForNewIssueLabel}>
              Embed Metadata
            </span>

            <CheckInput
              containerClassName={styles.searchForNewIssueContainer}
              className={styles.searchForNewIssueInput}
              name="embedMetadata"
              value={this.state.embedMetadata}
              onChange={this.onCheckInputChange}
            />
          </label>

          <Button onPress={onModalClose}>
            Cancel
          </Button>

          <Button
            kind={kinds.DANGER}
            onPress={this.onRetagVolumePress}
          >
            Retag
          </Button>
        </ModalFooter>
      </ModalContent>
    );
  }
}

RetagVolumeModalContent.propTypes = {
  volumeNames: PropTypes.arrayOf(PropTypes.string).isRequired,
  onModalClose: PropTypes.func.isRequired,
  onRetagVolumePress: PropTypes.func.isRequired
};

export default RetagVolumeModalContent;
