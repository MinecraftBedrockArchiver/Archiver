using System.Text;
using System.Xml.Linq;

namespace MicrosoftAuth.Models.Token
{
    public class LegacyToken : BaseToken
    {
        public byte[] BinarySecret { get; set; }

        public LegacyToken() : base()
        {
            BinarySecret = Array.Empty<byte>();
        }

        public LegacyToken(string token, string key, DateTime startTime, DateTime expirationTime) : base(startTime, expirationTime)
        {
            Type = TokenType.LegacyToken;
            Token = token;
            BinarySecret = Convert.FromBase64String(key);
        }

        internal LegacyToken(XElement element) : base(element)
        {
            Type = TokenType.LegacyToken;

            var requestedTokenElement = element.Element(XmlConstants.WST + "RequestedSecurityToken")!;

            Token = requestedTokenElement.Element(XmlConstants.XMLENC + "EncryptedData")!.ToString(SaveOptions.DisableFormatting);

            BinarySecret = Convert.FromBase64String(element.Element(XmlConstants.WST + "RequestedProofToken")!.Element(XmlConstants.WST + "BinarySecret")!.Value);

        }

    }
}