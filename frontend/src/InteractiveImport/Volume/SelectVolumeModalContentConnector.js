import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { saveInteractiveImportItem, updateInteractiveImportItem } from 'Store/Actions/interactiveImportActions';
import createAllVolumeSelector from 'Store/Selectors/createAllVolumesSelector';
import SelectVolumeModalContent from './SelectVolumeModalContent';

function createMapStateToProps() {
  return createSelector(
    createAllVolumeSelector(),
    (items) => {
      return {
        items: [...items].sort((a, b) => {
          if (a.sortName < b.sortName) {
            return -1;
          }

          if (a.sortName > b.sortName) {
            return 1;
          }

          return 0;
        })
      };
    }
  );
}

const mapDispatchToProps = {
  updateInteractiveImportItem,
  saveInteractiveImportItem
};

class SelectVolumeModalContentConnector extends Component {

  //
  // Listeners

  onVolumeSelect = (volumeId) => {
    const volume = _.find(this.props.items, { id: volumeId });

    const ids = this.props.ids;

    ids.forEach((id) => {
      this.props.updateInteractiveImportItem({
        id,
        volume,
        issue: undefined,
        foreignEditionId: undefined,
        rejections: []
      });
    });

    this.props.saveInteractiveImportItem({ ids });

    this.props.onModalClose(true);
  };

  //
  // Render

  render() {
    return (
      <SelectVolumeModalContent
        {...this.props}
        onVolumeSelect={this.onVolumeSelect}
      />
    );
  }
}

SelectVolumeModalContentConnector.propTypes = {
  ids: PropTypes.arrayOf(PropTypes.number).isRequired,
  items: PropTypes.arrayOf(PropTypes.object).isRequired,
  saveInteractiveImportItem: PropTypes.func.isRequired,
  updateInteractiveImportItem: PropTypes.func.isRequired,
  onModalClose: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(SelectVolumeModalContentConnector);
