using System.Xml.Linq;

namespace MicrosoftAuth.Models.Token
{
    public class SecureScope
    {
        public string Address { get; set; }
        public string? PolicyRef { get; set; }

        public SecureScope(string address, string? policyRef = null)
        {
            Address = address;
            PolicyRef = policyRef;
        }

        public static SecureScope FromElement(XElement element)
            => new(element.Element(XmlConstants.WSP + "AppliesTo").Element(XmlConstants.WSA + "EndpointReference").Element(XmlConstants.WSA + "Address").Value);
    }
}