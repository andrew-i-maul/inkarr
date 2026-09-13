import PropTypes from 'prop-types';
import React from 'react';
import FieldSet from 'Components/FieldSet';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import { inputTypes } from 'Helpers/Props';
import translate from 'Utilities/String/translate';

function MetadataSourceSettings(props) {
  const {
    settings,
    onInputChange
  } = props;

  const {
    comicVineApiKey
  } = settings;

  return (
    <FieldSet legend={translate('MetadataSource')}>
      <FormGroup>
        <FormLabel>{translate('ComicVineApiKey')}</FormLabel>

        <FormInputGroup
          type={inputTypes.TEXT}
          name="comicVineApiKey"
          helpText={translate('ComicVineApiKeyHelpText')}
          onChange={onInputChange}
          {...comicVineApiKey}
        />
      </FormGroup>
    </FieldSet>
  );
}

MetadataSourceSettings.propTypes = {
  settings: PropTypes.object.isRequired,
  onInputChange: PropTypes.func.isRequired
};

export default MetadataSourceSettings;
