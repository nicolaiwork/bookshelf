using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Download.Clients.PyBookIrc
{
    public class PyBookIrcSettingsValidator : AbstractValidator<PyBookIrcSettings>
    {
        public PyBookIrcSettingsValidator()
        {
            RuleFor(c => c.BaseUrl).ValidRootUrl();
            RuleFor(c => c.AuthToken).NotEmpty();
        }
    }

    public class PyBookIrcSettings : IProviderConfig
    {
        private static readonly PyBookIrcSettingsValidator Validator = new PyBookIrcSettingsValidator();

        public PyBookIrcSettings()
        {
            BaseUrl = "http://pybookirc:8789";
        }

        [FieldDefinition(0, Label = "Daemon URL", HelpText = "Base URL of the pybookirc daemon")]
        public string BaseUrl { get; set; }

        [FieldDefinition(1, Label = "Auth Token", Type = FieldType.Textbox, Privacy = PrivacyLevel.ApiKey, HelpText = "Bearer token — retrieved with: docker exec <container> cat /data/auth_token.txt")]
        public string AuthToken { get; set; }

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
