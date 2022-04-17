using System.Text;
using System.Text.Encodings.Web;
using System.Web;
using MicrosoftAuth.Models;
using System.Xml.Linq;
using MicrosoftAuth.Models.Token;

namespace MicrosoftAuth
{
    internal class TokenRequest : SecureTokenRequest<SecureTokenResponse>
    {
        private LegacyToken DeviceToken { get; set; }
        private List<SecureScope> Scopes { get; set; }
        internal TokenRequest(LegacyToken daToken, LegacyToken deviceAuthToken, string appId, params SecureScope[] scopes) : base(daToken, appId)
        {
            DeviceToken = deviceAuthToken;
            Scopes = new List<SecureScope>(scopes);
        }

        protected override void BuildExtraAuthenticationInfo(XElement authElement)
        {
            base.BuildExtraAuthenticationInfo(authElement);

            authElement.Add(new XElement(XmlConstants.PS + "InlineUX", "Silent"));
        }

        protected override void BuildBody(XElement baseElement)
        {
            var bodyElement = new XElement(XmlConstants.SOAP + "Body");
            baseElement.Add(bodyElement);

            if (Scopes.Count > 1)
            {
                var multipleTokenElement = BuildMultipleTokenRequest(bodyElement);
                SigningContext.Add(multipleTokenElement);
                for (int i = 0; i < Scopes.Count; i++)
                    BuildTokenRequest(multipleTokenElement, Scopes[i], i);
            }
            else
            {
                var tokenRequest = BuildTokenRequest(bodyElement, Scopes[0], 0);
                SigningContext.Add(tokenRequest);
            }
        }

        protected override void BuildSecurityInfo(XElement baseElement)
        {
            baseElement.Element(XmlConstants.SOAP + "Header")!.Add(new XElement(XmlConstants.WSSE + "Security",
                XDocument.Parse(SigningToken!.Token).FirstNode,
                new XElement(XmlConstants.WSSE + "BinarySecurityToken",
                    BuildDeviceProofUri(),
                    new XAttribute("ValueType", "urn:liveid:sha1device"),
                    new XAttribute("Id", "DeviceDAToken")
                ),
                BuildTimestamp()
            ));
        }

        private string BuildDeviceProofUri()
        {
            var cryptoNonce = Crypto.CreateNonce();
            var uriValues = new Dictionary<string, string>
            {
                ["ct"] = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromSeconds(1)).ToUnixTimeSeconds().ToString(), // Should be server-time based, but I literally dont care at this point
                ["hashalg"] = "SHA256",
                ["bver"] = "19",
                ["appid"] = appId,
                ["da"] = DeviceToken.Token,
                ["nonce"] = Convert.ToBase64String(cryptoNonce)
            };

            var urlEncodedString = string.Join("&", uriValues.Select(pair => $"{HttpUtility.UrlEncode(pair.Key)}={HttpUtility.UrlEncode(pair.Value)}")).Replace("+", "%20");

            uriValues["hash"] = Crypto.SignData(Encoding.UTF8.GetBytes(urlEncodedString), DeviceToken.BinarySecret,
                "WS-SecureConversation", cryptoNonce);

            return string.Join("&", uriValues.Select(pair => $"{HttpUtility.UrlEncode(pair.Key)}={HttpUtility.UrlEncode(pair.Value)}")).Replace("+", "%20");
        }

        protected override SecureTokenResponse ParseSecurityResponse(SecureTokenResponse response)
            => response;
    }
}