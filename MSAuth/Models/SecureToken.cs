using System.Xml;
using System.Xml.Linq;
using MicrosoftAuth.Models.Token;
using System.Linq;

namespace MicrosoftAuth.Models
{
    internal abstract class SecureTokenRequest<T> : BaseRequest<T> where T : class
    {
        protected readonly LegacyToken? signingToken;
        protected readonly string appId;
        protected readonly XmlSigner SigningContext;

        protected SecureTokenRequest(LegacyToken? token = null, string appId = "{DF60E2DF-88AD-4526-AE21-83D130EF0F68}") : base(AuthenticationConfig.RST_URL)
        {
            signingToken = token;
            this.appId = appId;
            SigningContext = new XmlSigner();
        }

        protected override XDocument WriteRequestBody()
        {
            var envelope = BuildEnvelope();

            BuildHeader(envelope.Element(XmlConstants.SOAP + "Envelope"));

            BuildBody(envelope.Element(XmlConstants.SOAP + "Envelope"));

            if (signingToken != null)
            {
                SigningContext.AddSignature(envelope, signingToken);
            }

            return envelope;
        }

        private XDocument BuildEnvelope()
        {
            return new XDocument(new XElement(XmlConstants.SOAP + "Envelope",
                new XAttribute(XNamespace.Xmlns + "s", XmlConstants.XMLNS_S),
                new XAttribute(XNamespace.Xmlns + "ps", XmlConstants.XMLNS_PS),
                new XAttribute(XNamespace.Xmlns + "wsse", XmlConstants.XMLNS_WSSE),
                new XAttribute(XNamespace.Xmlns + "saml", XmlConstants.XMLNS_SAML),
                new XAttribute(XNamespace.Xmlns + "wsp", XmlConstants.XMLNS_WSP),
                new XAttribute(XNamespace.Xmlns + "wsu", XmlConstants.XMLNS_WSU),
                new XAttribute(XNamespace.Xmlns + "wsa", XmlConstants.XMLNS_WSA),
                new XAttribute(XNamespace.Xmlns + "wssc", XmlConstants.XMLNS_WSSC),
                new XAttribute(XNamespace.Xmlns + "wst", XmlConstants.XMLNS_WST)
            ));
        }

        protected void BuildHeader(XElement baseElement)
        {
            baseElement.Add(new XElement(XmlConstants.SOAP + "Header",
                new XElement(XmlConstants.WSA + "Action",
                    "http://schemas.xmlsoap.org/ws/2005/02/trust/RST/Issue",
                    new XAttribute(XmlConstants.SOAP + "mustUnderstand", "1")
                ),
                new XElement(XmlConstants.WSA + "To", this.Address.ToString(),
                    new XAttribute(XmlConstants.SOAP + "mustUnderstand", "1")
                ),
                new XElement(XmlConstants.WSA + "MessageID", 
                    DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString()
                )
            ));

            var authInfo = new XElement(XmlConstants.PS + "AuthInfo",
                new XAttribute(XNamespace.Xmlns + "ps", XmlConstants.XMLNS_PS),
                new XAttribute("Id", "PPAuthInfo")
            );

            BuildExtraAuthenticationInfo(authInfo);

            SigningContext.Add(authInfo);

            baseElement.Element(XmlConstants.SOAP + "Header").Add(authInfo);

            BuildSecurityInfo(baseElement);
        }

        public XElement BuildTimestamp()
        {
            var element = new XElement(XmlConstants.WSU + "Timestamp",
                new XAttribute(XNamespace.Xmlns + "wsu", XmlConstants.XMLNS_WSU),
                new XAttribute(XmlConstants.WSU + "Id", "Timestamp"),
                new XElement(XmlConstants.WSU + "Created", DateTime.UtcNow.ToString("s") + "Z"),
                new XElement(XmlConstants.WSU + "Expires", DateTime.UtcNow.AddMinutes(5).ToString("s") + "Z")
            );

            SigningContext.Add(element);

            return element;
        }

        protected abstract void BuildSecurityInfo(XElement baseElement);

        protected virtual void BuildExtraAuthenticationInfo(XElement authElement)
        {
            authElement.Add(
                new XElement(XmlConstants.PS + "BinaryVersion", "19"),
                new XElement(XmlConstants.PS + "UIVersion", "1"),
                new XElement(XmlConstants.PS + "IsAdmin", "1"),
                new XElement(XmlConstants.PS + "Cookies", ""),
                new XElement(XmlConstants.PS + "RequestParams", "AQAAAAIAAABsYwQAAAAxMDMz"),
                new XElement(XmlConstants.PS + "WindowsClientString", "d2TFWYN+emWKaAqzhhGYBeQgDt2GOG2cMcc8GFsVZtU="),
                new XElement(XmlConstants.PS + "HostingApp", this.appId),
                new XElement(XmlConstants.PS + "ClientCapabilities", "1"));
        }

        protected abstract void BuildBody(XElement baseElement); // provides the SOAP + "Body" element

