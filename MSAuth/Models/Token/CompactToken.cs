using System.Xml.Linq;

namespace MicrosoftAuth.Models.Token
{
    public class CompactToken : BaseToken
    {
        public CompactToken() : base() { }

        internal CompactToken(XElement element) : base(element)
        {
            Type = TokenType.CompactToken;

            Token = element.Element(XmlConstants.WST + "RequestedSecurityToken")!
                .Element(XmlConstants.WSSE + "BinarySecurityToken")!.Value;
        }
    }
}