import PropTypes from 'prop-types';
import React from 'react';
import VolumeIndexFilterModalConnector from 'Volume/Index/VolumeIndexFilterModalConnector';
import FilterMenu from 'Components/Menu/FilterMenu';
import { align } from 'Helpers/Props';

function VolumeIndexFilterMenu(props) {
  const {
    selectedFilterKey,
    filters,
    customFilters,
    isDisabled,
    onFilterSelect
  } = props;

  return (
    <FilterMenu
      alignMenu={align.RIGHT}
      isDisabled={isDisabled}
      selectedFilterKey={selectedFilterKey}
      filters={filters}
      customFilters={customFilters}
      filterModalConnectorComponent={VolumeIndexFilterModalConnector}
      onFilterSelect={onFilterSelect}
    />
  );
}

VolumeIndexFilterMenu.propTypes = {
  selectedFilterKey: PropTypes.oneOfType([PropTypes.string, PropTypes.number]).isRequired,
  filters: PropTypes.arrayOf(PropTypes.object).isRequired,
  customFilters: PropTypes.arrayOf(PropTypes.object).isRequired,
  isDisabled: PropTypes.bool.isRequired,
  onFilterSelect: PropTypes.func.isRequired
};

VolumeIndexFilterMenu.defaultProps = {
  showCustomFilters: false
};

export default VolumeIndexFilterMenu;
