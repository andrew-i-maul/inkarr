import PropTypes from 'prop-types';
import React, { Component } from 'react';
import DeleteVolumeModal from 'Volume/Delete/DeleteVolumeModal';
import EditVolumeModalConnector from 'Volume/Edit/EditVolumeModalConnector';
import IconButton from 'Components/Link/IconButton';
import SpinnerIconButton from 'Components/Link/SpinnerIconButton';
import VirtualTableRowCell from 'Components/Table/Cells/VirtualTableRowCell';
import { icons } from 'Helpers/Props';
import translate from 'Utilities/String/translate';

class IssueIndexActionsCell extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      isEditVolumeModalOpen: false,
      isDeleteVolumeModalOpen: false
    };
  }

  //
  // Listeners

  onEditVolumePress = () => {
    this.setState({ isEditVolumeModalOpen: true });
  };

  onEditVolumeModalClose = () => {
    this.setState({ isEditVolumeModalOpen: false });
  };

  onDeleteVolumePress = () => {
    this.setState({
      isEditVolumeModalOpen: false,
      isDeleteVolumeModalOpen: true
    });
  };

  onDeleteVolumeModalClose = () => {
    this.setState({ isDeleteVolumeModalOpen: false });
  };

  //
  // Render

  render() {
    const {
      id,
      isRefreshingVolume,
      onRefreshVolumePress,
      ...otherProps
    } = this.props;

    const {
      isEditVolumeModalOpen,
      isDeleteVolumeModalOpen
    } = this.state;

    return (
      <VirtualTableRowCell
        {...otherProps}
      >
        <SpinnerIconButton
          name={icons.REFRESH}
          title={translate('RefreshAuthor')}
          isSpinning={isRefreshingVolume}
          onPress={onRefreshVolumePress}
        />

        <IconButton
          name={icons.EDIT}
          title={translate('EditAuthor')}
          onPress={this.onEditVolumePress}
        />

        <EditVolumeModalConnector
          isOpen={isEditVolumeModalOpen}
          volumeId={id}
          onModalClose={this.onEditVolumeModalClose}
          onDeleteVolumePress={this.onDeleteVolumePress}
        />

        <DeleteVolumeModal
          isOpen={isDeleteVolumeModalOpen}
          volumeId={id}
          onModalClose={this.onDeleteVolumeModalClose}
        />
      </VirtualTableRowCell>
    );
  }
}

IssueIndexActionsCell.propTypes = {
  id: PropTypes.number.isRequired,
  isRefreshingVolume: PropTypes.bool.isRequired,
  onRefreshVolumePress: PropTypes.func.isRequired
};

export default IssueIndexActionsCell;
