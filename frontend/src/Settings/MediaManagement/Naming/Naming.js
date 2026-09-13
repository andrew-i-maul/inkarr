import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Alert from 'Components/Alert';
import FieldSet from 'Components/FieldSet';
import Form from 'Components/Form/Form';
import FormGroup from 'Components/Form/FormGroup';
import FormInputButton from 'Components/Form/FormInputButton';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import { inputTypes, kinds, sizes } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import NamingModal from './NamingModal';
import styles from './Naming.css';

class Naming extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      isNamingModalOpen: false,
      namingModalOptions: null
    };
  }

  //
  // Listeners

  onStandardNamingModalOpenClick = () => {
    this.setState({
      isNamingModalOpen: true,
      namingModalOptions: {
        name: 'standardIssueFormat',
        issue: true,
        additional: true
      }
    });
  };

  onVolumeFolderNamingModalOpenClick = () => {
    this.setState({
      isNamingModalOpen: true,
      namingModalOptions: {
        name: 'volumeFolderFormat'
      }
    });
  };

  onNamingModalClose = () => {
    this.setState({ isNamingModalOpen: false });
  };

  //
  // Render

  render() {
    const {
      advancedSettings,
      isFetching,
      error,
      settings,
      hasSettings,
      examples,
      examplesPopulated,
      onInputChange
    } = this.props;

    const {
      isNamingModalOpen,
      namingModalOptions
    } = this.state;

    const renameIssues = hasSettings && settings.renameIssues.value;
    const replaceIllegalCharacters = hasSettings && settings.replaceIllegalCharacters.value;

    const colonReplacementOptions = [
      { key: 0, value: translate('Delete') },
      { key: 1, value: translate('ReplaceWithDash') },
      { key: 2, value: translate('ReplaceWithSpaceDash') },
      { key: 3, value: translate('ReplaceWithSpaceDashSpace') },
      { key: 4, value: translate('SmartReplace'), hint: translate('DashOrSpaceDashDependingOnName') }
    ];

    const standardIssueFormatHelpTexts = [];
    const standardIssueFormatErrors = [];
    const volumeFolderFormatHelpTexts = [];
    const volumeFolderFormatErrors = [];

    if (examplesPopulated) {
      if (examples.singleIssueExample) {
        standardIssueFormatHelpTexts.push(`Single Issue: ${examples.singleIssueExample}`);
      } else {
        standardIssueFormatErrors.push({ message: 'Single Issue: Invalid Format' });
      }

      if (examples.multiPartIssueExample) {
        standardIssueFormatHelpTexts.push(`Multi-part Issue: ${examples.multiPartIssueExample}`);
      } else {
        standardIssueFormatErrors.push({ message: 'Multi-part Issue: Invalid Format' });
      }

      if (examples.volumeFolderExample) {
        volumeFolderFormatHelpTexts.push(`Example: ${examples.volumeFolderExample}`);
      } else {
        volumeFolderFormatErrors.push({ message: 'Invalid Format' });
      }
    }

    return (
      <FieldSet legend={translate('BookNaming')}>
        {
          isFetching &&
            <LoadingIndicator />
        }

        {
          !isFetching && error &&
            <Alert kind={kinds.DANGER}>
              {translate('UnableToLoadNamingSettings')}
            </Alert>
        }

        {
          hasSettings && !isFetching && !error &&
            <Form>
              <FormGroup size={sizes.MEDIUM}>
                <FormLabel>
                  {translate('RenameBooks')}
                </FormLabel>

                <FormInputGroup
                  type={inputTypes.CHECK}
                  name="renameIssues"
                  helpText={translate('RenameBooksHelpText')}
                  onChange={onInputChange}
                  {...settings.renameIssues}
                />
              </FormGroup>

              <FormGroup size={sizes.MEDIUM}>
                <FormLabel>
                  {translate('ReplaceIllegalCharacters')}
                </FormLabel>

                <FormInputGroup
                  type={inputTypes.CHECK}
                  name="replaceIllegalCharacters"
                  helpText={translate('ReplaceIllegalCharactersHelpText')}
                  onChange={onInputChange}
                  {...settings.replaceIllegalCharacters}
                />
              </FormGroup>

              {
                replaceIllegalCharacters ?
                  <FormGroup>
                    <FormLabel>
                      {translate('ColonReplacement')}
                    </FormLabel>

                    <FormInputGroup
                      type={inputTypes.SELECT}
                      name="colonReplacementFormat"
                      values={colonReplacementOptions}
                      onChange={onInputChange}
                      {...settings.colonReplacementFormat}
                    />
                  </FormGroup> :
                  null
              }

              {
                renameIssues &&
                  <div>
                    <FormGroup size={sizes.LARGE}>
                      <FormLabel>
                        {translate('StandardBookFormat')}
                      </FormLabel>

                      <FormInputGroup
                        inputClassName={styles.namingInput}
                        type={inputTypes.TEXT}
                        name="standardIssueFormat"
                        buttons={<FormInputButton onPress={this.onStandardNamingModalOpenClick}>?</FormInputButton>}
                        onChange={onInputChange}
                        {...settings.standardIssueFormat}
                        helpTexts={standardIssueFormatHelpTexts}
                        errors={[...standardIssueFormatErrors, ...settings.standardIssueFormat.errors]}
                      />
                    </FormGroup>
                  </div>
              }

              <FormGroup
                advancedSettings={advancedSettings}
                isAdvanced={true}
              >
                <FormLabel>
                  {translate('AuthorFolderFormat')}
                </FormLabel>

                <FormInputGroup
                  inputClassName={styles.namingInput}
                  type={inputTypes.TEXT}
                  name="volumeFolderFormat"
                  buttons={<FormInputButton onPress={this.onVolumeFolderNamingModalOpenClick}>?</FormInputButton>}
                  onChange={onInputChange}
                  {...settings.volumeFolderFormat}
                  helpTexts={['Used when adding a new volume or moving an volume via the volume editor', ...volumeFolderFormatHelpTexts]}
                  errors={[...volumeFolderFormatErrors, ...settings.volumeFolderFormat.errors]}
                />
              </FormGroup>

              {
                namingModalOptions &&
                  <NamingModal
                    isOpen={isNamingModalOpen}
                    advancedSettings={advancedSettings}
                    {...namingModalOptions}
                    value={settings[namingModalOptions.name].value}
                    onInputChange={onInputChange}
                    onModalClose={this.onNamingModalClose}
                  />
              }
            </Form>
        }
      </FieldSet>
    );
  }

}

Naming.propTypes = {
  advancedSettings: PropTypes.bool.isRequired,
  isFetching: PropTypes.bool.isRequired,
  error: PropTypes.object,
  settings: PropTypes.object.isRequired,
  hasSettings: PropTypes.bool.isRequired,
  examples: PropTypes.object.isRequired,
  examplesPopulated: PropTypes.bool.isRequired,
  onInputChange: PropTypes.func.isRequired
};

export default Naming;