        protected XElement BuildTokenRequest(XElement baseElement, SecureScope scope, int index)
        {
            baseElement.Add(
                new XElement(XmlConstants.WST + "RequestSecurityToken",
                    new XAttribute(XNamespace.Xmlns + "wst", XmlConstants.XMLNS_WST),
                    new XAttribute("Id", "RST" + index),
                    new XElement(XmlConstants.WST + "RequestType", "http://schemas.xmlsoap.org/ws/2005/02/trust/Issue"),
                    new XElement(XmlConstants.WSP + "AppliesTo",
                        new XAttribute(XNamespace.Xmlns + "wsp", XmlConstants.XMLNS_WSP),
                        new XElement(XmlConstants.WSA + "EndpointReference",
                            new XAttribute(XNamespace.Xmlns + "wsa", XmlConstants.XMLNS_WSA),
                            new XElement(XmlConstants.WSA + "Address", scope.Address)
                        )
                    )
                ));

            if (scope.PolicyRef != null)
                baseElement.Elements(XmlConstants.WST + "RequestSecurityToken").Last().Add(
                    new XElement(XmlConstants.WSP + "PolicyReference",
                        new XAttribute(XNamespace.Xmlns + "wsp", XmlConstants.XMLNS_WSP),
                        new XAttribute("URI", scope.PolicyRef)
                    )
                );

            return baseElement.Elements(XmlConstants.WST + "RequestSecurityToken").Last();
        }

        protected XElement BuildMultipleTokenRequest(XElement baseElement)
        {
            baseElement.Add(new XElement(XmlConstants.PS + "RequestMultipleSecurityTokens",
                new XAttribute(XNamespace.Xmlns + "ps", XmlConstants.XMLNS_PS),
                new XAttribute("Id", "RSTS")
            ));

            return baseElement.Elements(XmlConstants.PS + "RequestMultipleSecurityTokens").Last();
        }

        protected override T ParseResponse(string responseText)
        {
            var xmlMessage = XDocument.Parse(responseText);
            var soapEnvelope = xmlMessage.Element(XmlConstants.SOAP + "Envelope");
            var soapHeader = soapEnvelope.Element(XmlConstants.SOAP + "Header");
            var soapBody = soapEnvelope.Element(XmlConstants.SOAP + "Body");

            if (signingToken == null)
                return ParseDecryptedResponse(soapBody);

            var keyTokens = soapHeader.Element(XmlConstants.WSSE + "Security")!.Elements(XmlConstants.WSSC + "DerivedKeyToken");
            var encKeyToken = keyTokens.First(pred => pred.Attribute(XmlConstants.WSU + "Id")?.Value == "EncKey");
            var encKeyNonce = encKeyToken.Element(XmlConstants.WSSC + "Nonce").Value;


            byte[] nonce = Convert.FromBase64String(encKeyNonce);
            byte[] key = Crypto.GenerateSharedKey(32, signingToken.BinarySecret, "WS-SecureConversationWS-SecureConversation", nonce);


            if (soapBody.Element(XmlConstants.SOAP + "Fault") != null)
            {
                var encContainer = soapHeader.Element(XmlConstants.PSF + "EncryptedPP");
                var encData = encContainer.Element(XmlConstants.XMLENC + "EncryptedData");

                var cipherData = encData.Element(XmlConstants.XMLENC + "CipherData").Element(XmlConstants.XMLENC + "CipherValue").Value;

                var decryptedError = Crypto.DecryptSecurityResponse(key, cipherData);

                var response = new SecureTokenResponse();
                response.Errors.Add(TokenErrorInfo.FromElement(XDocument.Parse(decryptedError).Element(XmlConstants.PSF + "pp")));

                return ParseSecurityResponse(response);
            }

            if (soapBody.Element(XmlConstants.XMLENC + "EncryptedData") != null)
            {
                var encData = soapBody.Element(XmlConstants.XMLENC + "EncryptedData");

                var cipherData = encData.Element(XmlConstants.XMLENC + "CipherData").Element(XmlConstants.XMLENC + "CipherValue").Value;

                var decryptedResponse = Crypto.DecryptSecurityResponse(key, cipherData);

                return ParseDecryptedResponse(XDocument.Parse(decryptedResponse).Root);
            }

            return ParseDecryptedResponse(soapBody);
        }

        private T ParseDecryptedResponse(XElement response)
        {
            var multipleTokenResults = response.Element(XmlConstants.WST + "RequestSecurityTokenResponseCollection");

            if (multipleTokenResults != null)
            {
                var secureResponse = new SecureTokenResponse();
                secureResponse.Tokens
                    .AddRange(multipleTokenResults
                        .Elements(XmlConstants.WST + "RequestSecurityTokenResponse")
                        .Select(TokenResponse.FromElement));

                return ParseSecurityResponse(secureResponse);
            }

            var singleTokenResult = response.Element(XmlConstants.WST + "RequestSecurityTokenResponse");
            if (singleTokenResult != null)
            {
                var secureTokenResponse = new SecureTokenResponse();
                secureTokenResponse.Tokens.Add(TokenResponse.FromElement(singleTokenResult));

                return ParseSecurityResponse(secureTokenResponse);
            }

            throw new InvalidDataException("No tokens found in decrypted security token response.");
        }

        protected abstract T ParseSecurityResponse(SecureTokenResponse response);
    }

    internal class SecureTokenResponse
    {
        internal readonly List<TokenResponse> Tokens;
        internal readonly List<TokenErrorInfo> Errors;

        internal SecureTokenResponse()
        {
            Tokens = new List<TokenResponse>();
            Errors = new List<TokenErrorInfo>();
        }
    }
}