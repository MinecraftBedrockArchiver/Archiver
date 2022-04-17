using MicrosoftAuth.Models.Token;
using System.Xml;
using System.Xml.Linq;

namespace MicrosoftAuth.Models
{
    internal class AuthenticateDeviceRequest : SecureTokenRequest<AuthenticateDeviceResponse> 
    {
        private string Name { get; set; }
        private string Password { get; set; }

        internal AuthenticateDeviceRequest(string deviceName, string devicePassword)
        {
            Name = deviceName;
            Password = devicePassword;
        }

        protected override void BuildExtraAuthenticationInfo(XElement authElement)
        {
            base.BuildExtraAuthenticationInfo(authElement);

            authElement.Add(new XElement(XmlConstants.PS + "InlineUX", "TokenBroker"));
        }

        protected override AuthenticateDeviceResponse ParseSecurityResponse(SecureTokenResponse response)
        {
            if (response.Tokens.Count > 0)
            {
                var token = response.Tokens[0];
                if (token.TokenError != null)
                    throw new InvalidDataException("Received faulty device token from response.");

                return new AuthenticateDeviceResponse(token.Token as LegacyToken);
            }

            throw new IndexOutOfRangeException("No tokens received for device token request.");
        }

        protected override void BuildSecurityInfo(XElement baseElement)
        {
            baseElement.Element(XmlConstants.SOAP + "Header").Add(new XElement(XmlConstants.WSSE + "Security",
                new XElement(XmlConstants.WSSE + "UsernameToken",
                    new XAttribute(XmlConstants.WSU + "Id", "devicesoftware"),
                    new XElement(XmlConstants.WSSE + "Username", Name),
                    new XElement(XmlConstants.WSSE + "Password", Password)
                ),
                BuildTimestamp()
            ));
        }

        protected override void BuildBody(XElement baseElement)
        {
            var bodyElement = new XElement(XmlConstants.SOAP + "Body");
            baseElement.Add(bodyElement);
            BuildTokenRequest(bodyElement, new SecureScope("http://Passport.NET/tb"), 0);
        }
    }

    internal class AuthenticateDeviceResponse
    {
        internal LegacyToken DeviceToken { get; }

        internal AuthenticateDeviceResponse(LegacyToken token)
        {
            DeviceToken = token;
        }
    }
}