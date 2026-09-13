import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Link from 'Components/Link/Link';
import styles from './SelectVolumeRow.css';

class SelectVolumeRow extends Component {

  //
  // Listeners

  onPress = () => {
    this.props.onVolumeSelect(this.props.id);
  };

  //
  // Render

  render() {
    return (
      <Link
        className={styles.volume}
        component="div"
        onPress={this.onPress}
      >
        {this.props.volumeName}
      </Link>
    );
  }
}

SelectVolumeRow.propTypes = {
  id: PropTypes.number.isRequired,
  volumeName: PropTypes.string.isRequired,
  onVolumeSelect: PropTypes.func.isRequired
};

export default SelectVolumeRow;
