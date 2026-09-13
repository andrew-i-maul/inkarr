import PropTypes from 'prop-types';
import React from 'react';
import Link from 'Components/Link/Link';

function VolumeNameLink({ titleSlug, volumeName, ...otherProps }) {
  const link = `/volume/${titleSlug}`;

  return (
    <Link to={link} {...otherProps}>
      {volumeName}
    </Link>
  );
}

VolumeNameLink.propTypes = {
  titleSlug: PropTypes.string.isRequired,
  volumeName: PropTypes.string.isRequired
};

export default VolumeNameLink;
