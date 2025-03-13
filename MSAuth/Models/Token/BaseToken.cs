using System.Xml.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MicrosoftAuth.Models.Token
{
    public class BaseToken
    {
        public string Token { get; set; }
        public SecureScope? TokenScope { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? ExpirationTime { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public TokenType Type { get; protected set; }

        public bool IsExpired() => DateTime.UtcNow.CompareTo(ExpirationTime) > 0;

        public BaseToken()
        {
            Token = "";
        }

        internal BaseToken(DateTime start, DateTime end)
        {
            Token = "";
            StartTime = start;
            ExpirationTime = end;
        }

        internal BaseToken(XElement element)
        {
            Token = "";
            TokenScope = SecureScope.FromElement(element);

            var lifetimeInfo = element.Element(XmlConstants.WST + "Lifetime");
            if (lifetimeInfo != null)
            {
                StartTime = DateTime.Parse(lifetimeInfo.Element(XmlConstants.WSU + "Created")!.Value);
                ExpirationTime = DateTime.Parse(lifetimeInfo.Element(XmlConstants.WSU + "Expires")!.Value);
            }
        }

        public static BaseToken ParseTokenFromElement(XElement element)
        {
            var tokenType = element.Element(XmlConstants.WST + "TokenType")!.Value;

            return tokenType switch
            {
                "urn:passport:legacy" => new LegacyToken(element),
				"urn:passport:compact" => new CompactToken(element),
				"urn:passport:delegationcompact" => new CompactToken(element),
				_ => throw new InvalidDataException("Invalid token type received.")
            };
        }
    }
}