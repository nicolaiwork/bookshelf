using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.PyBookIrc
{
    public class PyBookIrcSettingsValidator : AbstractValidator<PyBookIrcSettings>
    {
        public PyBookIrcSettingsValidator()
        {
            RuleFor(c => c.BaseUrl).ValidRootUrl();
            RuleFor(c => c.AuthToken).NotEmpty();
        }
    }

    public class PyBookIrcSettings : IIndexerSettings
    {
        private static readonly PyBookIrcSettingsValidator Validator = new PyBookIrcSettingsValidator();

        public PyBookIrcSettings()
        {
            BaseUrl = "http://pybookirc:8789";
        }

        [FieldDefinition(0, Label = "Daemon URL", HelpText = "Base URL of the pybookirc daemon")]
        public string BaseUrl { get; set; }

        [FieldDefinition(1, Label = "Auth Token", Type = FieldType.Textbox, Privacy = PrivacyLevel.ApiKey, HelpText = "Bearer token — daemon writes it to ${DATA_DIR}/auth_token.txt on first run")]
        public string AuthToken { get; set; }

        [FieldDefinition(2, Type = FieldType.Number, Label = "Early Download Limit", Unit = "days", HelpText = "Time before release date Readarr will download from this indexer, empty is no limit", Advanced = true)]
        public int? EarlyReleaseLimit { get; set; }

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
