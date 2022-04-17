using System.Xml.Linq;

namespace MicrosoftAuth
{
    internal class XmlConstants
    {
        #region Namespace Constants
        internal const string XMLNS_S = "http://www.w3.org/2003/05/soap-envelope";
        internal const string XMLNS_PS = "http://schemas.microsoft.com/Passport/SoapServices/PPCRL";
        internal const string XMLNS_WSSE = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";
        internal const string XMLNS_SAML = "urn:oasis:names:tc:SAML:1.0:assertion";
        internal const string XMLNS_WSP = "http://schemas.xmlsoap.org/ws/2004/09/policy";
        internal const string XMLNS_WSU = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd";
        internal const string XMLNS_WSA = "http://www.w3.org/2005/08/addressing";
        internal const string XMLNS_WSSC = "http://schemas.xmlsoap.org/ws/2005/02/sc";
        internal const string XMLNS_WST = "http://schemas.xmlsoap.org/ws/2005/02/trust";

        internal static readonly XNamespace PS = XMLNS_PS;
        internal static readonly XNamespace WSA = XMLNS_WSA;
        internal static readonly XNamespace WSU = XMLNS_WSU;
        internal static readonly XNamespace WST = XMLNS_WST;
        internal static readonly XNamespace WSP = XMLNS_WSP;
        internal static readonly XNamespace PSF = "http://schemas.microsoft.com/Passport/SoapServices/SOAPFault";
        internal static readonly XNamespace SOAP = XMLNS_S;
        internal static readonly XNamespace WSSC = XMLNS_WSSC;
        internal static readonly XNamespace WSSE = XMLNS_WSSE;
        internal static readonly XNamespace XMLENC = "http://www.w3.org/2001/04/xmlenc#";

        #endregion
    }
}