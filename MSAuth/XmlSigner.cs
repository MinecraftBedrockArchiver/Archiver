using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using MicrosoftAuth.Models.Token;

namespace MicrosoftAuth
{
    // TODO: This could be ported over to the .NET SignedXml class
    internal class XmlSigner
    {
        private byte[] Nonce = Crypto.CreateNonce();
        private List<XElement> NodesToSign = new();

        public void Add(XElement element)
            => NodesToSign.Add(element);
        public void BuildNonce(XElement baseElement)
        {
            baseElement.Add(new XElement(XmlConstants.WSSC + "DerivedKeyToken",
                new XAttribute(XmlConstants.WSU + "Id", "SignKey"),
                new XAttribute("Algorithm", "urn:liveid:SP800-108CTR-HMAC-SHA256"),
                new XElement(XmlConstants.WSSE + "RequestedTokenReference",
                    new XElement(XmlConstants.WSSE + "KeyIdentifier",
                        new XAttribute("ValueType",
                            "http://docs.oasis-open.org/wss/2004/XX/oasis-2004XX-wss-saml-token-profile-1.0#SAMLAssertionID")
                    ),
                    new XElement(XmlConstants.WSSE + "Reference",
                        new XAttribute("URI", "#DeviceDAToken")
                    )
                ),
                new XElement(XmlConstants.WSSC + "Nonce", Convert.ToBase64String(Nonce))
            ));
        }

        public void BuildSignature(XDocument document, XElement baseElement, LegacyToken token)
        {
            var xmlDoc = new XmlDocument();
            xmlDoc.Load(document.CreateReader());

            var signedXml = new MsXmlSigner(xmlDoc)
            {
                SignedInfo =
                {
                    CanonicalizationMethod = SignedXml.XmlDsigExcC14NTransformUrl,
                    SignatureMethod = "http://www.w3.org/2001/04/xmldsig-more#hmac-sha256"
                }
            };

            var key = new HMACSHA256(Crypto.GenerateSharedKey(32, token.BinarySecret,
                "WS-SecureConversationWS-SecureConversation", Nonce));

            foreach (XElement element in NodesToSign)
            {
                var reference = new Reference();
                reference.AddTransform(new XmlDsigExcC14NTransform());

                var attributes = element.Attributes();
                reference.Uri = "#" + attributes.First(pred => pred.Name.LocalName == "Id").Value;

                signedXml.AddReference(reference);

            }

            signedXml.ComputeSignature(key);

            var signatureXml = signedXml.GetXml();

            var signature = XElement.Load(signatureXml.CreateNavigator().ReadSubtree());
            var signNs = XNamespace.Get("http://www.w3.org/2000/09/xmldsig#");

            baseElement.Add(signature);
            baseElement.Element(signNs + "Signature")
                .Add(new XElement(signNs + "KeyInfo",
                    new XElement(XmlConstants.WSSE + "SecurityTokenReference",
                        new XElement(XmlConstants.WSSE + "Reference",
                            new XAttribute("URI", "#SignKey")
                        )
                    )
                ));
        }

        internal void AddSignature(XDocument document, LegacyToken signingToken)
        {
            var header = document.Element(XmlConstants.SOAP + "Envelope").Element(XmlConstants.SOAP + "Header").Element(XmlConstants.WSSE + "Security");
            BuildNonce(header);
            BuildSignature(document, header, signingToken);
        }

        private string GetXmlString(XElement element)
        {
            var sb = new StringBuilder();
            var xw = XmlWriter.Create(sb);
            element.WriteTo(xw);
            xw.Flush();
            return sb.ToString();
        }

        private XElement BuildSignatureInfo(XElement element)
        {
            var hash = Convert.ToBase64String(Crypto.HashSha256(GetXmlString(element)));

            return new XElement("Reference",
                new XAttribute("URI",
                    "#" + (element.Element("Id") != null ? element.Element("Id").Value : element.Element(XmlConstants.WSU + "Id").Value)),
                new XElement("Transforms",
                    new XElement("Transform",
                        new XAttribute("Algorithm", "http://www.w3.org/2001/10/xml-exc-c14n#")
                    )
                ),
                new XElement("DigestMethod",
                    new XElement("Algorithm", "http://www.w3.org/2001/04/xmlenc#sha256")
                ),
                new XElement("DigestValue", hash)
            );
        }
    }

    internal class MsXmlSigner : SignedXml
    {
        public MsXmlSigner(XmlDocument document) : base(document)
        {
        }

        public override XmlElement GetIdElement(XmlDocument document, string idValue)
        {
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(document.NameTable);
            nsmgr.AddNamespace("wsu", XmlConstants.WSU.NamespaceName);
            if (document.SelectSingleNode($"//*[@wsu:Id='{idValue}']", nsmgr) is XmlElement element)
            {
                return element;
            }

            return base.GetIdElement(document, idValue);
        }
    }
}