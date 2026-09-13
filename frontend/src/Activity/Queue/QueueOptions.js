import PropTypes from 'prop-types';
import React, { Component, Fragment } from 'react';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import { inputTypes } from 'Helpers/Props';
import translate from 'Utilities/String/translate';

class QueueOptions extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      includeUnknownVolumeItems: props.includeUnknownVolumeItems
    };
  }

  componentDidUpdate(prevProps) {
    const {
      includeUnknownVolumeItems
    } = this.props;

    if (includeUnknownVolumeItems !== prevProps.includeUnknownVolumeItems) {
      this.setState({
        includeUnknownVolumeItems
      });
    }
  }

  //
  // Listeners

  onOptionChange = ({ name, value }) => {
    this.setState({
      [name]: value
    }, () => {
      this.props.onOptionChange({
        [name]: value
      });
    });
  };

  //
  // Render

  render() {
    const {
      includeUnknownVolumeItems
    } = this.state;

    return (
      <Fragment>
        <FormGroup>
          <FormLabel>
            {translate('ShowUnknownAuthorItems')}
          </FormLabel>

          <FormInputGroup
            type={inputTypes.CHECK}
            name="includeUnknownVolumeItems"
            value={includeUnknownVolumeItems}
            helpText={translate('IncludeUnknownAuthorItemsHelpText')}
            onChange={this.onOptionChange}
          />
        </FormGroup>
      </Fragment>
    );
  }
}

QueueOptions.propTypes = {
  includeUnknownVolumeItems: PropTypes.bool.isRequired,
  onOptionChange: PropTypes.func.isRequired
};

export default QueueOptions;
