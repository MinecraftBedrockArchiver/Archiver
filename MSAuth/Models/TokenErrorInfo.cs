using System.Xml.Linq;

namespace MicrosoftAuth.Models
{
    public class TokenErrorInfo
    {
        public string? ReqStatus { get; set; }
        public string? ErrorStatus { get; set; }
        public string? FlowUri { get; set; }
        public string? InlineAuthUrl { get; set; }
        public string? InlineEndAuthUrl { get; set; }

        public static TokenErrorInfo FromElement(XElement document)
        {
            return new TokenErrorInfo
            {
                ReqStatus = document.Element(XmlConstants.PSF + "reqstatus")?.Value,
                ErrorStatus = document.Element(XmlConstants.PSF + "errorstatus")?.Value,
                FlowUri = document.Element(XmlConstants.PSF + "flowuri")?.Value,
                InlineAuthUrl = document.Element(XmlConstants.PSF + "inlineauthurl")?.Value,
                InlineEndAuthUrl = document.Element(XmlConstants.PSF + "inlineendauthurl")?.Value
            };
        }
    }
}